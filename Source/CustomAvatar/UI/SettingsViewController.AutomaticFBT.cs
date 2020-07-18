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
        // ReSharper disable UnusedMember.Local

        [UIValue("waist-tracker-position-options")] private readonly List<object> _waistTrackerOptions = new List<object> { WaistTrackerPosition.Front, WaistTrackerPosition.Left, WaistTrackerPosition.Right, WaistTrackerPosition.Back };
        
        // ReSharper restore UnusedMember.Local
        #endregion

        #region Actions
        // ReSharper disable UnusedMember.Local

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
            _autoClearButton.interactable = _settings.automaticCalibration.isCalibrated;

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
        
        // ReSharper restore UnusedMember.Local
        #endregion
    }
}
