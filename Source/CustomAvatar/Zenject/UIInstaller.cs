//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using BeatSaberMarkupLanguage;
using CustomAvatar.UI;
using VRUIControls;
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
            Container.QueueForInject(avatarListViewController.GetComponent<VRGraphicRaycaster>());
            Container.QueueForInject(mirrorViewController);
            Container.QueueForInject(mirrorViewController.GetComponent<VRGraphicRaycaster>());
            Container.QueueForInject(settingsViewController);
            Container.QueueForInject(settingsViewController.GetComponent<VRGraphicRaycaster>());
        }
    }
}
