/*using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using TMPro;
using UnityEngine;

#pragma warning disable 649 // disable "field is never assigned"
#pragma warning disable IDE0044 // disable "make field readonly"
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
namespace CustomAvatar.UI
{
    class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.SettingsViewController.bsml";

        #region Components
        
        [UIComponent("arm-span")] private TextMeshProUGUI armSpanLabel;

        #endregion

        #region Properties

        [UIValue("resize-options")] private readonly List<object> resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };

        #endregion

        #region Values

        [UIValue("visible-first-person-value")] private bool visibleInFirstPerson;
        [UIValue("resize-value")] private AvatarResizeMode resizeMode;
        [UIValue("floor-adjust-value")] private bool floorHeightAdjust;
        [UIValue("calibrate-fbt-on-start")] private bool calibrateFullBodyTrackingOnStart;

        #endregion

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            visibleInFirstPerson = SettingsManager.settings.isAvatarVisibleInFirstPerson;
            resizeMode = SettingsManager.settings.resizeMode;
            floorHeightAdjust = SettingsManager.settings.enableFloorAdjust;
            calibrateFullBodyTrackingOnStart = SettingsManager.settings.calibrateFullBodyTrackingOnStart;

            base.DidActivate(firstActivation, type);

            armSpanLabel.SetText($"{SettingsManager.settings.playerArmSpan:0.00} m");
        }

        #region Actions

        [UIAction("visible-first-person-change")]
        private void OnVisibleInFirstPersonChanged(bool value)
        {
            SettingsManager.settings.isAvatarVisibleInFirstPerson = value;
            AvatarManager.instance.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
        }

        [UIAction("resize-change")]
        private void OnResizeModeChanged(AvatarResizeMode value)
        {
            SettingsManager.settings.resizeMode = value;
            AvatarManager.instance.ResizeCurrentAvatar();
        }

        [UIAction("floor-adjust-change")]
        private void OnFloorHeightAdjustChanged(bool value)
        {
            SettingsManager.settings.enableFloorAdjust = value;
            AvatarManager.instance.ResizeCurrentAvatar();
        }

        [UIAction("resize-mode-formatter")]
        private string ResizeModeFormatter(object value)
        {
            if (!(value is AvatarResizeMode)) return null;

            switch ((AvatarResizeMode) value)
            {
                case AvatarResizeMode.Height:
                    return "Height";
                case AvatarResizeMode.ArmSpan:
                    return "Arm Span";
                case AvatarResizeMode.None:
                    return "Don't Resize";
                default:
                    return null;
            }
        }

        [UIAction("measure-arm-span-click")]
        private void OnMeasureArmSpanButtonClicked()
        {
            MeasureArmSpan();
        }

        [UIAction("calibrate-fbt-click")]
        private void OnCalibrateFullBodyTrackingClicked()
        {
            AvatarManager.instance.avatarTailor.CalibrateFullBodyTracking();
        }

        [UIAction("calibrate-fbt-on-start-change")]
        private void OnCalibrateFullBodyTrackingOnStartChanged(bool value)
        {
            SettingsManager.settings.calibrateFullBodyTrackingOnStart = value;
        }

        [UIAction("clear-fbt-calibration-data-click")]
        private void OnClearFullBodyTrackingCalibrationDataClicked()
        {
            AvatarManager.instance.avatarTailor.ClearFullBodyTrackingData();
        }

        #endregion

        #region Arm Span Measurement
        
        private const float KMinArmSpan = 0.5f;

        private TrackedDeviceManager playerInput = PersistentSingleton<TrackedDeviceManager>.instance;
        private bool isMeasuring;
        private float maxMeasuredArmSpan;
        private float lastUpdateTime;

        private void MeasureArmSpan()
        {
            if (isMeasuring) return;

            isMeasuring = true;
            maxMeasuredArmSpan = KMinArmSpan;
            lastUpdateTime = Time.timeSinceLevelLoad;

            InvokeRepeating(nameof(ScanArmSpan), 0.0f, 0.1f);
        }

        private void ScanArmSpan()
        {
            var armSpan = Vector3.Distance(playerInput.LeftHand.Position, playerInput.RightHand.Position);

            if (armSpan > maxMeasuredArmSpan)
            {
                maxMeasuredArmSpan = armSpan;
                lastUpdateTime = Time.timeSinceLevelLoad;
            }

            if (Time.timeSinceLevelLoad - lastUpdateTime < 2.0f)
            {
                armSpanLabel.SetText($"Measuring... {maxMeasuredArmSpan:0.00} m");
            }
            else
            {
                CancelInvoke(nameof(ScanArmSpan));
                armSpanLabel.SetText($"{maxMeasuredArmSpan:0.00} m");
                SettingsManager.settings.playerArmSpan = maxMeasuredArmSpan;
                isMeasuring = false;
            }
        }

        #endregion
    }
}
*/