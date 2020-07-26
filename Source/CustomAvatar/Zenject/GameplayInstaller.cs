using CustomAvatar.Lighting;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class GameplayInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<GameplayLightingController>().FromNewComponentOnNewGameObject().NonLazy();
        }
    }
}
