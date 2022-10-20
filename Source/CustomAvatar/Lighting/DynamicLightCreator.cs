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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Lighting.Lights;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using CustomAvatar.Zenject.Internal;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicLightCreator : IInitializable, IDisposable
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

        public void Initialize()
        {
            // we have to do this because there is currently a bug with Affinity patches that prevents targeting a method with no parameters if it has overloads with parameters
            ZenjectHelper.installInstallers += OnInstallInstallers;
        }

        public void Dispose()
        {
            ZenjectHelper.installInstallers -= OnInstallInstallers;
        }

        private void OnInstallInstallers(Context context)
        {
            int count = 0;
            var stopwatch = Stopwatch.StartNew();

            switch (context)
            {
                case SceneContext sceneContext:
                    count = ProcessSceneLights(sceneContext.gameObject.scene);
                    break;

                case SceneDecoratorContext sceneDecoratorContext:
                    count = ProcessSceneLights(sceneDecoratorContext.gameObject.scene);
                    break;

                case GameObjectContext gameObjectContext:
                    count = ProcessTransform(gameObjectContext.transform);
                    break;

                default:
                    _logger.LogWarning("Unexpected context type " + context.GetType().Name);
                    break;
            }

            _logger.LogInformation($"Created {count} lights for context {context.name} ({context.GetType().Name} on {context.gameObject.scene.name}) in {(float)stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond:0.000} ms");
        }

        private int ProcessSceneLights(Scene scene)
        {
            int count = 0;

            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                count += ProcessTransform(gameObject.transform);
            }

            return count;
        }

        private int ProcessTransform(Transform transform)
        {
            if (transform.TryGetComponent(out TubeBloomPrePassLight tubeBloomPrePassLight))
            {
                ConfigureTubeBloomPrePassLight(tubeBloomPrePassLight);
                return 1;
            }
            else if (transform.TryGetComponent(out Parametric3SliceSpriteController parametric3SliceSpriteController))
            {
                ConfigureParametric3SliceSpriteLight(parametric3SliceSpriteController);
                return 1;
            }
            else if (transform.TryGetComponent(out DirectionalLight directionalLight))
            {
                ConfigureDirectionalLight(directionalLight);
                return 1;
            }
            else if (transform.TryGetComponent(out BloomPrePassBackgroundColorsGradient bloomPrePassBackgroundColorsGradient))
            {
                ConfigureBloomPrePassBackgroundColorsGradient(bloomPrePassBackgroundColorsGradient);
                return 1;
            }
            else if (transform.TryGetComponent(out SpriteLightWithId spriteLightWithId))
            {
                ConfigureSpriteLight(spriteLightWithId);
                return 1;
            }
            else if (!transform.TryGetComponent(out GameObjectContext _))
            {
                int count = 0;

                for (int i = 0; i < transform.childCount; ++i)
                {
                    count += ProcessTransform(transform.GetChild(i));
                }

                return count;
            }

            return 0;
        }

        private void ConfigureTubeBloomPrePassLight(TubeBloomPrePassLight tubeBloomPrePassLight)
        {
            Parametric3SliceSpriteController parametric3SliceSpriteController = tubeBloomPrePassLight.GetField<Parametric3SliceSpriteController, TubeBloomPrePassLight>("_dynamic3SliceSprite");
            ParametricBoxController parametricBoxController = tubeBloomPrePassLight.GetField<ParametricBoxController, TubeBloomPrePassLight>("_parametricBoxController");

            DynamicTubeBloomPrePassLight dynamicLight = _container.InstantiateComponent<DynamicTubeBloomPrePassLight>(CreateLightObject(nameof(DynamicTubeBloomPrePassLight)));
            dynamicLight.transform.SetParent(tubeBloomPrePassLight.transform, false);
            dynamicLight.Init(
                new ApproximatedTubeBloomPrePassLight(tubeBloomPrePassLight, _settings.lighting.environment.intensity * _lightIntensityData.tubeBloomPrePassLight),
                ConfigureParametric3SliceSpriteLight(parametric3SliceSpriteController),
                ConfigureParametricBoxLight(parametricBoxController));
        }

        private string GetTransformPath(Transform transform)
        {
            var parts = new List<string>();

            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }

            parts.Reverse();

            return string.Join("/", parts);
        }

        private ApproximatedParametric3SliceSpriteLight ConfigureParametric3SliceSpriteLight(Parametric3SliceSpriteController parametric3SliceSpriteController)
        {
            if (parametric3SliceSpriteController == null)
            {
                return null;
            }

            MeshRenderer meshRenderer = parametric3SliceSpriteController.GetField<MeshRenderer, Parametric3SliceSpriteController>("_meshRenderer");

            if (meshRenderer == null)
            {
                _logger.LogTrace($"{nameof(Parametric3SliceSpriteController)} on '{GetTransformPath(parametric3SliceSpriteController.transform)}' has no mesh renderer");
                return null;
            }

            if (meshRenderer == null || meshRenderer.material == null || !meshRenderer.material.HasProperty(kMainTexNameId) || meshRenderer.material.mainTexture == null)
            {
                _logger.LogTrace($"{nameof(Parametric3SliceSpriteController)} on '{GetTransformPath(meshRenderer.transform)}' has no {kMainTexName}");
                return null;
            }

            Material material = meshRenderer.material;
            string mainTextureName = material.mainTexture.name;
            float materialIntensity;

            switch (mainTextureName)
            {
                case "LaserGlowSprite1":
                    materialIntensity = 1.2f;
                    break;

                case "LaserGlowSprite":
                case "LaserGlowSprite2":
                case "LaserGlowSpritePyro":
                    materialIntensity = 1f;
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
                materialIntensity *= kWorldNoiseIntensityMultiplier;
            }

            return new ApproximatedParametric3SliceSpriteLight(parametric3SliceSpriteController, _settings.lighting.environment.intensity * _lightIntensityData.parametric3SliceSprite * materialIntensity);
        }

        private ApproximatedParametricBoxLight ConfigureParametricBoxLight(ParametricBoxController parametricBoxController)
        {
            if (parametricBoxController == null)
            {
                return null;
            }

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

            return new ApproximatedParametricBoxLight(parametricBoxController, _settings.lighting.environment.intensity * _lightIntensityData.parametricBoxLight * shaderIntensity);
        }

        private void ConfigureDirectionalLight(DirectionalLight directionalLight)
        {
            DynamicDirectionalLight dynamicDirectionalLight = _container.InstantiateComponent<DynamicDirectionalLight>(CreateLightObject(nameof(DynamicDirectionalLight)));
            dynamicDirectionalLight.transform.SetParent(directionalLight.transform, false);
            dynamicDirectionalLight.Init(directionalLight, _settings.lighting.environment.intensity * _lightIntensityData.directionalLight);
        }

        private void ConfigureBloomPrePassBackgroundColorsGradient(BloomPrePassBackgroundColorsGradient bloomPrePassBackgroundColorsGradient)
        {
            DynamicBloomPrePassBackgroundColorsGradient dynamicBloomPrePassBackgroundColorsGradient =
                _container.InstantiateComponent<DynamicBloomPrePassBackgroundColorsGradient>(new GameObject(nameof(DynamicBloomPrePassBackgroundColorsGradient)));

            dynamicBloomPrePassBackgroundColorsGradient.transform.SetParent(bloomPrePassBackgroundColorsGradient.transform, false);
            dynamicBloomPrePassBackgroundColorsGradient.Init(bloomPrePassBackgroundColorsGradient, _lightIntensityData.ambient);
        }

        private void ConfigureSpriteLight(SpriteLightWithId spriteLightWithId)
        {
            TubeBloomPrePassLight tubeBloomPrePassLight = spriteLightWithId.GetComponentInChildren<TubeBloomPrePassLight>();

            DynamicSpriteLight dynamicSpriteLight = _container.InstantiateComponent<DynamicSpriteLight>(CreateLightObject(nameof(DynamicSpriteLight)));
            dynamicSpriteLight.transform.SetParent(spriteLightWithId.transform, false);
            dynamicSpriteLight.Init(spriteLightWithId, tubeBloomPrePassLight, _lightIntensityData.spriteLight);
        }

        private GameObject CreateLightObject(string name)
        {
            var gameObject = new GameObject(name);
            Light light = gameObject.AddComponent<Light>();

            light.type = LightType.Directional;
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadows = LightShadows.None;
            light.intensity = 0;

            return gameObject;
        }
    }
}
