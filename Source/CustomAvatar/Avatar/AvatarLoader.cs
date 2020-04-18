using System;
using System.Collections.Generic;
using System.IO;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    internal class AvatarLoader
    {
        private const string kGameObjectName = "_CustomAvatar";

        private readonly ILogger _logger;
        private readonly DiContainer _container;

        public AvatarLoader(ILoggerFactory loggerFactory, DiContainer container)
        {
            _logger = loggerFactory.CreateLogger<AvatarLoader>();
            _container = container;
        }

        // TODO from stream/memory
        public IEnumerator<AsyncOperation> FromFileCoroutine(string fileName, Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            _logger.Info($"Loading avatar from '{fileName}'");

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Path.Combine(AvatarManager.kCustomAvatarsPath, fileName));

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;
            assetBundleCreateRequest.assetBundle.Unload(false);

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                var exception = new AvatarLoadException("Could not load asset bundle");

                _logger.Error($"Failed to load avatar {fileName}");
                _logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }
                
            try
            {
                var loadedAvatar = _container.Instantiate<LoadedAvatar>(new object[] { fileName, (GameObject)assetBundleRequest.asset });

                _logger.Info($"Successfully loaded avatar '{loadedAvatar.descriptor.name}' ({fileName})");

                success?.Invoke(loadedAvatar);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load avatar {fileName}");
                _logger.Error(ex);

                error?.Invoke(ex);
            }
        }
    }
}
