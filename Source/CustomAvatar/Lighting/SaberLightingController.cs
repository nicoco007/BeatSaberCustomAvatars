using CustomAvatar.Avatar;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class SaberLightingController : IInitializable
    {
        private SaberManager _saberManager;
        private ColorManager _colorManager;

        [Inject]
        private void Inject(SaberManager saberManager, ColorManager colorManager)
        {
            _saberManager = saberManager;
            _colorManager = colorManager;
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
            light.intensity = 0.35f;
            light.shadows = LightShadows.Hard;
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
