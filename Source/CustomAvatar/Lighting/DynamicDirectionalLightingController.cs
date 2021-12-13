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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicDirectionalLightingController : MonoBehaviour
    {
        private static readonly Vector3 kOrigin = new Vector3(0, 1, 0);

        private ILogger<DynamicDirectionalLightingController> _logger;
        private Settings _settings;

        private List<(DirectionalLight, Light)> _directionalLights;

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(ILogger<DynamicDirectionalLightingController> logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        internal void Start()
        {
            CreateLights();
        }

        internal void LateUpdate()
        {
            foreach ((DirectionalLight directionalLight, Light light) in _directionalLights)
            {
                // the game's "directional lights" act more like Unity's point lights with a radius and a falloff
                float distance = Vector3.Distance(directionalLight.transform.position, kOrigin);
                float intensityFalloff = Mathf.Max((directionalLight.radius - distance) / directionalLight.radius, 0);

                light.color = directionalLight.color;
                light.intensity = intensityFalloff * directionalLight.intensity * _settings.lighting.environment.intensity * 0.5f;
                light.transform.rotation = directionalLight.transform.rotation;
            }
        }

        #endregion

        private void CreateLights()
        {
            _directionalLights = new List<(DirectionalLight, Light)>();

            foreach (DirectionalLight directionalLight in DirectionalLight.lights)
            {
                Light light = new GameObject($"DynamicDirectionalLight({directionalLight.name})").AddComponent<Light>();

                light.type = LightType.Directional;
                light.color = directionalLight.color;
                light.intensity = 0;
                light.cullingMask = AvatarLayers.kAllLayersMask;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1;
                light.renderMode = LightRenderMode.Auto;

                light.transform.parent = transform;
                light.transform.SetPositionAndRotation(Vector3.zero, directionalLight.transform.rotation);

                _directionalLights.Add((directionalLight, light));
            }

            _logger.Trace($"Created {_directionalLights.Count} DynamicDirectionalLights");
        }
    }
}
