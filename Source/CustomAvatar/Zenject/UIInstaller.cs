using BeatSaberMarkupLanguage;
using CustomAvatar.UI;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class UIInstaller : Installer
    {
        public override void InstallBindings()
        {
            var avatarListViewController = BeatSaberUI.CreateViewController<AvatarListViewController>();
            var mirrorViewController = BeatSaberUI.CreateViewController<MirrorViewController>();
            var settingsViewController = BeatSaberUI.CreateViewController<SettingsViewController>();

            Container.Bind<AvatarListViewController>().FromInstance(avatarListViewController);
            Container.Bind<MirrorViewController>().FromInstance(mirrorViewController);
            Container.Bind<SettingsViewController>().FromInstance(settingsViewController);
            Container.BindInterfacesAndSelfTo<AvatarMenuFlowCoordinator>().FromNewComponentOnNewGameObject(nameof(AvatarMenuFlowCoordinator));

            Container.QueueForInject(avatarListViewController);
            Container.QueueForInject(mirrorViewController);
            Container.QueueForInject(settingsViewController);
        }
    }
}
