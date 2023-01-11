//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using IPA.Utilities;
using IPA.Utilities.Async;
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

            _logger.LogInformation($"Loading avatar from '{fullPath}'");

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
            return LoadFromFileAsync(path, null, CancellationToken.None);
        }

        /// <summary>
        /// Load an avatar from a file.
        /// </summary>
        /// <param name="path">Path to the .avatar file.</param>
        /// <param name="progress">The <see cref="IProgress{T}"/> to use to report loading progress.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to propagate notification that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{T}"/> that completes once the avatar has loaded.</returns>
        public Task<AvatarPrefab> LoadFromFileAsync(string path, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!UnityGame.OnMainThread) throw new InvalidOperationException($"{nameof(LoadFromFileAsync)} should only be called on the main thread");

            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                throw new IOException($"File '{fullPath}' does not exist");
            }

            // prevent Unity from complaining that we're loading the same asset bundle more than once concurrently
            if (_tasks.TryGetValue(fullPath, out Task<AvatarPrefab> task))
            {
                return task;
            }

            _logger.LogInformation($"Loading avatar from '{fullPath}'");

            task = LoadAssetBundle(fullPath, progress, cancellationToken);
            _tasks.Add(fullPath, task);
            return task;
        }

        private async Task<AvatarPrefab> LoadAssetBundle(string fullPath, IProgress<float> progress, CancellationToken cancellationToken)
        {
            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fullPath);

            if (progress != null)
            {
                // this isn't amazing, but since there's no progress event and Unity expects the progress
                // property will be accessed in an Update() loop, this should *hopefully* be fine
                _ = UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
                {
                    while (assetBundleCreateRequest.progress < 1f)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress.Report(assetBundleCreateRequest.progress / 0.9f);
                        await Task.Yield();
                    }

                    progress.Report(1);
                }, cancellationToken);
            }

            // for the time being, we don't allow cancelling the actual loading because of the possibility of multiple places
            // waiting for the same task to complete due to the task reuse in LoadFromFileAsync - some kind of cancellation
            // token merging would be required to avoid cancelling the task if it is being awaited from somewhere else

            await assetBundleCreateRequest;
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
            _logger.LogInformation($"Successfully loaded avatar '{avatarPrefab.descriptor.name}' by '{avatarPrefab.descriptor.author}' from '{fullPath}'");

            foreach (LoadHandlers handler in _handlers[fullPath])
            {
                handler.InvokeSuccess(avatarPrefab);
            }

            _handlers.Remove(fullPath);
        }

        [Obsolete]
        private void HandleException(string fullPath, Exception exception)
        {
            _logger.LogError($"Failed to load avatar at '{fullPath}'");
            _logger.LogError(exception);

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
