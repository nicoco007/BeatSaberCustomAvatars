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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Lighting.Lights;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using LightIntensitiesWithId = RuntimeLightWithIds.LightIntensitiesWithId;

namespace CustomAvatar.Lighting
{
    internal class AdditionalEnvironmentLightingController : MonoBehaviour
    {
        private ILogger<AdditionalEnvironmentLightingController> _logger;
        private DiContainer _container;
        private SceneDecoratorContext _environmentDecoratorContext;

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(ILogger<AdditionalEnvironmentLightingController> logger, DiContainer container, List<SceneDecoratorContext> decoratorContexts)
        {
            _logger = logger;
            _container = container;
            _environmentDecoratorContext = decoratorContexts.Where(c => c.DecoratedContractName == "Environment").FirstOrDefault();
        }

        private void Start()
        {
            string environmentName = _environmentDecoratorContext.gameObject.scene.name;

            _logger.LogInformation($"Handling additional lights for {environmentName}");

            switch (environmentName)
            {
                case "InterscopeEnvironment":
                    HandleInterscopeEnvironment();
                    break;
            }
        }

        private void HandleInterscopeEnvironment()
        {
            // These lights take into account the "E" LightmapLightWithIds that are based on a light that's been removed.
            CreateAdditionalLight(0.5f, Quaternion.Euler(135, 0, 0), new[]
            {
                new LightIntensitiesWithId(5, 1f),
                new LightIntensitiesWithId(6, 0.7f),
                new LightIntensitiesWithId(7, 0.7f),
            });

            CreateAdditionalLight(0.5f, Quaternion.Euler(45, 0, 0), new[]
            {
                new LightIntensitiesWithId(5, 1f),
                new LightIntensitiesWithId(6, 0.7f),
                new LightIntensitiesWithId(7, 0.7f),
            });
        }

        private void CreateAdditionalLight(float intensity, Quaternion rotation, LightIntensitiesWithId[] lightIntensitiesWithId)
        {
            var lightGameObject = new GameObject("AdditionalLight");

            Transform lightTransform = lightGameObject.transform;
            lightTransform.SetParent(transform, false);
            lightTransform.rotation = rotation;

            Light light = lightGameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadows = LightShadows.None;

            _container.InstantiateComponent<MagicalNonexistentLightBecauseLightmappingIsBasedOnALightThatDoesNotExists>(lightGameObject, new object[] { light, intensity, lightIntensitiesWithId });
        }
    }
}
