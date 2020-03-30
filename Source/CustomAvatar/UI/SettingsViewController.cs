using System.Collections.Generic;
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
    internal class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.SettingsViewController.bsml";

        private readonly TrackedDeviceManager _playerInput = PersistentSingleton<TrackedDeviceManager>.instance;
        private bool _calibrating;
        private Material _sphereMaterial;

        #region Components
        
        [UIComponent("arm-span")] private TextMeshProUGUI _armSpanLabel;
        [UIComponent("calibrate-button")] private TextMeshProUGUI _calibrateButtonText;
        [UIComponent("clear-button")] private TextMeshProUGUI _clearButtonText;

        #endregion

        #region Properties

        [UIValue("resize-options")] private readonly List<object> _resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };

        #endregion

        #region Values

        [UIValue("visible-first-person-value")] private bool _visibleInFirstPerson;
        [UIValue("resize-value")] private AvatarResizeMode _resizeMode;
        [UIValue("floor-adjust-value")] private bool _floorHeightAdjust;
        [UIValue("calibrate-fbt-on-start")] private bool _calibrateFullBodyTrackingOnStart;

        #endregion

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            _visibleInFirstPerson = SettingsManager.settings.isAvatarVisibleInFirstPerson;
            _resizeMode = SettingsManager.settings.resizeMode;
            _floorHeightAdjust = SettingsManager.settings.enableFloorAdjust;
            _calibrateFullBodyTrackingOnStart = SettingsManager.settings.calibrateFullBodyTrackingOnStart;

            base.DidActivate(firstActivation, type);

            _armSpanLabel.SetText($"{SettingsManager.settings.playerArmSpan:0.00} m");

            _sphereMaterial = new Material(ShaderLoader.unlitShader);
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            DisableCalibrationMode(false);
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
            if (SettingsManager.settings.useAutomaticFullBodyCalibration)
            {
                AvatarManager.instance.avatarTailor.CalibrateFullBodyTrackingAuto(AvatarManager.instance.currentlySpawnedAvatar);
            }
            else if (!_calibrating)
            {
                EnableCalibrationMode();
            }
            else
            {
                DisableCalibrationMode(true);
            }
        }

        [UIAction("calibrate-fbt-on-start-change")]
        private void OnCalibrateFullBodyTrackingOnStartChanged(bool value)
        {
            SettingsManager.settings.calibrateFullBodyTrackingOnStart = value;
        }

        [UIAction("clear-fbt-calibration-data-click")]
        private void OnClearFullBodyTrackingCalibrationDataClicked()
        {
            if (_calibrating)
            {
                DisableCalibrationMode(false);
            }
            else
            {
                AvatarManager.instance.avatarTailor.ClearFullBodyTrackingData(AvatarManager.instance.currentlySpawnedAvatar);
            }
        }

        #endregion

        private GameObject _waistSphere;
        private GameObject _leftFootSphere;
        private GameObject _rightFootSphere;

        private void EnableCalibrationMode()
        {
            AvatarManager.instance.currentlySpawnedAvatar.tracking.isCalibrationModeEnabled = true;
            _calibrating = true;
            _calibrateButtonText.text = "Save";
            _clearButtonText.text = "Cancel";

            _waistSphere = CreateCalibrationSphere();
            _leftFootSphere = CreateCalibrationSphere();
            _rightFootSphere = CreateCalibrationSphere();
        }

        private void DisableCalibrationMode(bool save)
        {
            if (save)
            {
                AvatarManager.instance.avatarTailor.CalibrateFullBodyTrackingManual(AvatarManager.instance.currentlySpawnedAvatar);
            }

            Destroy(_waistSphere);
            Destroy(_leftFootSphere);
            Destroy(_rightFootSphere);

            AvatarManager.instance.currentlySpawnedAvatar.tracking.isCalibrationModeEnabled = false;
            _calibrating = false;
            _calibrateButtonText.text = "Calibrate";
            _clearButtonText.text = "Clear";
        }

        private GameObject CreateCalibrationSphere()
        { 
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.layer = AvatarLayers.AlwaysVisible;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material = _sphereMaterial;

            return sphere;
        }

        private void Update()
        {
            if (_calibrating)
            {
                if (_playerInput.waist.tracked)
                {
                    _waistSphere.SetActive(true);
                    _waistSphere.transform.position = _playerInput.waist.position;
                    _waistSphere.transform.rotation = _playerInput.waist.rotation;
                }
                else
                {
                    _waistSphere.SetActive(false);
                }

                if (_playerInput.leftFoot.tracked)
                {
                    _leftFootSphere.SetActive(true);
                    _leftFootSphere.transform.position = _playerInput.leftFoot.position;
                    _leftFootSphere.transform.rotation = _playerInput.leftFoot.rotation;
                }
                else
                {
                    _leftFootSphere.SetActive(false);
                }

                if (_playerInput.rightFoot.tracked)
                {
                    _rightFootSphere.SetActive(true);
                    _rightFootSphere.transform.position = _playerInput.rightFoot.position;
                    _rightFootSphere.transform.rotation = _playerInput.rightFoot.rotation;
                }
                else
                {
                    _rightFootSphere.SetActive(false);
                }
            }
        }

        #region Arm Span Measurement
        
        private const float kMinArmSpan = 0.5f;

        private bool _isMeasuring;
        private float _maxMeasuredArmSpan;
        private float _lastUpdateTime;

        private void MeasureArmSpan()
        {
            if (_isMeasuring) return;

            _isMeasuring = true;
            _maxMeasuredArmSpan = kMinArmSpan;
            _lastUpdateTime = Time.timeSinceLevelLoad;

            InvokeRepeating(nameof(ScanArmSpan), 0.0f, 0.1f);
        }

        private void ScanArmSpan()
        {
            var armSpan = Vector3.Distance(_playerInput.leftHand.position, _playerInput.rightHand.position);

            if (armSpan > _maxMeasuredArmSpan)
            {
                _maxMeasuredArmSpan = armSpan;
                _lastUpdateTime = Time.timeSinceLevelLoad;
            }

            if (Time.timeSinceLevelLoad - _lastUpdateTime < 2.0f)
            {
                _armSpanLabel.SetText($"Measuring... {_maxMeasuredArmSpan:0.00} m");
            }
            else
            {
                CancelInvoke(nameof(ScanArmSpan));
                _armSpanLabel.SetText($"{_maxMeasuredArmSpan:0.00} m");
                SettingsManager.settings.playerArmSpan = _maxMeasuredArmSpan;
                _isMeasuring = false;

                if (SettingsManager.settings.resizeMode == AvatarResizeMode.ArmSpan)
                {
                    AvatarManager.instance.ResizeCurrentAvatar();
                }
            }
        }

        #endregion
    }
}