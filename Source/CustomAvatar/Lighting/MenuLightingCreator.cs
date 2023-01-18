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
            lightTransform.rotation = Quaternion.Euler(30, 180, 0);

            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 1;
            _light.renderMode = LightRenderMode.Auto;

            _lightWithIdManager.didChangeSomeColorsThisFrameEvent += UpdateLightColor;

            UpdateLightColor();

            HideMenuReflectionProbe();
        }

        public void Dispose()
        {
            _lightWithIdManager.didChangeSomeColorsThisFrameEvent -= UpdateLightColor;
        }

        private void UpdateLightColor()
        {
            Color color = DirectionalLight.lights.Aggregate(new Color(0, 0, 0, 0), (acc, l) => acc + l.color * l.intensity) / DirectionalLight.lights.Sum(l => l.intensity);

            _light.color = color;
            _light.intensity = color.a * 1.5f;

            RenderSettings.ambientSkyColor = color * 0.1f;
            RenderSettings.ambientEquatorColor = color * 0.3f;
            RenderSettings.ambientGroundColor = color * 0.05f;
        }

        private void HideMenuReflectionProbe()
        {
            // there unfortunately don't seem to be any injectable components that can get to the ReflectionProbe object more directly
            var gameObject = GameObject.Find("/Wrapper/MenuEnvironmentCore/ReflectionProbe");

            if (gameObject == null || !gameObject.TryGetComponent(out ReflectionProbe reflectionProbe))
            {
                return;
            }

            // customBakedTexture is set to some weird texture by default
            // it doesn't seem to be used by anything in-game so devs may have forgotten it?
            // (not destroying the component just in case something uses it)
            reflectionProbe.customBakedTexture = null;
            reflectionProbe.enabled = false;
        }
    }
}
