using CustomAvatar.Avatar;
using UnityEngine;

namespace CustomAvatar.Lighting
{
    internal class LogoLighting : MonoBehaviour
    {
        public void Awake()
        {
            var lightGameObject = new GameObject("DirectionalLight");
            Transform lightTransform = lightGameObject.transform;
            lightTransform.parent = transform;
            lightTransform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(150, 0, 0));

            Light light = lightGameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.10f, 0.68f, 1); // just a little bit of red from the logo
            light.intensity = 0.35f;
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1;
            light.renderMode = LightRenderMode.ForcePixel;
        }
    }
}
