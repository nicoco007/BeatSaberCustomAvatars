﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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
using System.Threading.Tasks;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
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

        [Obsolete]
        private readonly Dictionary<string, List<LoadHandlers>> _handlers = new Dictionary<string, List<LoadHandlers>>();

        private readonly Dictionary<string, Task<AvatarPrefab>> _tasks = new Dictionary<string, Task<AvatarPrefab>>();

        internal AvatarLoader(ILogger<AvatarLoader> logger, DiContainer container)
        {
            _logger = logger;
            _container = container;
        }

        [Obsolete("Use LoadFromFileAsync(string) instead")]
        public IEnumerator<AsyncOperation> FromFileCoroutine(string path, Action<LoadedAvatar> success = null, Action<Exception> error = null, Action complete = null)
        {
            return LoadFromFileAsync(path, (avatarPrefab) => success?.Invoke(avatarPrefab.loadedAvatar), error, complete);
        }

        /// <summary>
        /// Load an avatar from a file.
        /// </summary>
        /// <param name="path">Path to the .avatar file</param>
        /// <param name="success">Action to call if the avatar is loaded successfully</param>
        /// <param name="error">Action to call if the avatar isn't loaded successfully</param>
        /// <returns><see cref="IEnumerator{AsyncOperation}"/></returns>
        [Obsolete("Use LoadFromFileAsync(string) instead")]
        public IEnumerator<AsyncOperation> LoadFromFileAsync(string path, Action<AvatarPrefab> success = null, Action<Exception> error = null, Action complete = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);

            // already loading, just add handlers
            if (_handlers.ContainsKey(fullPath))
            {
                _handlers[fullPath].Add(new LoadHandlers(success, error, complete));

                yield break;
            }

            _handlers.Add(fullPath, new List<LoadHandlers> { new LoadHandlers(success, error, complete) });

            if (!File.Exists(fullPath))
            {
                HandleException(fullPath, new IOException($"File '{fullPath}' does not exist"));
                yield break;
            }

            _logger.Info($"Loading avatar from '{fullPath}'");

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fullPath);

            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || !assetBundleCreateRequest.assetBundle)
            {
                HandleException(fullPath, new AvatarLoadException("Could not load asset bundle"));
                yield break;
            }

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                assetBundleCreateRequest.assetBundle.Unload(true);

                HandleException(fullPath, new AvatarLoadException("Could not load asset from asset bundle"));

                yield break;
            }

            assetBundleCreateRequest.assetBundle.Unload(false);

            try
            {
                var prefabObject = (GameObject)assetBundleRequest.asset;
                AvatarPrefab avatarPrefab = _container.InstantiateComponent<AvatarPrefab>(prefabObject, new object[] { fullPath });
                avatarPrefab.name = $"AvatarPrefab({avatarPrefab.descriptor.name})";

                HandleSuccess(fullPath, avatarPrefab);
            }
            catch (Exception ex)
            {
                HandleException(fullPath, ex);
            }
        }

        /// <summary>
        /// Load an avatar from a file.
        /// </summary>
        /// <param name="path">Path to the .avatar file.</param>
        /// <returns>A <see cref="Task{T}"/> that completes once the avatar has loaded.</returns>
        public Task<AvatarPrefab> LoadFromFileAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                throw new IOException($"File '{fullPath}' does not exist");
            }

            if (_tasks.TryGetValue(fullPath, out Task<AvatarPrefab> task))
            {
                return task;
            }

            _logger.Info($"Loading avatar from '{fullPath}'");

            task = LoadAssetBundle(fullPath);
            _tasks.Add(fullPath, task);
            return task;
        }

        private async Task<AvatarPrefab> LoadAssetBundle(string fullPath)
        {
            AssetBundleCreateRequest assetBundleCreateRequest = await AssetBundle.LoadFromFileAsync(fullPath);
            AssetBundle assetBundle = assetBundleCreateRequest.assetBundle;

            if (!assetBundle)
            {
                throw new AvatarLoadException("Could not load asset bundle");
            }

            AssetBundleRequest assetBundleRequest = await assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            var prefabObject = (GameObject)assetBundleRequest.asset;

            if (!prefabObject)
            {
                assetBundle.Unload(true);

                throw new AvatarLoadException("Could not load asset from asset bundle");
            }

            assetBundle.Unload(false);

            AvatarPrefab avatarPrefab = _container.InstantiateComponent<AvatarPrefab>(prefabObject, new object[] { fullPath });
            avatarPrefab.name = $"AvatarPrefab({avatarPrefab.descriptor.name})";

            _tasks.Remove(fullPath);

            return avatarPrefab;
        }

        [Obsolete]
        private void HandleSuccess(string fullPath, AvatarPrefab avatarPrefab)
        {
            _logger.Info($"Successfully loaded avatar '{avatarPrefab.descriptor.name}' by '{avatarPrefab.descriptor.author}' from '{fullPath}'");

            foreach (LoadHandlers handler in _handlers[fullPath])
            {
                handler.InvokeSuccess(avatarPrefab);
            }

            _handlers.Remove(fullPath);
        }

        [Obsolete]
        private void HandleException(string fullPath, Exception exception)
        {
            _logger.Error($"Failed to load avatar at '{fullPath}'");
            _logger.Error(exception);

            foreach (LoadHandlers handler in _handlers[fullPath])
            {
                handler.InvokeError(exception);
            }

            _handlers.Remove(fullPath);
        }

        [Obsolete]
        private struct LoadHandlers
        {
            private readonly Action<AvatarPrefab> _success;
            private readonly Action<Exception> _error;
            private readonly Action _complete;

            public LoadHandlers(Action<AvatarPrefab> success, Action<Exception> error, Action complete)
            {
                _success = success;
                _error = error;
                _complete = complete;
            }

            public void InvokeSuccess(AvatarPrefab value)
            {
                _success?.Invoke(value);
                _complete?.Invoke();
            }

            public void InvokeError(Exception exception)
            {
                _error?.Invoke(exception);
                _complete?.Invoke();
            }
        }
    }
}
