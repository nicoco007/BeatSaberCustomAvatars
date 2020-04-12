using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

#pragma warning disable 649 // disable "field is never assigned"
#pragma warning disable IDE0044 // disable "make field readonly"
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
namespace CustomAvatar.UI
{
    internal class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.Settings.bsml";

        private static readonly int kColor = Shader.PropertyToID("_Color");

        private bool _calibrating;
        private Material _sphereMaterial;
        private Material _redMaterial;
        private Material _greenMaterial;
        private Material _blueMaterial;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        
        [Inject] private TrackedDeviceManager _trackedDeviceManager;
        [Inject] private AvatarManager _avatarManager;

        #region Components
        
        // text
        [UIComponent("arm-span")] private TextMeshProUGUI _armSpanLabel;
        [UIComponent("calibrate-button")] private TextMeshProUGUI _calibrateButtonText;
        [UIComponent("clear-button")] private TextMeshProUGUI _clearButtonText;

        // buttons
        [UIComponent("calibrate-button")] private Button _calibrateButton;
        [UIComponent("clear-button")] private Button _clearButton;

        // settings
        [UIComponent("visible-in-first-person")] private BoolSetting _visibleInFirstPerson;
        [UIComponent("resize-mode")] private ListSetting _resizeMode;
        [UIComponent("floor-adjust")] private BoolSetting _floorHeightAdjust;
        [UIComponent("camera-clip-plane")] private IncrementSetting _cameraNearClipPlane;
        [UIComponent("calibrate-fbt-on-start")] private BoolSetting _calibrateFullBodyTrackingOnStart;
        [UIComponent("automatic-calibration")] private BoolSetting _automaticCalibrationSetting;

        [UIComponent("automatic-calibration")] private HoverHint _automaticCalibrationHoverHint;

        #endregion

        #region Values

