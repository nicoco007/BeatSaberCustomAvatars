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
    internal partial class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.Settings.bsml";

        #region Components
        #pragma warning disable CS0649

        [UIComponent("container")] private readonly RectTransform _container;
        [UIComponent("loader")] private readonly Transform _loader;

        #pragma warning restore CS0649
        #endregion

        #region Values

        [UIValue("general-settings-host")] private GeneralSettingsHost _generalSettingsHost;
        [UIValue("avatar-specific-settings-host")] private AvatarSpecificSettingsHost _avatarSpecificSettingsHost;
        [UIValue("automatic-fbt-calibration-host")] private AutomaticFbtCalibrationHost _automaticFbtCalibrationHost;

        #endregion

        private PlayerAvatarManager _avatarManager;
        private GameplaySetupViewController _gameplaySetupViewController;
        private VRPlayerInputInternal _playerInput;

        [Inject]
        internal void Construct(PlayerAvatarManager avatarManager, GameplaySetupViewController gameplaySetupViewController, VRPlayerInputInternal playerInput, GeneralSettingsHost generalSettingsHost, AvatarSpecificSettingsHost avatarSpecificSettingsHost, AutomaticFbtCalibrationHost automaticFbtCalibrationHost)
        {
            _avatarManager = avatarManager;
            _gameplaySetupViewController = gameplaySetupViewController;
            _playerInput = playerInput;
            _generalSettingsHost = generalSettingsHost;
            _avatarSpecificSettingsHost = avatarSpecificSettingsHost;
            _automaticFbtCalibrationHost = automaticFbtCalibrationHost;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                RectTransform header = Instantiate((RectTransform)_gameplaySetupViewController.transform.Find("HeaderPanel"), rectTransform, false);

                header.name = "HeaderPanel";
                header.offsetMin = new Vector2(-45, -8);
                header.offsetMax = new Vector2(45, 0);

                Destroy(header.GetComponentInChildren<LocalizedTextMeshProUGUI>());

                TextMeshProUGUI textMesh = header.Find("Text").GetComponent<TextMeshProUGUI>();
                textMesh.text = "Settings";
                textMesh.fontSize = 6;
                textMesh.rectTransform.offsetMin = new Vector2(0, -1.86f);
                textMesh.rectTransform.offsetMax = new Vector2(0, -1.86f);
            }

            _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _avatarManager.avatarLoadFailed += OnAvatarLoadFailed;
            _playerInput.inputChanged += OnInputChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);

            _generalSettingsHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            _avatarSpecificSettingsHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            _automaticFbtCalibrationHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _avatarManager.avatarStartedLoading -= OnAvatarStartedLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;
            _playerInput.inputChanged -= OnInputChanged;

            _generalSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _avatarSpecificSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _automaticFbtCalibrationHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
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
            _generalSettingsHost.UpdateUI(avatar);
            _avatarSpecificSettingsHost.UpdateUI(avatar);
            _automaticFbtCalibrationHost.UpdateUI(avatar);
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
