//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using AssetBundleLoadingTools.Utilities;
using CustomAvatar.Configuration;
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
    /// Allows loading <see cref="AvatarPrefab"/> from various sources.
    /// </summary>
    public class AvatarLoader
    {
        private const string kGameObjectName = "_CustomAvatar";

        private readonly ILogger<AvatarLoader> _logger;
        private readonly DiContainer _container;
        private readonly Settings _settings;

        private readonly Dictionary<string, Task<AvatarPrefab>> _tasks = new();

        internal AvatarLoader(ILogger<AvatarLoader> logger, DiContainer container, Settings settings)
        {
            _logger = logger;
            _container = container;
            _settings = settings;
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
            AssetBundle assetBundle = null;
            AvatarPrefab avatarPrefab = null;

            try
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
                            progress.Report(assetBundleCreateRequest.progress);
                            await Task.Yield();
                        }

                        progress.Report(1);
                    }, cancellationToken);
                }

                // for the time being, we don't allow cancelling the actual loading because of the possibility of multiple places
                // waiting for the same task to complete due to the task reuse in LoadFromFileAsync - some kind of cancellation
                // token merging would be required to avoid cancelling the task if it is being awaited from somewhere else

                await assetBundleCreateRequest;
                assetBundle = assetBundleCreateRequest.assetBundle;

                if (!assetBundle)
                {
                    throw new AvatarLoadException("Could not load asset bundle");
                }

                AssetBundleRequest assetBundleRequest = await assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
                var prefabObject = (GameObject)assetBundleRequest.asset;

                if (!prefabObject)
                {
                    throw new AvatarLoadException("Could not load asset from asset bundle");
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefabObject);

                avatarPrefab = _container.InstantiateComponent<AvatarPrefab>(instance, new object[] { fullPath });

                instance.name = $"AvatarPrefab({avatarPrefab.descriptor.name})";
                instance.SetActive(false);

                await ShaderRepair.FixShadersOnGameObjectAsync(instance);

                return avatarPrefab;
            }
            finally
            {
                if (assetBundle != null)
                {
                    await assetBundle.UnloadAsync(avatarPrefab == null);
                }

                _tasks.Remove(fullPath);
            }
        }
    }
}
