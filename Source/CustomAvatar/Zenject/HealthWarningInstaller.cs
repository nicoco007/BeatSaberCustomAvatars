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
using CustomAvatar.Lighting;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class HealthWarningInstaller : Installer
    {
        private readonly ILogger<HealthWarningInstaller> _logger;

        internal HealthWarningInstaller(ILogger<HealthWarningInstaller> logger)
        {
            _logger = logger;
        }

        public override void InstallBindings()
        {
            TryAddEnvironmentObject("/BasicMenuGround");
            TryAddEnvironmentObject("/MenuFogRing (1)");

            Container.Bind(typeof(IInitializable), typeof(IDisposable)).To<MenuLightingCreator>().AsSingle().NonLazy();
        }

        private void TryAddEnvironmentObject(string name)
        {
            var gameObject = GameObject.Find(name);

            if (gameObject)
            {
                Container.QueueForInject(gameObject.AddComponent<EnvironmentObject>());
            }
            else
            {
                _logger.LogError($"GameObject '{name}' does not exist!");
            }
        }
    }
}
