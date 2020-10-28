using CustomAvatar.Configuration;
using CustomAvatar.Lighting;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class LightingInstaller : Installer
    {
        private readonly Settings _settings;

        public LightingInstaller(Settings settings)
        {
            _settings = settings;
        }

        public override void InstallBindings()
        {
            if (_settings.lighting.quality != LightingQuality.Off)
            {
                if (_settings.lighting.enableDynamicLighting)
                {
                    Container.Bind<DynamicLightingController>().FromNewComponentOnNewGameObject().NonLazy();
                }
                else
                {
                    Container.Bind<TwoSidedLightingController>().FromNewComponentOnNewGameObject().NonLazy();
                }
            }
        }
    }
}
