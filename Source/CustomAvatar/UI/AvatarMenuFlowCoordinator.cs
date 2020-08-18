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
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.Utilities;
using HMUI;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarMenuFlowCoordinator : FlowCoordinator, IInitializable, IDisposable
    {
        private AvatarListViewController _avatarListViewController; 
        private MirrorViewController _mirrorViewController;
        private SettingsViewController _settingsViewController;

        private GameObject _mainScreen;
        private Vector3 _mainScreenScale;

        private MenuButton menuButton;

        public void Initialize()
        {
            menuButton = new MenuButton("Avatars", () =>
            {
                BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(this, null, true);
            });

            MenuButtons.instance.RegisterButton(menuButton);
        }

        public void Dispose()
        {
            if (MenuButtons.IsSingletonAvailable && BSMLParser.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(menuButton);
            }
        }

        [Inject]
        private void Inject(AvatarListViewController avatarListViewController, MirrorViewController mirrorViewController, SettingsViewController settingsViewController)
        {
            _avatarListViewController = avatarListViewController;
            _mirrorViewController = mirrorViewController;
            _settingsViewController = settingsViewController;
        }

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _mainScreen = GameObject.Find("MainScreen");

            showBackButton = true;

            if (firstActivation)
            {
                title = "Custom Avatars";
                _mainScreenScale = _mainScreen.transform.localScale;
            }

            if (activationType == ActivationType.AddedToHierarchy)
            {
                ProvideInitialViewControllers(_mirrorViewController, _settingsViewController, _avatarListViewController);
                _mainScreen.transform.localScale = Vector3.zero;
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                _mainScreen.transform.localScale = _mainScreenScale;
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            mainFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", this, null, false);
        }
    }
}
