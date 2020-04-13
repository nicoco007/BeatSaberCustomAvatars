using System.Collections.Generic;
using UnityEngine;

namespace CustomAvatar.Lighting
{
    public class LightingRig
    {
        private readonly GameObject _root = new GameObject(nameof(LightingRig));

        public LightingRig(Transform parent = null)
        {
            if (parent) _root.transform.SetParent(parent);
        }

        public void AddLight(Quaternion rotation)
        {
            var container = new GameObject();
            var light = container.AddComponent<Light>();

            light.type = LightType.Directional;
            light.color = Color.white;
            light.shadows = LightShadows.Soft;

            container.transform.position = Vector3.zero;
            container.transform.rotation = rotation;
            container.transform.SetParent(_root.transform, false);
        }
    }
}
