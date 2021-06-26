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

using CustomAvatar.Avatar;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using UnityEngine;
using UnityEngine.UI;
using CustomAvatar.Tracking;
using CustomAvatar.Player;
using CustomAvatar.Configuration;

namespace CustomAvatar.UI
{
    internal class AvatarSpecificSettingsHost : IViewControllerHost
    {
        #region Components
#pragma warning disable CS0649, IDE0044

        [UIComponent("ignore-exclusions")] private ToggleSetting _ignoreExclusionsSetting;
        [UIComponent("bypass-calibration")] private ToggleSetting _bypassCalibration;
        [UIComponent("automatic-calibration")] private ToggleSetting _automaticCalibrationSetting;

        [UIComponent("calibrate-button")] private Button _calibrateButton;
        [UIComponent("clear-button")] private Button _clearButton;

        [UIComponent("calibrate-button")] private CurvedTextMeshPro _calibrateButtonText;
        [UIComponent("clear-button")] private CurvedTextMeshPro _clearButtonText;

        [UIComponent("automatic-calibration")] private HoverHint _automaticCalibrationHoverHint;
        [UIComponent("calibrate-button")] private HoverHint _calibrateButtonHoverHint;

#pragma warning restore CS0649, IDE0044
        #endregion

        private readonly PlayerAvatarManager _avatarManager;
        private readonly VRPlayerInputInternal _playerInput;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;
        private readonly ManualCalibrationHelper _manualCalibrationHelper;

        private bool _calibrating;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        private CalibrationData.FullBodyCalibration _currentAvatarManualCalibration;

        internal AvatarSpecificSettingsHost(PlayerAvatarManager avatarManager, VRPlayerInputInternal playerInput, Settings settings, CalibrationData calibrationData, ManualCalibrationHelper manualCalibrationHelper)
        {
            _avatarManager = avatarManager;
            _playerInput = playerInput;
            _settings = settings;
            _calibrationData = calibrationData;
            _manualCalibrationHelper = manualCalibrationHelper;
        }

        public void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) { }

        public void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            DisableCalibrationMode(false);
        }

        public void UpdateUI(SpawnedAvatar avatar)
        {
            if (_currentAvatarSettings != null) _currentAvatarSettings.useAutomaticCalibration.changed -= OnUseAutomaticCalibrationChanged;

            UpdateCalibrationButtons(avatar);

            if (!avatar)
            {
                _clearButton.interactable = false;
                _calibrateButton.interactable = false;
                _automaticCalibrationSetting.interactable = false;
                _automaticCalibrationHoverHint.text = "No avatar selected";

                return;
            }

            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.prefab.fileName);
            _currentAvatarManualCalibration = _calibrationData.GetAvatarManualCalibration(avatar.prefab.fileName);

            _currentAvatarSettings.useAutomaticCalibration.changed += OnUseAutomaticCalibrationChanged;

            _ignoreExclusionsSetting.Value = _currentAvatarSettings.ignoreExclusions;

            _bypassCalibration.Value = _currentAvatarSettings.bypassCalibration;

            _automaticCalibrationSetting.Value = _currentAvatarSettings.useAutomaticCalibration;
            _automaticCalibrationSetting.interactable = avatar.prefab.descriptor.supportsAutomaticCalibration;
            _automaticCalibrationHoverHint.text = avatar.prefab.descriptor.supportsAutomaticCalibration ? "Use automatic calibration instead of manual calibration." : "Not supported by current avatar";
        }

        private void UpdateCalibrationButtons(SpawnedAvatar avatar)
        {
            if (!avatar)
            {
                _calibrateButton.interactable = false;
                _clearButton.interactable = false;
                _calibrateButtonHoverHint.text = "No avatar selected";
                _calibrateButtonText.text = "Calibrate";
                _clearButtonText.text = "Clear";

                return;
            }

            if (!_playerInput.TryGetUncalibratedPose(DeviceUse.Waist, out Pose _) &&
                !_playerInput.TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose _) &&
                !_playerInput.TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose _))
            {
                _calibrateButton.interactable = false;
                _clearButton.interactable = _currentAvatarManualCalibration?.isCalibrated == true;
                _calibrateButtonHoverHint.text = "No trackers detected";
                _calibrateButtonText.text = "Calibrate";
                _clearButtonText.text = "Clear";

                return;
            }

            _calibrateButton.interactable = true;
            _clearButton.interactable = _calibrating || _currentAvatarManualCalibration?.isCalibrated == true;
            _calibrateButtonHoverHint.text = "Start manual full body calibration";
            _calibrateButtonText.text = _calibrating ? "Save" : "Calibrate";
            _clearButtonText.text = _calibrating ? "Cancel" : "Clear";
        }

        private void OnUseAutomaticCalibrationChanged(bool value)
        {
            _automaticCalibrationSetting.Value = value;
        }

        #region Actions
#pragma warning disable IDE0051

        private void OnIgnoreExclusionsChanged(bool value)
        {
            if (_currentAvatarSettings == null) return;

            _currentAvatarSettings.ignoreExclusions.value = value;
        }

        private void OnEnableBypassCalibrationChanged(bool value)
        {
            if (_currentAvatarSettings == null) return;

            _currentAvatarSettings.bypassCalibration.value = value;
        }

        private void OnEnableAutomaticCalibrationChanged(bool value)
        {
            DisableCalibrationMode(false);

            if (_currentAvatarSettings == null) return;

            _currentAvatarSettings.useAutomaticCalibration.value = value;
        }

        private void OnCalibrateFullBodyTrackingClicked()
        {
            if (!_calibrating)
            {
                EnableCalibrationMode();
            }
            else
            {
                DisableCalibrationMode(true);
            }
        }

        private void OnClearFullBodyTrackingCalibrationDataClicked()
        {
            if (_calibrating)
            {
                DisableCalibrationMode(false);
            }
            else
            {
                _playerInput.ClearManualFullBodyTrackingData(_avatarManager.currentlySpawnedAvatar);
                _clearButton.interactable = false;
            }
        }

#pragma warning restore IDE0051
        #endregion

        private void EnableCalibrationMode()
        {
            if (!_avatarManager.currentlySpawnedAvatar) return;

            SetCalibrationMode(true);
            UpdateCalibrationButtons(_avatarManager.currentlySpawnedAvatar);
        }

        private void DisableCalibrationMode(bool save)
        {
            SetCalibrationMode(false);

            if (!_avatarManager.currentlySpawnedAvatar) return;

            if (save)
            {
                _playerInput.CalibrateFullBodyTrackingManual(_avatarManager.currentlySpawnedAvatar);

                _automaticCalibrationSetting.Value = false;
                OnEnableAutomaticCalibrationChanged(false);
            }

            UpdateCalibrationButtons(_avatarManager.currentlySpawnedAvatar);
        }

        private void SetCalibrationMode(bool enabled)
        {
            _calibrating = enabled;
            _manualCalibrationHelper.enabled = enabled;
            _playerInput.isCalibrationModeEnabled = enabled;
        }
    }
}