        [UIValue("resize-mode-options")] private readonly List<object> _resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };

        #endregion

        private Settings _settings;
        private ShaderLoader _shaderLoader;
        private ILogger _logger;

        [Inject]
        private void Inject(Settings settings, ShaderLoader shaderLoader, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _shaderLoader = shaderLoader;
            _logger = loggerFactory.CreateLogger<SettingsViewController>();
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            _visibleInFirstPerson.Value = _settings.isAvatarVisibleInFirstPerson;
            _resizeMode.Value = _settings.resizeMode;
            _floorHeightAdjust.Value = _settings.enableFloorAdjust;
            _calibrateFullBodyTrackingOnStart.Value = _settings.calibrateFullBodyTrackingOnStart;
            _cameraNearClipPlane.Value = _settings.cameraNearClipPlane;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnInputDevicesChanged();

            _armSpanLabel.SetText($"{_settings.playerArmSpan:0.00} m");

            _sphereMaterial = new Material(_shaderLoader.unlitShader);
            _redMaterial = new Material(_shaderLoader.unlitShader);
            _greenMaterial = new Material(_shaderLoader.unlitShader);
            _blueMaterial = new Material(_shaderLoader.unlitShader);

            _redMaterial.SetColor(kColor, new Color(0.8f, 0, 0, 1));
            _greenMaterial.SetColor(kColor, new Color(0, 0.8f, 0, 1));
            _blueMaterial.SetColor(kColor, new Color(0, 0.5f, 1, 1));

            _avatarManager.avatarChanged += OnAvatarChanged;

            // TODO unsub from all these
            _trackedDeviceManager.deviceAdded += (state, use) => OnInputDevicesChanged();
            _trackedDeviceManager.deviceRemoved += (state, use) => OnInputDevicesChanged();
            _trackedDeviceManager.deviceTrackingAcquired += (state, use) => OnInputDevicesChanged();
            _trackedDeviceManager.deviceTrackingLost += (state, use) => OnInputDevicesChanged();
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            _avatarManager.avatarChanged -= OnAvatarChanged;

            DisableCalibrationMode(false);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            DisableCalibrationMode(false);

            if (avatar == null)
            {
                _clearButton.interactable = false;
                _calibrateButton.interactable = false;
                _automaticCalibrationSetting.SetInteractable(false);
                _automaticCalibrationHoverHint.text = "No avatar selected";

                return;
            }

            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.avatar.fullPath);

            _clearButton.interactable = !_currentAvatarSettings.fullBodyCalibration.isDefault;
            // TODO same here
            _calibrateButton.interactable = _avatarManager.currentlySpawnedAvatar.avatar.isIKAvatar && (_trackedDeviceManager.waist.tracked || _trackedDeviceManager.leftFoot.tracked || _trackedDeviceManager.rightFoot.tracked);

            _automaticCalibrationSetting.Value = _currentAvatarSettings.useAutomaticCalibration;
            _automaticCalibrationSetting.SetInteractable(avatar.avatar.descriptor.supportsAutomaticCalibration);
            _automaticCalibrationHoverHint.text = avatar.avatar.descriptor.supportsAutomaticCalibration ? "Use automatic calibration instead of manual calibration" : "Not supported by current avatar";
        }

        private void OnInputDevicesChanged()
        {
            // TODO check targets exist on avatar, e.g. isFbtCapable
            _calibrateButton.interactable = (_avatarManager.currentlySpawnedAvatar?.avatar.isIKAvatar ?? false) && (_trackedDeviceManager.waist.tracked || _trackedDeviceManager.leftFoot.tracked || _trackedDeviceManager.rightFoot.tracked);
        }

        #region Actions

        [UIAction("visible-first-person-change")]
        private void OnVisibleInFirstPersonChanged(bool value)
        {
            _settings.isAvatarVisibleInFirstPerson = value;
            _avatarManager.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
        }

        [UIAction("resize-mode-change")]
        private void OnResizeModeChanged(AvatarResizeMode value)
        {
            _settings.resizeMode = value;
            _avatarManager.ResizeCurrentAvatar();
        }

        [UIAction("floor-adjust-change")]
        private void OnFloorHeightAdjustChanged(bool value)
        {
            _settings.enableFloorAdjust = value;
            _avatarManager.ResizeCurrentAvatar();
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
            if (_currentAvatarSettings.useAutomaticCalibration)
            {
                _avatarManager.avatarTailor.CalibrateFullBodyTrackingAuto(_avatarManager.currentlySpawnedAvatar);
                _clearButton.interactable = !_currentAvatarSettings.fullBodyCalibration.isDefault;
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
            _settings.calibrateFullBodyTrackingOnStart = value;
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
                _avatarManager.avatarTailor.ClearFullBodyTrackingData(_avatarManager.currentlySpawnedAvatar);
                _clearButton.interactable = false;
            }
        }

        [UIAction("automatic-calibration-change")]
        private void OnAutomaticCalibrationChanged(bool value)
        {
            DisableCalibrationMode(false);
            _currentAvatarSettings.useAutomaticCalibration = value;
        }

        [UIAction("camera-clip-plane-change")]
        private void OnCameraClipPlaneChanged(float value)
        {
            _settings.cameraNearClipPlane = value;

            // TODO logic in view controller is not ideal
            Camera mainCamera = Camera.main;

            if (mainCamera)
            {
                mainCamera.nearClipPlane = value;
            }
            else
            {
                _logger.Error("Could not find main camera!");
            }
        }

        #endregion

        private GameObject _waistSphere;
        private GameObject _leftFootSphere;
        private GameObject _rightFootSphere;

        private void EnableCalibrationMode()
        {
            _avatarManager.currentlySpawnedAvatar.EnableCalibrationMode();;
            _calibrating = true;
            _calibrateButtonText.text = "Save";
            _clearButtonText.text = "Cancel";
            _clearButton.interactable = true;

            _waistSphere = CreateCalibrationSphere();
            _leftFootSphere = CreateCalibrationSphere();
            _rightFootSphere = CreateCalibrationSphere();
        }

        private void DisableCalibrationMode(bool save)
        {
            if (_avatarManager.currentlySpawnedAvatar != null)
            {
                if (save)
                {
                    _avatarManager.avatarTailor.CalibrateFullBodyTrackingManual(_avatarManager.currentlySpawnedAvatar);
                }
                
                _avatarManager.currentlySpawnedAvatar.DisableCalibrationMode();
            }

            Destroy(_waistSphere);
            Destroy(_leftFootSphere);
            Destroy(_rightFootSphere);

            _calibrating = false;
            _calibrateButtonText.text = "Calibrate";
            _clearButtonText.text = "Clear";
            _clearButton.interactable = !_currentAvatarSettings?.fullBodyCalibration.isDefault ?? false;
        }

        private GameObject CreateCalibrationSphere()
        { 
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.layer = AvatarLayers.AlwaysVisible;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material = _sphereMaterial;

            CreateAxis(new Vector3(0.5f, 0, 0), new Vector3(1f, 0.1f, 0.1f), _redMaterial, sphere.transform);
            CreateAxis(new Vector3(0, 0.5f, 0), new Vector3(0.1f, 1f, 0.1f), _greenMaterial, sphere.transform);
            CreateAxis(new Vector3(0, 0, 0.5f), new Vector3(0.1f, 0.1f, 1f), _blueMaterial, sphere.transform);

            return sphere;
        }

        private void CreateAxis(Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject axis = GameObject.CreatePrimitive(PrimitiveType.Cube);

            axis.layer = AvatarLayers.AlwaysVisible;
            axis.transform.localPosition = position;
            axis.transform.localScale = scale;
            axis.GetComponent<Renderer>().material = material;
            axis.transform.SetParent(parent, false);
        }

        private void Update()
        {
            if (_calibrating)
            {
                if (_trackedDeviceManager.waist.tracked)
                {
                    _waistSphere.SetActive(true);
                    _waistSphere.transform.position = _trackedDeviceManager.waist.position;
                    _waistSphere.transform.rotation = _trackedDeviceManager.waist.rotation;
                }
                else
                {
                    _waistSphere.SetActive(false);
                }

                if (_trackedDeviceManager.leftFoot.tracked)
                {
                    _leftFootSphere.SetActive(true);
                    _leftFootSphere.transform.position = _trackedDeviceManager.leftFoot.position;
                    _leftFootSphere.transform.rotation = _trackedDeviceManager.leftFoot.rotation;
                }
                else
                {
                    _leftFootSphere.SetActive(false);
                }

                if (_trackedDeviceManager.rightFoot.tracked)
                {
                    _rightFootSphere.SetActive(true);
                    _rightFootSphere.transform.position = _trackedDeviceManager.rightFoot.position;
                    _rightFootSphere.transform.rotation = _trackedDeviceManager.rightFoot.rotation;
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
            var armSpan = Vector3.Distance(_trackedDeviceManager.leftHand.position, _trackedDeviceManager.rightHand.position);

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
                _settings.playerArmSpan = _maxMeasuredArmSpan;
                _isMeasuring = false;

                if (_settings.resizeMode == AvatarResizeMode.ArmSpan)
                {
                    _avatarManager.ResizeCurrentAvatar();
                }
            }
        }

        #endregion
    }
}