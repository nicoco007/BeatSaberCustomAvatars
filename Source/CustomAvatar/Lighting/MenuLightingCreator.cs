//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class MenuLightingCreator : IInitializable, IDisposable
    {
        private readonly LightWithIdManager _lightWithIdManager;
        private readonly Settings _settings;

        private Light _light;

        public MenuLightingCreator(LightWithIdManager lightWithIdManager, Settings settings)
        {
            _lightWithIdManager = lightWithIdManager;
            _settings = settings;
        }

        public void Initialize()
        {
            if (!_settings.lighting.environment.enabled) return;

            var lightObject = new GameObject("Menu Light");
            Transform lightTransform = lightObject.transform;

            lightObject.transform.SetParent(GameObject.Find("/MenuCore").transform, false);
            lightObject.transform.rotation = Quaternion.Euler(30, 180, 0);

            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = 1;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 1;
            _light.renderMode = LightRenderMode.ForcePixel;

            _lightWithIdManager.didChangeSomeColorsThisFrameEvent += UpdateLightColor;

            UpdateLightColor();
        }

        public void Dispose()
        {
            _lightWithIdManager.didChangeSomeColorsThisFrameEvent -= UpdateLightColor;
        }

        private void UpdateLightColor()
        {
            Color color = DirectionalLight.lights.Aggregate(new Color(0, 0, 0, 0), (acc, l) => acc + l.color * l.intensity) / DirectionalLight.lights.Sum(l => l.intensity);

            _light.color = color;
            _light.intensity = color.a;

            RenderSettings.ambientSkyColor = color * 0.2f;
            RenderSettings.ambientEquatorColor = color * 0.5f;
            RenderSettings.ambientGroundColor = color * 0.1f;
        }
    }
}
