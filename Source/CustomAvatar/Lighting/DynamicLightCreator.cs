//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Configuration;
using CustomAvatar.Lighting.Lights;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using IPA.Utilities;
using UnityEngine;
using Zenject;
using static RuntimeLightWithIds;

namespace CustomAvatar.Lighting
{
    internal class DynamicLightCreator
    {
        private const string kMainTexName = "_MainTex";
        private const string kWorldNoiseKeyword = "ENABLE_WORLD_NOISE";

        // the shader doesn't just reduce the intensity when ENABLE_WORLD_NOISE is present
        // so this is an approximation of the average relative intensity reduction
        private const float kWorldNoiseIntensityMultiplier = 0.3f;

        private static readonly int kMainTexNameId = Shader.PropertyToID(kMainTexName);

        private readonly ILogger<DynamicLightCreator> _logger;
        private readonly Settings _settings;
        private readonly LightIntensityData _lightIntensityData;
        private readonly DiContainer _container;

        private DynamicLightCreator(ILogger<DynamicLightCreator> logger, Settings settings, LightIntensityData lightIntensityData, DiContainer container)
        {
            _logger = logger;
            _settings = settings;
            _lightIntensityData = lightIntensityData;
            _container = container;
        }

        public void CreateExtraLight(Quaternion rotation, LightIntensitiesWithId[] lightIntensitiesWithIds, float intensity)
        {
            var go = new GameObject(nameof(MagicalNonexistentLightBecauseLightmappingIsBasedOnALightThatDoesNotExists));

            go.transform.rotation = rotation;

            _container.InstantiateComponent<MagicalNonexistentLightBecauseLightmappingIsBasedOnALightThatDoesNotExists>(go, new object[] { lightIntensitiesWithIds, intensity });
        }

        public void ConfigureTubeBloomPrePassLight(TubeBloomPrePassLight tubeBloomPrePassLight)
        {
            if (!_settings.lighting.environment.enabled)
            {
                return;
            }

            DynamicLineLight dynamicLight = _container.InstantiateComponent<DynamicLineLight>(new GameObject(nameof(DynamicLineLight)));
            dynamicLight.transform.SetParent(tubeBloomPrePassLight.transform, false);
            dynamicLight.lights.Add(new ApproximatedTubeBloomPrePassLight(tubeBloomPrePassLight, _settings.lighting.environment.intensity * _lightIntensityData.tubeBloomPrePassLight));

            Parametric3SliceSpriteController parametric3SliceSpriteController = tubeBloomPrePassLight.GetField<Parametric3SliceSpriteController, TubeBloomPrePassLight>("_dynamic3SliceSprite");
            ParametricBoxController parametricBoxController = tubeBloomPrePassLight.GetField<ParametricBoxController, TubeBloomPrePassLight>("_parametricBoxController");

            if (parametricBoxController)
            {
                MeshRenderer meshRenderer = parametricBoxController.GetField<MeshRenderer, ParametricBoxController>("_meshRenderer");
                float shaderIntensity;

                Material material = meshRenderer.material;
                string shaderName = material.shader.name;

                switch (shaderName)
                {
                    case "Custom/OpaqueNeonLight":
                        shaderIntensity = 1f;
                        break;

                    case "Custom/TransparentNeonLight":
                        shaderIntensity = material.HasKeyword(kWorldNoiseKeyword) ? kWorldNoiseIntensityMultiplier : 1f;
                        break;

                    default:
                        _logger.LogError($"Unexpected shader '{shaderName}'");
                        shaderIntensity = 0f;
                        break;
                }

                dynamicLight.lights.Add(new ApproximatedParametricBoxLight(parametricBoxController, _settings.lighting.environment.intensity * _lightIntensityData.parametricBoxLight * shaderIntensity));
            }

            if (parametric3SliceSpriteController)
            {
                MeshRenderer meshRenderer = parametric3SliceSpriteController.GetField<MeshRenderer, Parametric3SliceSpriteController>("_meshRenderer");

                if (meshRenderer == null || meshRenderer.material == null || !meshRenderer.material.HasProperty(kMainTexNameId) || meshRenderer.material.mainTexture == null)
                {
                    _logger.LogWarning($"{nameof(Parametric3SliceSpriteController)} has no {kMainTexName}");
                    return;
                }

                Material material = meshRenderer.material;
                string mainTextureName = material.mainTexture.name;
                float materialIntensity;

                switch (mainTextureName)
                {
                    case "LaserGlowSprite1":
                        materialIntensity = 1.5f;
                        break;

                    case "LaserGlowSprite":
                    case "LaserGlowSpritePyro":
                        materialIntensity = 1.2f;
                        break;

                    case "LaserGlowSpriteWithCore":
                        materialIntensity = 0.6f;
                        break;

                    case "LaserGlowSprite0":
                        materialIntensity = 0.3f;
                        break;

                    case "LaserGlowSpriteHalf":
                        materialIntensity = 0.15f;
                        break;

                    default:
                        _logger.LogError($"Unexpected main texture '{mainTextureName}'");
                        materialIntensity = 0f;
                        break;
                }

                if (material.HasKeyword(kWorldNoiseKeyword))
                {
                    _logger.LogTrace($"{material.name} has {kWorldNoiseKeyword}");
                    materialIntensity *= kWorldNoiseIntensityMultiplier;
                }

                dynamicLight.lights.Add(new ApproximatedParametric3SliceSpriteLight(parametric3SliceSpriteController, _settings.lighting.environment.intensity * _lightIntensityData.parametric3SliceSprite * materialIntensity));
            }
        }

        public void ConfigureDirectionalLight(DirectionalLight directionalLight)
        {
            if (!_settings.lighting.environment.enabled)
            {
                return;
            }

            DynamicDirectionalLight directionalUnityLight = _container.InstantiateComponent<DynamicDirectionalLight>(
                new GameObject(nameof(DynamicDirectionalLight)),
                new object[] { directionalLight, _settings.lighting.environment.intensity * _lightIntensityData.directionalLight });

            directionalUnityLight.transform.SetParent(directionalLight.transform, false);
        }

        public void ConfigureBloomPrePassBackgroundColorsGradient(BloomPrePassBackgroundColorsGradient bloomPrePassBackgroundColorsGradient)
        {
            if (!_settings.lighting.environment.enabled)
            {
                return;
            }

            DynamicBloomPrePassBackgroundColorsGradient dynamicBloomPrePassBackgroundColorsGradient = _container.InstantiateComponent<DynamicBloomPrePassBackgroundColorsGradient>(
                new GameObject(nameof(DynamicBloomPrePassBackgroundColorsGradient)),
                new object[] { bloomPrePassBackgroundColorsGradient, _lightIntensityData.ambient });

            dynamicBloomPrePassBackgroundColorsGradient.transform.SetParent(bloomPrePassBackgroundColorsGradient.transform, false);
        }
    }
}
