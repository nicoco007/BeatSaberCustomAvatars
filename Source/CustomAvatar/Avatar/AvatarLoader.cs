using System;
using System.Collections.Generic;
using System.IO;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using UnityEngine;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    public class AvatarLoader
    {
        private const string kGameObjectName = "_CustomAvatar";

        private readonly ILogger _logger;

        internal AvatarLoader(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AvatarLoader>();
        }

        // TODO from stream/memory
        public IEnumerator<AsyncOperation> FromFileCoroutine(string filePath, Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            _logger.Info($"Loading avatar from '{filePath}'");

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, filePath));

            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || !assetBundleCreateRequest.assetBundle)
            {
                var exception = new AvatarLoadException("Could not load asset bundle");

                _logger.Error($"Failed to load avatar at '{filePath}'");
                _logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                assetBundleCreateRequest.assetBundle.Unload(true);

                var exception = new AvatarLoadException("Could not load asset from asset bundle");

                _logger.Error($"Failed to load avatar at '{filePath}'");
                _logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }

            assetBundleCreateRequest.assetBundle.Unload(false);
                
            try
            {
                var loadedAvatar = new LoadedAvatar(filePath, (GameObject)assetBundleRequest.asset);

                _logger.Info($"Successfully loaded avatar '{loadedAvatar.descriptor.name}' from '{filePath}'");

                success?.Invoke(loadedAvatar);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load avatar at '{filePath}'");
                _logger.Error(ex);

                error?.Invoke(ex);
            }
        }
    }
}
