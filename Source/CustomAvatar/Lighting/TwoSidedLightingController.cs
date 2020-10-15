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
using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class TwoSidedLightingController : MonoBehaviour
    {
        private Settings _settings;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        private void Inject(Settings settings)
        {
            _settings = settings;
        }

        private void Start()
        {
            SetLightingQuality(_settings.lighting.quality);

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

        private void SetLightingQuality(LightingQuality quality)
        {
            // these settings are based off Unity's default quality profiles
            QualitySettings.shadowDistance = 10;
            QualitySettings.shadowNearPlaneOffset = 3;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;

            switch (quality)
            {
                case LightingQuality.VeryLow:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowCascades = 0;
                    QualitySettings.pixelLightCount = 0;
                    break;

                case LightingQuality.Low:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowCascades = 0;
                    QualitySettings.pixelLightCount = 1;
                    break;

                case LightingQuality.Medium:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.pixelLightCount = 2;
                    break;

                case LightingQuality.High:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.pixelLightCount = 3;
                    break;

                case LightingQuality.VeryHigh:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.pixelLightCount = 4;
                    break;
            }
        }
    }
}
