using System;
using System.Linq;
using CustomAvatar.Avatar;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class MenuLightingCreator : IInitializable, IDisposable
    {
        private readonly LightWithIdManager _lightWithIdManager;
        private readonly MenuEnvironmentManager _menuEnvironmentManager;

        private Light _light;

        public MenuLightingCreator(LightWithIdManager lightWithIdManager, MenuEnvironmentManager menuEnvironmentManager)
        {
            _lightWithIdManager = lightWithIdManager;
            _menuEnvironmentManager = menuEnvironmentManager;
        }

        public void Initialize()
        {
            var lightObject = new GameObject("Menu Light");
            Transform lightTransform = lightObject.transform;

            lightObject.transform.SetParent(_menuEnvironmentManager.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(30, 180, 0);

            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = 1;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 1;
            _light.renderMode = LightRenderMode.ForcePixel;

            _lightWithIdManager.didChangeSomeColorsThisFrameEvent += UpdateLightColor;

            UpdateLightColor();
        }

        public void Dispose()
        {
            _lightWithIdManager.didChangeSomeColorsThisFrameEvent -= UpdateLightColor;
        }

        private void UpdateLightColor()
        {
            _light.color = DirectionalLight.lights.Aggregate(Color.black, (acc, l) => acc + l.color * l.intensity) / DirectionalLight.lights.Sum(l => l.intensity);
        }
    }
}
