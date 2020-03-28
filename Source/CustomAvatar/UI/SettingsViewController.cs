using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Tracking;
using TMPro;
using UnityEngine;

#pragma warning disable 649 // disable "field is never assigned"
#pragma warning disable IDE0044 // disable "make field readonly"
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
namespace CustomAvatar.UI
{
    internal class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.SettingsViewController.bsml";

        private bool _calibrating = false;
        private Material _sphereMaterial = null;

        #region Components
        
        [UIComponent("arm-span")] private TextMeshProUGUI armSpanLabel;
        [UIComponent("calibrate-button")] private TextMeshProUGUI calibrateButtonText;

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
            visibleInFirstPerson = Plugin.settings.isAvatarVisibleInFirstPerson;
            resizeMode = Plugin.settings.resizeMode;
            floorHeightAdjust = Plugin.settings.enableFloorAdjust;
            calibrateFullBodyTrackingOnStart = Plugin.settings.calibrateFullBodyTrackingOnStart;

            base.DidActivate(firstActivation, type);

            armSpanLabel.SetText($"{Plugin.settings.playerArmSpan:0.00} m");

            _sphereMaterial = new Material(ShaderLoader.unlitShader);
        }

        #region Actions

        [UIAction("visible-first-person-change")]
        private void OnVisibleInFirstPersonChanged(bool value)
        {
            Plugin.settings.isAvatarVisibleInFirstPerson = value;
            AvatarManager.instance.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
        }

        [UIAction("resize-change")]
        private void OnResizeModeChanged(AvatarResizeMode value)
        {
            Plugin.settings.resizeMode = value;
            AvatarManager.instance.ResizeCurrentAvatar();
        }

        [UIAction("floor-adjust-change")]
        private void OnFloorHeightAdjustChanged(bool value)
        {
            Plugin.settings.enableFloorAdjust = value;
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
            if (Plugin.settings.useAutomaticFullBodyCalibration)
            {
                AvatarManager.instance.avatarTailor.CalibrateFullBodyTrackingAuto();
            }
            else if (!_calibrating)
            {
                AvatarManager.instance.currentlySpawnedAvatar.tracking.isCalibrationModeEnabled = true;
                _calibrating = true;
                calibrateButtonText.text = "Save";

                _waistSphere = CreateCalibrationSphere();
                _leftFootSphere = CreateCalibrationSphere();
                _rightFootSphere = CreateCalibrationSphere();
            }
            else
            {
                AvatarManager.instance.avatarTailor.CalibrateFullBodyTrackingManual(AvatarManager.instance.currentlySpawnedAvatar);
                AvatarManager.instance.currentlySpawnedAvatar.tracking.isCalibrationModeEnabled = false;
                _calibrating = false;
                calibrateButtonText.text = "Start Calibrating";

                Destroy(_waistSphere.gameObject);
                Destroy(_leftFootSphere.gameObject);
                Destroy(_rightFootSphere.gameObject);
            }
        }

        [UIAction("calibrate-fbt-on-start-change")]
        private void OnCalibrateFullBodyTrackingOnStartChanged(bool value)
        {
            Plugin.settings.calibrateFullBodyTrackingOnStart = value;
        }

        [UIAction("clear-fbt-calibration-data-click")]
        private void OnClearFullBodyTrackingCalibrationDataClicked()
        {
            AvatarManager.instance.avatarTailor.ClearFullBodyTrackingData();
        }

        #endregion

        private Transform _waistSphere;
        private Transform _leftFootSphere;
        private Transform _rightFootSphere;

        private Transform CreateCalibrationSphere()
        { 
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.layer = AvatarLayers.AlwaysVisible;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material = _sphereMaterial;

            return sphere.transform;
        }

        private void Update()
        {
            if (_calibrating)
            {
                TrackedDeviceManager input = PersistentSingleton<TrackedDeviceManager>.instance;

                if (input.waist.tracked)
                {
                    _waistSphere.gameObject.SetActive(true);
                    _waistSphere.position = input.waist.position;
                    _waistSphere.rotation = input.waist.rotation;
                }
                else
                {
                    _waistSphere.gameObject.SetActive(false);
                }

                if (input.leftFoot.tracked)
                {
                    _leftFootSphere.gameObject.SetActive(true);
                    _leftFootSphere.position = input.leftFoot.position;
                    _leftFootSphere.rotation = input.leftFoot.rotation;
                }
                else
                {
                    _leftFootSphere.gameObject.SetActive(false);
                }

                if (input.rightFoot.tracked)
                {
                    _rightFootSphere.gameObject.SetActive(true);
                    _rightFootSphere.position = input.rightFoot.position;
                    _rightFootSphere.rotation = input.rightFoot.rotation;
                }
                else
                {
                    _rightFootSphere.gameObject.SetActive(false);
                }
            }
        }

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
            var armSpan = Vector3.Distance(playerInput.leftHand.position, playerInput.rightHand.position);

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
                Plugin.settings.playerArmSpan = maxMeasuredArmSpan;
                isMeasuring = false;

                if (Plugin.settings.resizeMode == AvatarResizeMode.ArmSpan)
                {
                    AvatarManager.instance.ResizeCurrentAvatar();
                }
            }
        }

        #endregion
    }
}