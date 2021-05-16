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

using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace CustomAvatar.UI
{
    internal class AutomaticFbtCalibrationHost : IViewControllerHost
    {
        #region Components
#pragma warning disable CS0649, IDE0044

        [UIComponent("calibrate-fbt-on-start")] private ToggleSetting _calibrateFullBodyTrackingOnStart;
        [UIComponent("pelvis-offset")] private IncrementSetting _pelvisOffset;
        [UIComponent("foot-offset")] private IncrementSetting _footOffset;
        [UIComponent("waist-tracker-position")] private DropDownListSetting _waistTrackerPosition;

        [UIComponent("auto-calibrate-button")] private Button _autoCalibrateButton;
        [UIComponent("auto-clear-button")] private Button _autoClearButton;

        [UIComponent("auto-calibrate-button")] private HoverHint _autoCalibrateButtonHoverHint;

#pragma warning restore CS0649, IDE0044
        #endregion

        #region Values
#pragma warning disable IDE0052

        [UIValue("waist-tracker-position-options")] private readonly List<object> _waistTrackerOptions = new List<object> { WaistTrackerPosition.Front, WaistTrackerPosition.Left, WaistTrackerPosition.Right, WaistTrackerPosition.Back };

        #endregion

        private readonly PlayerAvatarManager _avatarManager;
        private readonly VRPlayerInputInternal _playerInput;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;

        internal AutomaticFbtCalibrationHost(PlayerAvatarManager avatarManager, VRPlayerInputInternal playerInput, Settings settings, CalibrationData calibrationData)
        {
            _avatarManager = avatarManager;
            _playerInput = playerInput;
            _settings = settings;
            _calibrationData = calibrationData;
        }

        public void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _calibrateFullBodyTrackingOnStart.Value = _settings.calibrateFullBodyTrackingOnStart;
            _pelvisOffset.Value = _settings.automaticCalibration.pelvisOffset;
            _footOffset.Value = _settings.automaticCalibration.legOffset;
            _waistTrackerPosition.Value = _settings.automaticCalibration.waistTrackerPosition;
        }

        public void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) { }

        public void UpdateUI(SpawnedAvatar avatar)
        {
            if (!avatar)
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = false;
                _autoCalibrateButtonHoverHint.text = "No avatar selected";

                return;
            }

            if (!_playerInput.TryGetUncalibratedPose(DeviceUse.Waist, out Pose _) &&
                !_playerInput.TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose _) &&
                !_playerInput.TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose _))
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = _calibrationData.automaticCalibration.isCalibrated;
                _autoCalibrateButtonHoverHint.text = "No trackers detected";

                return;
            }

            if (avatar.prefab.descriptor.supportsAutomaticCalibration)
            {
                _autoCalibrateButton.interactable = true;
                _autoClearButton.interactable = _calibrationData.automaticCalibration.isCalibrated;
                _autoCalibrateButtonHoverHint.text = "Calibrate full body tracking automatically";
            }
            else
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = false;
                _autoCalibrateButtonHoverHint.text = "Not supported by current avatar";
            }
        }

        #region Actions
#pragma warning disable IDE0051

        [UIAction("calibrate-fbt-on-start-change")]
        private void OnCalibrateFullBodyTrackingOnStartChanged(bool value)
        {
            _settings.calibrateFullBodyTrackingOnStart = value;
        }

        [UIAction("pelvis-offset-change")]
        private void OnPelvisOffsetChanged(float value)
        {
            _settings.automaticCalibration.pelvisOffset = value;
        }

        [UIAction("foot-offset-change")]
        private void OnLeftFootOffsetChanged(float value)
        {
            _settings.automaticCalibration.legOffset = value;
        }

        [UIAction("auto-calibrate-fbt-click")]
        private void OnCalibrateAutoFullBodyTrackingClicked()
        {
            _playerInput.CalibrateFullBodyTrackingAuto();

            if (_avatarManager.currentlySpawnedAvatar)
            {
                _settings.GetAvatarSettings(_avatarManager.currentlySpawnedAvatar.prefab.fileName).useAutomaticCalibration.value = true;
                UpdateUI(_avatarManager.currentlySpawnedAvatar);
            }
        }

        [UIAction("auto-clear-fbt-calibration-data-click")]
        private void OnClearAutoFullBodyTrackingCalibrationDataClicked()
        {
            _playerInput.ClearAutomaticFullBodyTrackingData();

            if (_avatarManager.currentlySpawnedAvatar) UpdateUI(_avatarManager.currentlySpawnedAvatar);
        }

        [UIAction("waist-tracker-position-change")]
        private void OnWaistTrackerPositionChanged(WaistTrackerPosition waistTrackerPosition)
        {
            _settings.automaticCalibration.waistTrackerPosition = waistTrackerPosition;
        }

#pragma warning restore IDE0051
        #endregion
    }
}
