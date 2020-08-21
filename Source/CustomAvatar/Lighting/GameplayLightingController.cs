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

        private List<GameLight>[] _lights;
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

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

                foreach (GameLight gameLight in _lights[id])
                {
                    gameLight.light.transform.LookAt(kOrigin - gameLight.lightWithId.transform.position);
                }
            }
        }

        private void OnDestroy()
        {
            _twoSidedLightingController.gameObject.SetActive(true);
        }

        // ReSharper disable UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private void CreateLights()
        {
            List<LightWithId>[] lightsWithId = _lightManager.GetPrivateField<List<LightWithId>[]>("_lights");
            int maxLightId = _lightManager.GetPrivateField<int>("kMaxLightId");

            _lights = new List<GameLight>[maxLightId + 1];
            
            for (int id = 0; id < lightsWithId.Length; id++)
            {
                if (lightsWithId[id] == null) continue;

                foreach (LightWithId lightWithId in lightsWithId[id])
                {
                    Vector3 direction = kOrigin - lightWithId.transform.position;

                    var light = new GameObject("DynamicLight").AddComponent<Light>();

                    light.type = LightType.Directional;
                    light.color = Color.black;
                    light.shadows = LightShadows.None; // shadows murder fps since there's so many lights being added
                    light.renderMode = LightRenderMode.ForcePixel; // reduce performance toll
                    light.intensity = 0;
                    light.spotAngle = 45;
                    light.cullingMask = AvatarLayers.kAllLayersMask;

                    light.transform.SetParent(transform);
                    light.transform.position = Vector3.zero;
                    light.transform.rotation = Quaternion.identity;

                    if (_lights[id] == null)
                    {
                        _lights[id] = new List<GameLight>(10);
                    }

                    _lights[id].Add(new GameLight(lightWithId, light));
                }
            }

            _logger.Trace($"Created {_lights.Sum(l => l?.Count)} lights");
        }

        private void OnSetColorForId(int id, Color color)
        {
            if (_lights[id] == null) return;

            foreach (GameLight light in _lights[id])
            {
                light.light.color = color;
                light.light.intensity = color.a;
            }
        }

        private void AddPointLight(Color color, Transform parent)
        {
            Light light = new GameObject(parent.name + "Light").AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.intensity = 0.35f;
            light.shadows = LightShadows.Hard;
            light.range = 5;
            light.renderMode = LightRenderMode.ForcePixel;
            light.cullingMask = AvatarLayers.kAllLayersMask;

            light.transform.SetParent(parent, false);
            light.transform.localPosition = new Vector3(0, 0, 0.5f); // middle of saber
            light.transform.rotation = Quaternion.identity;
        }

        private struct GameLight
        {
            public readonly LightWithId lightWithId;
            public readonly Light light;
            public readonly float magnitude;

            public GameLight(LightWithId lightWithId, Light light)
            {
                this.lightWithId = lightWithId;
                this.light = light;

                // this doesn't really make sense physically but it works out nicer than sqrMagnitude
                magnitude = (kOrigin - lightWithId.transform.position).magnitude;
            }
        }
    }
}
