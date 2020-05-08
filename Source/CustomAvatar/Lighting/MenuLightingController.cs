using UnityEngine;

namespace CustomAvatar.Lighting
{
    internal class MenuLightingController : MonoBehaviour
    {
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        private void Start()
        {
            AddLight(Vector3.zero, Quaternion.Euler(135, 0, 0), LightType.Directional, new Color(0.8f, 0.9f, 1.000f), 1.0f, 25); // front
            AddLight(Vector3.zero, Quaternion.Euler(45, 0, 0), LightType.Directional, new Color(0.8f, 0.9f, 1.000f), 1.0f, 25); // back
        }

        private void OnDestroy()
        {
            Destroy(gameObject);
        }
        
        // ReSharper disable UnusedMember.Local
        #pragma warning disable IDE0051
        #endregion

        private void AddLight(Vector3 position, Quaternion rotation, LightType type, Color color, float intensity, float range)
        {
            var container = new GameObject();
            var light = container.AddComponent<Light>();

            light.type = type;
            light.color = color;
            light.shadows = LightShadows.Soft;
            light.intensity = intensity;
            light.range = range;
            light.cullingMask = (1 << AvatarLayers.kOnlyInFirstPerson) | (1 << AvatarLayers.kOnlyInThirdPerson) | (1 << AvatarLayers.kAlwaysVisible);

            container.transform.SetParent(transform, false);
            container.transform.position = position;
            container.transform.rotation = rotation;
        }
    }
}
