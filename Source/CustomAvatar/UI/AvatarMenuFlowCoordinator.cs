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

using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarMenuFlowCoordinator : FlowCoordinator, IInitializable, IDisposable
    {
        private AvatarListViewController _avatarListViewController;
        private MirrorViewController _mirrorViewController;
        private SettingsViewController _settingsViewController;

        private MenuButton menuButton;

        public void Initialize()
        {
            menuButton = new MenuButton("Avatars", () =>
            {
                BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(this);
            });

            MenuButtons.instance.RegisterButton(menuButton);
        }

        public void Dispose()
        {
            try
            {
                MenuButtons.instance.UnregisterButton(menuButton);
            }
            catch (NullReferenceException) { } // this is usually expected when the game is shutting down
        }

        [Inject]
        private void Inject(AvatarListViewController avatarListViewController, MirrorViewController mirrorViewController, SettingsViewController settingsViewController)
        {
            _avatarListViewController = avatarListViewController;
            _mirrorViewController = mirrorViewController;
            _settingsViewController = settingsViewController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            showBackButton = true;

            if (firstActivation)
            {
                SetTitle("Custom Avatars");
            }

            if (addedToHierarchy)
            {
                ProvideInitialViewControllers(_mirrorViewController, _settingsViewController, _avatarListViewController);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
