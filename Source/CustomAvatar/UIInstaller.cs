using BeatSaberMarkupLanguage;
using CustomAvatar.UI;
using HMUI;
using UnityEngine;
using Zenject;

namespace CustomAvatar
{
    internal class UIInstaller : Installer
    {
        public override void InstallBindings()
        {
            var avatarListViewController = BeatSaberUI.CreateViewController<AvatarListViewController>();
            var mirrorViewController = BeatSaberUI.CreateViewController<MirrorViewController>();
            var settingsViewController = BeatSaberUI.CreateViewController<SettingsViewController>();

            // required since BaseInputModule isn't actually registered for some reason...?
            Container.Bind<BaseInputModule>().FromInstance(null);

            Container.Bind<AvatarListViewController>().FromInstance(avatarListViewController);
            Container.Bind<MirrorViewController>().FromInstance(mirrorViewController);
            Container.Bind<SettingsViewController>().FromInstance(settingsViewController);
            Container.Bind<AvatarListFlowCoordinator>().FromNewComponentOnNewPrefab(new GameObject()).AsSingle();

            Container.QueueForInject(avatarListViewController);
            Container.QueueForInject(mirrorViewController);
            Container.QueueForInject(settingsViewController);
        }
    }
}
