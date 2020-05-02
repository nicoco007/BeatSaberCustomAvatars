using System.Collections.Generic;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Lighting
{
    internal class GameplayLightingController : MonoBehaviour
    {
        private ILogger _logger;
        private LightWithIdManager _lightManager;

        private List<Light>[] _lights;
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, LightWithIdManager lightManager)
        {
            _logger = loggerProvider.CreateLogger<GameplayLightingController>();
            _lightManager = lightManager;

            _lightManager.didSetColorForIdEvent += OnSetColorForId;
        }
        private void Start()
        {
            List<LightWithId>[] lightsWithId = _lightManager.GetPrivateField<LightWithIdManager, List<LightWithId>[]>("_lights");
            int maxLightId = _lightManager.GetPrivateField<LightWithIdManager, int>("kMaxLightId");
            Vector3 origin = new Vector3(0, 1, 0);

            _lights = new List<Light>[maxLightId + 1];

            for (int id = 0; id < lightsWithId.Length; id++)
            {
                if (lightsWithId[id] == null) continue;

                foreach (LightWithId lightWithId in lightsWithId[id])
                {
                    Vector3 direction = (lightWithId.transform.position - origin);

                    var light = new GameObject("DynamicLight").AddComponent<Light>();
                    
                    light.type = LightType.Directional;
                    light.color = Color.black;
                    light.shadows = LightShadows.None; // shadows murder fps since there's so many lights being added
                    light.renderMode = LightRenderMode.Auto;
                    light.intensity = 5f * (1 / direction.magnitude);
                    light.spotAngle = 45;

                    _logger.Info("Intensity: " + light.intensity);
                    
                    light.transform.SetParent(transform);
                    light.transform.position = direction.normalized * 15;
                    light.transform.rotation = Quaternion.LookRotation(-direction);

                    if (_lights[id] == null)
                    {
                        _lights[id] = new List<Light>(10);
                    }

                    _lights[id].Add(light);
                }
            }
        }
        
        // ReSharper disable UnusedMember.Local
        #pragma warning disable IDE0051
        #endregion

        private void OnSetColorForId(int id, Color color)
        {
            if (_lights[id] == null) return;

            _logger.Info("Color: " + color);

            foreach (Light light in _lights[id])
            {
                light.color = color;
                light.intensity = color.a;
            }
        }
    }
}
