//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Avatar;
using UnityEngine;

namespace CustomAvatar.Lighting
{
    internal class TwoSidedLightingController : MonoBehaviour
    {
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        private void Start()
        {
            AddLight(Vector3.zero, Quaternion.Euler(135, 0, 0), LightType.Directional, new Color(1, 1, 1), 0.8f, 25); // front
            AddLight(Vector3.zero, Quaternion.Euler(45, 0, 0), LightType.Directional, new Color(1, 1, 1), 0.8f, 25); // back
        }
        
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
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadowStrength = 1;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.shadowNearPlane = 0.2f;

            container.transform.SetParent(transform, false);
            container.transform.position = position;
            container.transform.rotation = rotation;
        }
    }
}
