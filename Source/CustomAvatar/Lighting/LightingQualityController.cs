using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class LightingQualityController : IInitializable
    {
        private readonly Settings _settings;

        public LightingQualityController(Settings settings)
        {
            _settings = settings;
        }

        public void Initialize()
        {
            QualitySettings.shadowDistance = 10;
            QualitySettings.shadowNearPlaneOffset = 3;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;

            QualitySettings.shadows = _settings.lighting.shadowQuality;
            QualitySettings.shadowResolution = _settings.lighting.shadowResolution;
            QualitySettings.pixelLightCount = _settings.lighting.pixelLightCount;
        }
    }
}
