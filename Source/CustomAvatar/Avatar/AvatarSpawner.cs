using System;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    public class AvatarSpawner
    {
        private readonly ILogger _logger;
        private readonly DiContainer _container;

        internal AvatarSpawner(ILoggerProvider loggerProvider, DiContainer container)
        {
            _logger = loggerProvider.CreateLogger<AvatarSpawner>();
            _container = container;
        }

        public SpawnedAvatar SpawnAvatar(LoadedAvatar avatar, AvatarInput input, Transform parent = null)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            if (input == null) throw new ArgumentNullException(nameof(input));

            if (parent)
            {
                _logger.Info($"Spawning avatar '{avatar.descriptor.name}' into '{parent.name}'");
            }
            else
            {
                _logger.Info($"Spawning avatar '{avatar.descriptor.name}'");
            }

            GameObject avatarInstance = _container.InstantiatePrefab(avatar.prefab, parent);
            return _container.InstantiateComponent<SpawnedAvatar>(avatarInstance, new object[] { avatar, input });
        }
    }
}
