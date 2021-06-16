//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Rendering;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class GameEnvironmentObjectManager : IInitializable, IDisposable
    {
        private static readonly int kReflectionProbeTexture1PropertyId = Shader.PropertyToID("_ReflectionProbeTexture1");
        private static readonly int kReflectionProbeTexture2PropertyId = Shader.PropertyToID("_ReflectionProbeTexture2");
        private static readonly Cubemap kBlackCubemap = new Cubemap(0, TextureFormat.DXT1Crunched, false);

        private readonly DiContainer _container;
        private readonly Settings _settings;

        internal GameEnvironmentObjectManager(DiContainer container, Settings settings)
        {
            _container = container;
            _settings = settings;
        }

        public void Initialize()
        {
            switch (_settings.floorHeightAdjust.value)
            {
                case FloorHeightAdjustMode.EntireEnvironment:
                    _container.InstantiateComponent<EnvironmentObject>(GameObject.Find("/Environment"));
                    break;

                case FloorHeightAdjustMode.PlayersPlaceOnly:
                    var environment = GameObject.Find("/Environment");

                    _container.InstantiateComponent<EnvironmentObject>(environment.transform.Find("PlayersPlace").gameObject);

                    Transform shadow = environment.transform.Find("PlayersPlaceShadow");
                    if (shadow) _container.InstantiateComponent<EnvironmentObject>(shadow.gameObject);

                    break;
            }

            // ScoreSaber replay spectator camera
            var spectatorParent = GameObject.Find("/SpectatorParent");

            if (spectatorParent)
            {
                _container.InstantiateComponent<AvatarCenterAdjust>(spectatorParent);
                _container.InstantiateComponent<CustomAvatarsMainCameraController>(spectatorParent.GetComponentInChildren<Camera>().gameObject);
            }
        }

        public void Dispose()
        {
            Shader.SetGlobalTexture(kReflectionProbeTexture1PropertyId, kBlackCubemap);
            Shader.SetGlobalTexture(kReflectionProbeTexture2PropertyId, kBlackCubemap);
        }
    }
}
