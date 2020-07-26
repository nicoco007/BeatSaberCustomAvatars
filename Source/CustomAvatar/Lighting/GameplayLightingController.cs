using System.Collections.Generic;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class GameplayLightingController : MonoBehaviour
    {
        private readonly Vector3 kOrigin = new Vector3(0, 1, 0);

        private LightWithIdManager _lightManager;
        private ColorManager _colorManager;
        private PlayerController _playerController;

        private List<Light>[] _lights;
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(LightWithIdManager lightManager, ColorManager colorManager, PlayerController playerController)
        {
            _lightManager = lightManager;
            _colorManager = colorManager;
            _playerController = playerController;

            _lightManager.didSetColorForIdEvent += OnSetColorForId;
        }

        private void Start()
        {
            List<LightWithId>[] lightsWithId = _lightManager.GetPrivateField<List<LightWithId>[]>("_lights");
            int maxLightId = _lightManager.GetPrivateField<int>("kMaxLightId");

            _lights = new List<Light>[maxLightId + 1];

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
                    light.renderMode = LightRenderMode.Auto;
                    light.intensity = 5f * (1 / direction.magnitude);
                    light.spotAngle = 45;
                    light.cullingMask = (1 << AvatarLayers.kOnlyInFirstPerson) | (1 << AvatarLayers.kOnlyInThirdPerson) | (1 << AvatarLayers.kAlwaysVisible);
                    
                    light.transform.SetParent(lightWithId.transform);
                    light.transform.localPosition = Vector3.zero;
                    light.transform.rotation = Quaternion.LookRotation(direction);

                    if (_lights[id] == null)
                    {
                        _lights[id] = new List<Light>(10);
                    }

                    _lights[id].Add(light);
                }
            }

            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberA), _playerController.leftSaber.transform);
            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberB), _playerController.rightSaber.transform);
        }

        private void Update()
        {
            foreach (List<Light> lights in _lights)
            {
                if (lights == null) continue;

                foreach (Light light in lights)
                {
                    light.transform.rotation = Quaternion.LookRotation(kOrigin - light.transform.position);
                }
            }
        }

        // ReSharper disable UnusedMember.Local
        #pragma warning disable IDE0051
        #endregion

        private void OnSetColorForId(int id, Color color)
        {
            if (_lights[id] == null) return;

            foreach (Light light in _lights[id])
            {
                light.color = color;
                light.intensity = color.a;
            }
        }

        private void AddPointLight(Color color, Transform parent)
        {
            Light light = new GameObject(parent.name + "Light").AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1;
            light.shadows = LightShadows.Hard;
            light.range = 5;
            light.renderMode = LightRenderMode.ForcePixel;
            light.cullingMask = (1 << AvatarLayers.kOnlyInFirstPerson) | (1 << AvatarLayers.kOnlyInThirdPerson) | (1 << AvatarLayers.kAlwaysVisible);

            light.transform.SetParent(parent, false);
            light.transform.localPosition = new Vector3(0, 0, 0.5f); // middle of saber
            light.transform.rotation = Quaternion.identity;
        }
    }
}
