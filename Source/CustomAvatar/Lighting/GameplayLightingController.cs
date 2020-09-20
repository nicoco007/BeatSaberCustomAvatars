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

using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class GameplayLightingController : MonoBehaviour
    {
        // TODO this should be adjusted according to room config
        private static readonly Vector3 kOrigin = new Vector3(0, 1, 0);

        private ILogger<GameplayLightingController> _logger;
        private LightWithIdManager _lightManager;
        private ColorManager _colorManager;
        private PlayerController _playerController;
        private TwoSidedLightingController _twoSidedLightingController;

        private List<DynamicLight>[] _lights;
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, LightWithIdManager lightManager, ColorManager colorManager, PlayerController playerController, TwoSidedLightingController twoSidedLightingController)
        {
            _logger = loggerProvider.CreateLogger<GameplayLightingController>();
            _lightManager = lightManager;
            _colorManager = colorManager;
            _playerController = playerController;
            _twoSidedLightingController = twoSidedLightingController;

            _lightManager.didSetColorForIdEvent += OnSetColorForId;
        }

        private void Start()
        {
            _twoSidedLightingController.gameObject.SetActive(false);

            CreateLights();

            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberA), _playerController.leftSaber.transform);
            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberB), _playerController.rightSaber.transform);
        }

        private void Update()
        {
            for (int id = 0; id < _lights.Length; id++)
            {
                if (_lights[id] == null) continue;

                foreach (DynamicLight gameLight in _lights[id])
                {
                    if (!gameLight.tubeLight.isActiveAndEnabled) continue;

                    Vector3 position = gameLight.tubeLight.transform.position;
                    Vector3 up = (gameLight.tubeLight.transform.rotation * Vector3.up).normalized;

                    Vector3 projectionOnLight = Vector3.Project(position, up);
                    Vector3 perpendicularOriginToLight = position - projectionOnLight;

                    float sqrMinimumDistance = perpendicularOriginToLight.sqrMagnitude;

                    // the two ends of the light
                    Vector3 endA = position + (1.0f - gameLight.center) * gameLight.length * up; // end at +Y
                    Vector3 endB = position - gameLight.center * gameLight.length * up;          // end at -Y

                    // parallel to up
                    Vector3 distanceOnLineA = endA - perpendicularOriginToLight;
                    Vector3 distanceOnLineB = endB - perpendicularOriginToLight;

                    float xA = distanceOnLineA.magnitude * Mathf.Sign(Vector3.Dot(distanceOnLineA, up));
                    float xB = distanceOnLineB.magnitude * Mathf.Sign(Vector3.Dot(distanceOnLineB, up));

                    float intensity = Mathf.Abs(RelativeIntensityAlongLine(xB, sqrMinimumDistance) - RelativeIntensityAlongLine(xA, sqrMinimumDistance));

                    gameLight.intensity = intensity;
                    gameLight.rotation = Quaternion.LookRotation(kOrigin - (gameLight.tubeLight.transform.position + gameLight.offset));
                }
            }
        }

        private void OnDestroy()
        {
            _twoSidedLightingController.gameObject.SetActive(true);
        }

        #pragma warning restore IDE0051
        #endregion

        private void CreateLights()
        {
            List<LightWithId>[] lightsWithId = _lightManager.GetPrivateField<List<LightWithId>[]>("_lights");
            int maxLightId = _lightManager.GetPrivateField<int>("kMaxLightId");

            _lights = new List<DynamicLight>[maxLightId + 1];
            
            for (int id = 0; id < lightsWithId.Length; id++)
            {
                if (lightsWithId[id] == null) continue;

                foreach (LightWithId lightWithId in lightsWithId[id])
                {
                    foreach (TubeBloomPrePassLight tubeLight in lightWithId.GetComponentsInChildren<TubeBloomPrePassLight>())
                    {
                        Light light = new GameObject("DynamicLight").AddComponent<Light>();

                        light.type = LightType.Directional;
                        light.color = Color.black;
                        light.intensity = 0;
                        light.spotAngle = 45;
                        light.cullingMask = AvatarLayers.kAllLayersMask;

                        light.transform.parent = transform;
                        light.transform.position = Vector3.zero;
                        light.transform.rotation = Quaternion.identity;

                        if (_lights[id] == null)
                        {
                            _lights[id] = new List<DynamicLight>(10);
                        }

                        _lights[id].Add(new DynamicLight(tubeLight, light));
                    }
                }
            }

            _logger.Trace($"Created {_lights.Sum(l => l?.Count)} lights");
        }

        private void OnSetColorForId(int id, Color color)
        {
            if (_lights[id] == null) return;

            foreach (DynamicLight light in _lights[id])
            {
                if (light.tubeLight.isActiveAndEnabled)
                {
                    light.color = color;
                }
                else
                {
                    light.color = Color.black;
                    light.intensity = 0;
                }
            }
        }

        private float RelativeIntensityAlongLine(float x, float h2)
        {
            // integral is ∫ 1 / (1 + h^2 + x^2) dx = atan(x / sqrt(h^2 + 1)) / sqrt(h^2 + 1)
            float sqrt = Mathf.Sqrt(h2 + 1);
            return Mathf.Atan(x / sqrt) / sqrt;
        }

        private void AddPointLight(Color color, Transform parent)
        {
            Light light = new GameObject(parent.name + "Light").AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.intensity = 0.35f;
            light.shadows = LightShadows.Hard;
            light.range = 5;
            light.cullingMask = AvatarLayers.kAllLayersMask;

            light.transform.SetParent(parent, false);
            light.transform.localPosition = new Vector3(0, 0, 0.5f); // middle of saber
            light.transform.rotation = Quaternion.identity;
        }

        private class DynamicLight
        {
            public float intensity
            {
                get => _intensity;
                set
                {
                    _intensity = value;
                    UpdateLight();
                }
            }

            public Color color
            {
                get => _color;
                set
                {
                    _color = value;
                    UpdateLight();
                }
            }

            public Quaternion rotation
            {
                get => _unityLight.transform.rotation;
                set => _unityLight.transform.rotation = value;
            }

            public readonly TubeBloomPrePassLight tubeLight;
            public readonly float width;
            public readonly float length;
            public readonly float center;
            public readonly Vector3 offset;

            private readonly Light _unityLight;
            private readonly float _colorAlphaMultiplier;
            private readonly float _bloomFogIntensityMultiplier;

            private float _intensity;
            private Color _color;

            public DynamicLight(TubeBloomPrePassLight tubeLight, Light unityLight)
            {
                this.tubeLight = tubeLight;
                this._unityLight = unityLight;

                width = tubeLight.GetPrivateField<float>("_width");
                length = tubeLight.GetPrivateField<float>("_length");
                center = tubeLight.GetPrivateField<float>("_center");

                _colorAlphaMultiplier = tubeLight.GetPrivateField<float>("_colorAlphaMultiplier");
                _bloomFogIntensityMultiplier = tubeLight.GetPrivateField<float>("_bloomFogIntensityMultiplier");

                offset = (0.5f - center) * length * Vector3.up;
            }

            private void UpdateLight()
            {
                _unityLight.color = _color;
                _unityLight.intensity = _intensity * width * _colorAlphaMultiplier * _bloomFogIntensityMultiplier * _color.a * 3f;
            }
        }
    }
}
