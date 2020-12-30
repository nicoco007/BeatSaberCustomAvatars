using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class LightingQualityController : IInitializable
    {
        private readonly Settings _settings;

        internal LightingQualityController(Settings settings)
        {
            _settings = settings;
        }

        public void Initialize()
        {
            SetLightingQuality(_settings.lighting.quality);
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
                    QualitySettings.pixelLightCount = 0;
                    break;

                case LightingQuality.Low:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.pixelLightCount = 2;
                    break;

                case LightingQuality.Medium:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.pixelLightCount = 4;
                    break;

                case LightingQuality.High:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.pixelLightCount = 6;
                    break;

                case LightingQuality.VeryHigh:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.pixelLightCount = 12;
                    break;
            }
        }
    }
}
