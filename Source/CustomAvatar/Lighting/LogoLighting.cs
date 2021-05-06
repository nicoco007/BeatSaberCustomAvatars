using CustomAvatar.Avatar;
using UnityEngine;

namespace CustomAvatar.Lighting
{
    internal class LogoLighting : MonoBehaviour
    {
        public void Awake()
        {
            GameObject lightGameObject = new GameObject("DirectionalLight");
            Transform lightTransform = lightGameObject.transform;
            lightTransform.parent = transform;
            lightTransform.position = Vector3.zero;
            lightTransform.rotation = Quaternion.Euler(135, 0, 0);

            Light light = lightGameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.15f, 0.68f, 1); // just a little bit of red from the logo
            light.intensity = 0.5f;
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1;
            light.renderMode = LightRenderMode.ForcePixel;
        }
    }
}
