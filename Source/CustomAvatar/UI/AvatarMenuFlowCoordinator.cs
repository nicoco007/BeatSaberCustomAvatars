//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using HMUI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarMenuFlowCoordinator : FlowCoordinator, IInitializable, IDisposable
    {
        private ILogger<AvatarMenuFlowCoordinator> _logger;
        private MainFlowCoordinator _mainFlowCoordinator;
        private AvatarListViewController _avatarListViewController;
        private MirrorViewController _mirrorViewController;
        private SettingsViewController _settingsViewController;
        private Settings _settings;

        private MenuButton _menuButton;

        public void Initialize()
        {
            name = nameof(AvatarMenuFlowCoordinator);

            _menuButton = new MenuButton("Avatars", () =>
            {
                _mainFlowCoordinator.PresentFlowCoordinator(this);
            });

            MenuButtons.instance.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            if (MenuButtons.IsSingletonAvailable && BSMLParser.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(_menuButton);
            }
        }

        [Inject]
        private void Construct(ILogger<AvatarMenuFlowCoordinator> logger, MainFlowCoordinator mainFlowCoordinator, AvatarListViewController avatarListViewController, MirrorViewController mirrorViewController, SettingsViewController settingsViewController, Settings settings)
        {
            _logger = logger;
            _mainFlowCoordinator = mainFlowCoordinator;
            _avatarListViewController = avatarListViewController;
            _mirrorViewController = mirrorViewController;
            _settingsViewController = settingsViewController;
            _settings = settings;
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
            try
            {
                _logger.LogInformation($"Writing settings to '{Settings.kSettingsPath}'");
                _settings.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to write settings to file");
                _logger.LogError(ex);
            }

            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
