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

        private readonly Dictionary<string, List<LoadHandlers>> _handlers = new Dictionary<string, List<LoadHandlers>>();

        internal AvatarLoader(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<AvatarLoader>();
        }

        // TODO from stream/memory
        public IEnumerator<AsyncOperation> FromFileCoroutine(string path, Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath)) throw new IOException($"File '{fullPath}' does not exist");

            // already loading, just add handlers
            if (_handlers.ContainsKey(fullPath))
            {
                _handlers[fullPath].Add(new LoadHandlers(success, error));

                yield break;
            }

            _handlers.Add(fullPath, new List<LoadHandlers> { new LoadHandlers(success, error) });

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
                    handler.error?.Invoke(exception);
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
                    handler.error?.Invoke(exception);
                }

                _handlers.Remove(fullPath);

                yield break;
            }

            assetBundleCreateRequest.assetBundle.Unload(false);
                
            try
            {
                var loadedAvatar = new LoadedAvatar(fullPath, (GameObject)assetBundleRequest.asset);

                _logger.Info($"Successfully loaded avatar '{loadedAvatar.descriptor.name}' from '{fullPath}'");

                foreach (LoadHandlers handler in _handlers[fullPath])
                {
                    handler.success?.Invoke(loadedAvatar);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load avatar at '{fullPath}'");
                _logger.Error(ex);

                foreach (LoadHandlers handler in _handlers[fullPath])
                {
                    handler.error?.Invoke(ex);
                }
            }

            _handlers.Remove(fullPath);
        }

        private class LoadHandlers
        {
            internal readonly Action<LoadedAvatar> success;
            internal readonly Action<Exception> error;

            internal LoadHandlers(Action<LoadedAvatar> success, Action<Exception> error)
            {
                this.success = success;
                this.error = error;
            }
        }
    }
}
