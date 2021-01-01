using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class SaberLightingController : IInitializable
    {
        private SaberManager _saberManager;
        private ColorManager _colorManager;
        private Settings _settings;

        [Inject]
        private void Inject(SaberManager saberManager, ColorManager colorManager, Settings settings)
        {
            _saberManager = saberManager;
            _colorManager = colorManager;
            _settings = settings;
        }

        public void Initialize()
        {
            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberA), _saberManager.leftSaber.transform);
            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberB), _saberManager.rightSaber.transform);
        }

        private void AddPointLight(Color color, Transform parent)
        {
            Light light = new GameObject(parent.name + "Light").AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.intensity = 0.5f;
            light.shadows = _settings.lighting.shadowLevel == ShadowLevel.Directional ? LightShadows.None : LightShadows.Soft;
            light.shadowStrength = 1;
            light.renderMode = LightRenderMode.ForcePixel; // point lights don't do much when vertex rendered
            light.bounceIntensity = 0;
            light.range = 5;
            light.cullingMask = AvatarLayers.kAllLayersMask;

            light.transform.SetParent(parent, false);
            light.transform.localPosition = new Vector3(0, 0, 0.5f); // middle of saber
            light.transform.rotation = Quaternion.identity;
        }
    }
}
