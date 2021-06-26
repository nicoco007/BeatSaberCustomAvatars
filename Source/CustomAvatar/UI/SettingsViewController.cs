//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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

using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using HMUI;
using Polyglot;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.UI.Views.Settings.bsml";

        #region Components
#pragma warning disable CS0649

        [UIComponent("container")] private readonly RectTransform _container;
        [UIComponent("loader")] private readonly Transform _loader;

#pragma warning restore CS0649
        #endregion

        #region Values

        internal GeneralSettingsHost generalSettingsHost;
        internal AvatarSpecificSettingsHost avatarSpecificSettingsHost;
        internal AutomaticFbtCalibrationHost automaticFbtCalibrationHost;
        internal InterfaceSettingsHost interfaceSettingsHost;

        #endregion

        private PlayerAvatarManager _avatarManager;
        private PlatformLeaderboardViewController _leaderboardViewController;
        private VRPlayerInputInternal _playerInput;

        [Inject]
        internal void Construct(PlayerAvatarManager avatarManager, PlatformLeaderboardViewController leaderboardViewController, VRPlayerInputInternal playerInput, GeneralSettingsHost generalSettingsHost, AvatarSpecificSettingsHost avatarSpecificSettingsHost, AutomaticFbtCalibrationHost automaticFbtCalibrationHost, InterfaceSettingsHost interfaceSettingsHost)
        {
            _avatarManager = avatarManager;
            _leaderboardViewController = leaderboardViewController;
            _playerInput = playerInput;
            this.generalSettingsHost = generalSettingsHost;
            this.avatarSpecificSettingsHost = avatarSpecificSettingsHost;
            this.automaticFbtCalibrationHost = automaticFbtCalibrationHost;
            this.interfaceSettingsHost = interfaceSettingsHost;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                RectTransform header = Instantiate((RectTransform)_leaderboardViewController.transform.Find("HeaderPanel"), rectTransform, false);

                header.name = "HeaderPanel";

                Destroy(header.GetComponentInChildren<LocalizedTextMeshProUGUI>());

                TextMeshProUGUI textMesh = header.Find("Text").GetComponent<TextMeshProUGUI>();
                textMesh.text = "Settings";

                ImageView bg = header.Find("BG").GetComponent<ImageView>();
                bg.color0 = new Color(1, 1, 1, 0);
                bg.color1 = new Color(1, 1, 1, 1);
            }

            _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _avatarManager.avatarLoadFailed += OnAvatarLoadFailed;
            _playerInput.inputChanged += OnInputChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);

            generalSettingsHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            avatarSpecificSettingsHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            automaticFbtCalibrationHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _avatarManager.avatarStartedLoading -= OnAvatarStartedLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;
            _playerInput.inputChanged -= OnInputChanged;

            generalSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            avatarSpecificSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            automaticFbtCalibrationHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void OnAvatarStartedLoading(string fileName)
        {
            SetLoading(true);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            SetLoading(false);
        }

        private void OnAvatarLoadFailed(Exception exception)
        {
            SetLoading(false);
        }

        private void OnInputChanged()
        {
            UpdateUI(_avatarManager.currentlySpawnedAvatar);
        }

        private void SetLoading(bool loading)
        {
            _loader.gameObject.SetActive(loading);
            SetInteractableRecursively(!loading && _avatarManager.currentlySpawnedAvatar);

            UpdateUI(_avatarManager.currentlySpawnedAvatar);
        }

        private void UpdateUI(SpawnedAvatar avatar)
        {
            generalSettingsHost.UpdateUI(avatar);
            avatarSpecificSettingsHost.UpdateUI(avatar);
            automaticFbtCalibrationHost.UpdateUI(avatar);
        }

        private void SetInteractableRecursively(bool enable)
        {
            foreach (Selectable selectable in _container.GetComponentsInChildren<Selectable>(true))
            {
                selectable.interactable = enable;
                selectable.enabled = enable;
            }

            foreach (Interactable interactable in _container.GetComponentsInChildren<Interactable>(true))
            {
                interactable.interactable = enable;
                interactable.enabled = enable;
            }

            float alpha = enable ? 1 : 0.5f;

            foreach (TextMeshProUGUI textMesh in _container.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                textMesh.alpha = alpha;
            }
        }
    }
}
