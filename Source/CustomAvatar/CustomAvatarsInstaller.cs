using Zenject;

namespace CustomAvatar
{
    internal class CustomAvatarsInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<AvatarManager>().AsSingle();
            Container.Bind<AvatarTailor>().AsTransient();
        }
    }
}
