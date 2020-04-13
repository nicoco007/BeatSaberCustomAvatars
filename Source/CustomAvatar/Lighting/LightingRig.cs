using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Lighting
{
    internal class LightingRig
    {
        private readonly GameObject _root = new GameObject(nameof(LightingRig));

        public LightingRig(Transform parent = null)
        {
            if (parent) _root.transform.SetParent(parent);
        }

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
            container.transform.SetParent(_root.transform, false);
        }
    }
}
