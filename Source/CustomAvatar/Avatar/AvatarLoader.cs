//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Allows loading <see cref="LoadedAvatar"/> from various sources.
    /// </summary>
    public class AvatarLoader
    {
        private const string kGameObjectName = "_CustomAvatar";

        private readonly ILogger<AvatarLoader> _logger;
        private readonly DiContainer _container;

        private readonly Dictionary<string, List<LoadHandlers>> _handlers = new Dictionary<string, List<LoadHandlers>>();

        internal AvatarLoader(ILoggerProvider loggerProvider, DiContainer container)
        {
            _logger = loggerProvider.CreateLogger<AvatarLoader>();
            _container = container;
        }

        // TODO from stream/memory
        /// <summary>
        /// Load an avatar from a file.
        /// </summary>
        /// <param name="path">Path to the .avatar file</param>
        /// <param name="success">Action to call if the avatar is loaded successfully</param>
        /// <param name="error">Action to call if the avatar isn't loaded successfully</param>
        /// <returns><see cref="IEnumerator{AsyncOperation}"/></returns>
        public IEnumerator<AsyncOperation> FromFileCoroutine(string path, Action<LoadedAvatar> success = null, Action<Exception> error = null, Action complete = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath)) throw new IOException($"File '{fullPath}' does not exist");

            // already loading, just add handlers
            if (_handlers.ContainsKey(fullPath))
            {
                _handlers[fullPath].Add(new LoadHandlers(success, error, complete));

                yield break;
            }

            _handlers.Add(fullPath, new List<LoadHandlers> { new LoadHandlers(success, error, complete) });

            _logger.Info($"Loading avatar from '{fullPath}'");

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fullPath);

            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || !assetBundleCreateRequest.assetBundle)
            {
                var exception = new AvatarLoadException("Could not load asset bundle");

                _logger.Error($"Failed to load avatar at '{fullPath}'");
                _logger.Error(exception);

                foreach (LoadHandlers handler in _handlers[fullPath])
                {
                    handler.InvokeError(exception);
                }

                _handlers.Remove(fullPath);

                yield break;
            }

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                assetBundleCreateRequest.assetBundle.Unload(true);

                var exception = new AvatarLoadException("Could not load asset from asset bundle");

                _logger.Error($"Failed to load avatar at '{fullPath}'");
                _logger.Error(exception);

                foreach (LoadHandlers handler in _handlers[fullPath])
                {
                    handler.InvokeError(exception);
                }

                _handlers.Remove(fullPath);

                yield break;
            }

            assetBundleCreateRequest.assetBundle.Unload(false);
                
            try
            {
                var loadedAvatar = _container.Instantiate<LoadedAvatar>(new object[] { fullPath, (GameObject)assetBundleRequest.asset });

                _logger.Info($"Successfully loaded avatar '{loadedAvatar.descriptor.name}' from '{fullPath}'");

                foreach (LoadHandlers handler in _handlers[fullPath])
                {
                    handler.InvokeSuccess(loadedAvatar);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load avatar at '{fullPath}'");
                _logger.Error(ex);

                foreach (LoadHandlers handler in _handlers[fullPath])
                {
                    handler.InvokeError(new AvatarLoadException("Failed to load avatar", ex));
                }
            }

            _handlers.Remove(fullPath);
        }

        private struct LoadHandlers
        {
            private readonly Action<LoadedAvatar> success;
            private readonly Action<Exception> error;
            private readonly Action complete;

            internal LoadHandlers(Action<LoadedAvatar> success, Action<Exception> error, Action complete)
            {
                this.success = success;
                this.error = error;
                this.complete = complete;
            }

            public void InvokeSuccess(LoadedAvatar value)
            {
                success?.Invoke(value);
                complete?.Invoke();
            }

            public void InvokeError(Exception exception)
            {
                error?.Invoke(exception);
                complete?.Invoke();
            }
        }
    }
}
