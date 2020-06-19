using System;
using System.ComponentModel;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    public class AvatarSpawner
    {
        private readonly ILogger _logger;
        private readonly DiContainer _container;
        private readonly Settings _settings;

        internal AvatarSpawner(ILoggerProvider loggerProvider, DiContainer container, Settings settings)
        {
            _logger = loggerProvider.CreateLogger<AvatarSpawner>();
            _container = container;
            _settings = settings;
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

            Settings.AvatarSpecificSettings avatarSettings = _settings.GetAvatarSettings(avatar.fullPath);

            DiContainer subContainer = new DiContainer(_container);

            subContainer.Bind<LoadedAvatar>().FromInstance(avatar);
            subContainer.Bind<AvatarInput>().FromInstance(input);
            subContainer.Bind<Settings.AvatarSpecificSettings>().FromInstance(avatarSettings);

            GameObject avatarInstance = subContainer.InstantiatePrefab(avatar.prefab, parent);
            return subContainer.InstantiateComponent<SpawnedAvatar>(avatarInstance);
        }
    }
}
