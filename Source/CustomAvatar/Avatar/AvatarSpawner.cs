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
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarSpawner
    {
        private readonly ILogger<AvatarSpawner> _logger;
        private readonly DiContainer _container;

        internal AvatarSpawner(ILoggerProvider loggerProvider, DiContainer container)
        {
            _logger = loggerProvider.CreateLogger<AvatarSpawner>();
            _container = container;
        }

        public SpawnedAvatar SpawnAvatar(LoadedAvatar avatar, IAvatarInput input, Transform parent = null)
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

            DiContainer subContainer = new DiContainer(_container);

            subContainer.Bind<LoadedAvatar>().FromInstance(avatar);
            subContainer.Bind<IAvatarInput>().FromInstance(input);

            GameObject avatarInstance = subContainer.InstantiatePrefab(avatar.prefab, parent);
            return subContainer.InstantiateComponent<SpawnedAvatar>(avatarInstance);
        }
    }
}
