using CustomAvatar.Configuration;
using CustomAvatar.Lighting;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class LightingInstaller : Installer
    {
        public override void InstallBindings()
        {
            Settings settings = Container.Resolve<Settings>();

            if (settings.lighting.quality != LightingQuality.Off)
            {
                if (settings.lighting.enableDynamicLighting)
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
