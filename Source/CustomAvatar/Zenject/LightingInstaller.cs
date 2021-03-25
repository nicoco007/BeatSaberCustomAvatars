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
            switch (_settings.lighting.level)
            {
                case LightingLevel.TwoSided:
                    Container.Bind<TwoSidedLightingController>().FromNewComponentOnNewGameObject().NonLazy();
                    break;

                case LightingLevel.Dynamic:
                    Container.Bind<DynamicDirectionalLightingController>().FromNewComponentOnNewGameObject().NonLazy();
                    break;
            }
        }
    }
}
