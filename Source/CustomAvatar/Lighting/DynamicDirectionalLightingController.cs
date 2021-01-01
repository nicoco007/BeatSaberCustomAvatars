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

using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicDirectionalLightingController : MonoBehaviour
    {
        private ILogger<DynamicDirectionalLightingController> _logger;
        private LightWithIdManager _lightManager;

        private List<(DirectionalLight, Light)> _directionalLights;
        
        #region Behaviour Lifecycle

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, LightWithIdManager lightManager)
        {
            name = nameof(DynamicDirectionalLightingController);

            _logger = loggerProvider.CreateLogger<DynamicDirectionalLightingController>();
            _lightManager = lightManager;
        }

        private void Start()
        {
            _lightManager.didChangeSomeColorsThisFrameEvent += OnChangedSomeColorsThisFrame;

            CreateLights();
        }

        private void OnDestroy()
        {
            _lightManager.didChangeSomeColorsThisFrameEvent -= OnChangedSomeColorsThisFrame;
        }

        #endregion

        private void CreateLights()
        {
            _directionalLights = new List<(DirectionalLight, Light)>();

            foreach (var directionalLight in DirectionalLight.lights.OrderBy(l => l.transform.position.sqrMagnitude))
            {
                Light light = new GameObject($"DynamicDirectionalLight({directionalLight.name})").AddComponent<Light>();

                light.type = LightType.Directional;
                light.color = directionalLight.color;
                light.intensity = 1;
                light.cullingMask = AvatarLayers.kAllLayersMask;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1;
                light.renderMode = LightRenderMode.Auto;

                light.transform.parent = transform;
                light.transform.position = Vector3.zero;
                light.transform.rotation = directionalLight.transform.rotation;

                _directionalLights.Add((directionalLight, light));
            }

            _logger.Trace($"Created {_directionalLights.Count} DynamicDirectionalLights");
        }

        private void OnChangedSomeColorsThisFrame()
        {
            foreach ((DirectionalLight directionalLight, Light light) in _directionalLights)
            {
                light.color = directionalLight.color;
                light.intensity = directionalLight.intensity * 0.5f;
            }
        }
    }
}
