using CustomAvatar.Configuration;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class MenuLightingController : MonoBehaviour
    {
        private GameScenesHelper _gameScenesHelper;
        private Settings _settings;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(GameScenesHelper gameScenesHelper, Settings settings)
        {
            _gameScenesHelper = gameScenesHelper;
            _settings = settings;

            if (settings.lighting.castShadows)
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = settings.lighting.shadowResolution;
                QualitySettings.shadowDistance = 25;
            }

            _gameScenesHelper.transitionDidFinish += OnTransitionDidFinish;
        }

        private void Start()
        {
            AddLight(Vector3.zero, Quaternion.Euler(135, 0, 0), LightType.Directional, new Color(0.8f, 0.9f, 1.000f), 1.0f, 25); // front
            AddLight(Vector3.zero, Quaternion.Euler(45, 0, 0), LightType.Directional, new Color(0.8f, 0.9f, 1.000f), 1.0f, 25); // back
        }

        private void OnDestroy()
        {
            _gameScenesHelper.transitionDidFinish -= OnTransitionDidFinish;
        }
        
        // ReSharper disable UnusedMember.Local
        #pragma warning disable IDE0051
        #endregion

        private void OnTransitionDidFinish(BeatSaberScene scene, DiContainer container)
        {
            if (_settings.lighting.enableDynamicLighting && scene == BeatSaberScene.Game)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void AddLight(Vector3 position, Quaternion rotation, LightType type, Color color, float intensity, float range)
        {
            var container = new GameObject();
            var light = container.AddComponent<Light>();

            light.type = type;
            light.color = color;
            light.shadows = LightShadows.Soft;
            light.intensity = intensity;
            light.range = range;
            light.cullingMask = AvatarLayers.kAllLayersMask;

            container.transform.SetParent(transform, false);
            container.transform.position = position;
            container.transform.rotation = rotation;
        }
    }
}
