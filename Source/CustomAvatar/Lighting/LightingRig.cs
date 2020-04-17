using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Lighting
{
    internal class LightingRig : MonoBehaviour
    {
        internal void AddLight(Settings.LightDefinition definition)
        {
            var container = new GameObject();
            var light = container.AddComponent<Light>();

            light.type = definition.type;
            light.color = definition.color;
            light.shadows = LightShadows.Soft;
            light.renderMode = LightRenderMode.ForcePixel;
            light.intensity = definition.intensity;
            light.spotAngle = definition.spotAngle;
            light.range = definition.range;

            container.transform.position = definition.position;
            container.transform.rotation = Quaternion.Euler(definition.rotation);
            container.transform.SetParent(transform, false);
        }
    }
}
