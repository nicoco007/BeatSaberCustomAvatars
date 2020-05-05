using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class GameplayLightingController : MonoBehaviour
    {
        private LightWithIdManager _lightManager;

        private List<Light>[] _lights;
        private Vector3 _origin = new Vector3(0, 1, 0);
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(LightWithIdManager lightManager)
        {
            _lightManager = lightManager;

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
                    Vector3 direction = _origin - lightWithId.transform.position;

                    var light = new GameObject("DynamicLight").AddComponent<Light>();
                    
                    light.type = LightType.Directional;
                    light.color = Color.black;
                    light.shadows = LightShadows.None; // shadows murder fps since there's so many lights being added
                    light.renderMode = LightRenderMode.Auto;
                    light.intensity = 5f * (1 / direction.magnitude);
                    light.spotAngle = 45;
                    
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
        }

        private void Update()
        {
            foreach (List<Light> lights in _lights)
            {
                foreach (Light light in lights)
                {
                    light.transform.rotation = Quaternion.LookRotation(_origin - light.transform.position);
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
    }
}
