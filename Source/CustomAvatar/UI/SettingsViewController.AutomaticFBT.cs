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

/*
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using CustomAvatar.Tracking;
using HMUI;
using UnityEngine.UI;

namespace CustomAvatar.UI
{
    internal partial class SettingsViewController
    {
        #region Components
        #pragma warning disable 649
        #pragma warning disable IDE0044

        [UIComponent("calibrate-fbt-on-start")] private CheckboxSetting _calibrateFullBodyTrackingOnStart;
        [UIComponent("pelvis-offset")] private IncrementSetting _pelvisOffset;
        [UIComponent("foot-offset")] private IncrementSetting _footOffset;
        [UIComponent("waist-tracker-position")] private ListSetting _waistTrackerPosition;

        [UIComponent("auto-calibrate-button")] private Button _autoCalibrateButton;
        [UIComponent("auto-clear-button")] private Button _autoClearButton;

        [UIComponent("auto-calibrate-button")] private HoverHint _autoCalibrateButtonHoverHint;

        #pragma warning restore 649
        #pragma warning restore IDE0044
        #endregion

        #region Values

        [UIValue("waist-tracker-position-options")] private readonly List<object> _waistTrackerOptions = new List<object> { WaistTrackerPosition.Front, WaistTrackerPosition.Left, WaistTrackerPosition.Right, WaistTrackerPosition.Back };

        #endregion

        #region Actions

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
            _avatarTailor.CalibrateFullBodyTrackingAuto();
            _autoClearButton.interactable = _calibrationData.automaticCalibration.isCalibrated;

            _automaticCalibrationSetting.CheckboxValue = true;
            OnEnableAutomaticCalibrationChanged(true);
        }

        [UIAction("auto-clear-fbt-calibration-data-click")]
        private void OnClearAutoFullBodyTrackingCalibrationDataClicked()
        {
            _avatarTailor.ClearAutomaticFullBodyTrackingData();
            _autoClearButton.interactable = false;
        }

        [UIAction("waist-tracker-position-change")]
        private void OnWaistTrackerPositionChanged(WaistTrackerPosition waistTrackerPosition)
        {
            _settings.automaticCalibration.waistTrackerPosition = waistTrackerPosition;
        }

        #endregion
    }
}
*/
