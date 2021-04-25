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
            if (!_settings.lighting.environment.enabled) return;

            switch (_settings.lighting.environment.type)
            {
                case EnvironmentLightingType.TwoSided:
                    Container.Bind<TwoSidedLightingController>().FromNewComponentOnNewGameObject().NonLazy();
                    break;

                case EnvironmentLightingType.Dynamic:
                    Container.Bind<DynamicDirectionalLightingController>().FromNewComponentOnNewGameObject().NonLazy();
                    break;
            }
        }
    }
}
