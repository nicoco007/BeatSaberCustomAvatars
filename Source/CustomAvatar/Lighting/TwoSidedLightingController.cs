//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class TwoSidedLightingController : MonoBehaviour
    {
        private static readonly Color kAmbientColor = new Color(0.8f, 0.8f, 1);

        private Settings _settings;

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(Settings settings)
        {
            _settings = settings;
        }

        internal void Start()
        {
            AddLight(Quaternion.Euler(135, 0, 0), kAmbientColor, _settings.lighting.environment.intensity); // front
            AddLight(Quaternion.Euler(45, 0, 0), kAmbientColor, _settings.lighting.environment.intensity); // back
        }

        #endregion

        private void AddLight(Quaternion rotation, Color color, float intensity)
        {
            var container = new GameObject("Light");
            Light light = container.AddComponent<Light>();

            light.type = LightType.Directional;
            light.color = color;
            light.shadows = LightShadows.Soft;
            light.intensity = intensity;
            light.cullingMask = AvatarLayers.kAllLayersMask;
            light.shadowStrength = 1;

            container.transform.SetParent(transform, false);
            container.transform.SetPositionAndRotation(Vector3.zero, rotation);
        }
    }
}
