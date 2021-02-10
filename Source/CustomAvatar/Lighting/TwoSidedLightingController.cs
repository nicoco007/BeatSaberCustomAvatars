//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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
        private static readonly Color kAmbientColor = new Color(0.8f, 0.8f, 1);

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        private void Start()
        {
            AddLight(Quaternion.Euler(135, 0, 0), kAmbientColor, 0.9f); // front
            AddLight(Quaternion.Euler(45, 0, 0), kAmbientColor, 0.9f); // back
        }
        
        #pragma warning disable IDE0051
        #endregion

        private void AddLight(Quaternion rotation, Color color, float intensity)
        {
            var container = new GameObject("Light");
            var light = container.AddComponent<Light>();

            light.type = LightType.Directional;
            light.color = color;
            light.shadows = LightShadows.Soft;
            light.intensity = intensity;
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadowStrength = 1;

            container.transform.SetParent(transform, false);
            container.transform.position = Vector3.zero;
            container.transform.rotation = rotation;
        }
    }
}
