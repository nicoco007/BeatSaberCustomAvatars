using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.UI
{
    internal partial class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.Settings.bsml";

        private static readonly int kColor = Shader.PropertyToID("_Color");

        private bool _calibrating;
        private Material _sphereMaterial;
        private Material _redMaterial;
        private Material _greenMaterial;
        private Material _blueMaterial;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        
        private TrackedDeviceManager _trackedDeviceManager;
        private PlayerAvatarManager _avatarManager;
        private AvatarTailor _avatarTailor;
        private Settings _settings;
        private ShaderLoader _shaderLoader;
        private ILogger _logger;

        [Inject]
        private void Inject(TrackedDeviceManager trackedDeviceManager, PlayerAvatarManager avatarManager, AvatarTailor avatarTailor, Settings settings, ShaderLoader shaderLoader, ILoggerProvider loggerProvider)
        {
            _trackedDeviceManager = trackedDeviceManager;
            _avatarManager = avatarManager;
            _avatarTailor = avatarTailor;
            _settings = settings;
            _shaderLoader = shaderLoader;
            _logger = loggerProvider.CreateLogger<SettingsViewController>();
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
            OnInputDevicesChanged(null, DeviceUse.Unknown);

            _armSpanLabel.SetText($"{_settings.playerArmSpan:0.00} m");

            _sphereMaterial = new Material(_shaderLoader.unlitShader);
            _redMaterial = new Material(_shaderLoader.unlitShader);
            _greenMaterial = new Material(_shaderLoader.unlitShader);
            _blueMaterial = new Material(_shaderLoader.unlitShader);

            _redMaterial.SetColor(kColor, new Color(0.8f, 0, 0, 1));
            _greenMaterial.SetColor(kColor, new Color(0, 0.8f, 0, 1));
            _blueMaterial.SetColor(kColor, new Color(0, 0.5f, 1, 1));

            _pelvisOffset.Value = _settings.automaticCalibration.pelvisOffset;
            _leftFootOffset.Value = _settings.automaticCalibration.leftLegOffset;
            _rightFootOffset.Value = _settings.automaticCalibration.rightLegOffset;

            _autoClearButton.interactable = !_settings.automaticCalibration.isDefault;

            _avatarManager.avatarChanged += OnAvatarChanged;

            _trackedDeviceManager.deviceAdded += OnInputDevicesChanged;
            _trackedDeviceManager.deviceRemoved += OnInputDevicesChanged;
            _trackedDeviceManager.deviceTrackingAcquired += OnInputDevicesChanged;
            _trackedDeviceManager.deviceTrackingLost += OnInputDevicesChanged;
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            _avatarManager.avatarChanged -= OnAvatarChanged;

            _trackedDeviceManager.deviceAdded -= OnInputDevicesChanged;
            _trackedDeviceManager.deviceRemoved -= OnInputDevicesChanged;
            _trackedDeviceManager.deviceTrackingAcquired -= OnInputDevicesChanged;
            _trackedDeviceManager.deviceTrackingLost -= OnInputDevicesChanged;

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
            _calibrateButton.interactable = _avatarManager.currentlySpawnedAvatar.isIKAvatar && (_trackedDeviceManager.waist.tracked || _trackedDeviceManager.leftFoot.tracked || _trackedDeviceManager.rightFoot.tracked);

            _automaticCalibrationSetting.Value = _currentAvatarSettings.useAutomaticCalibration;
            _automaticCalibrationSetting.SetInteractable(avatar.avatar.descriptor.supportsAutomaticCalibration);
            _automaticCalibrationHoverHint.text = avatar.avatar.descriptor.supportsAutomaticCalibration ? "Use automatic calibration instead of manual calibration" : "Not supported by current avatar";
        }

        private void OnInputDevicesChanged(TrackedDeviceState state, DeviceUse use)
        {
            // TODO check targets exist on avatar, e.g. isFbtCapable
            _autoCalibrateButton.interactable = _trackedDeviceManager.waist.tracked || _trackedDeviceManager.leftFoot.tracked || _trackedDeviceManager.rightFoot.tracked;
            _calibrateButton.interactable     = _avatarManager.currentlySpawnedAvatar && _avatarManager.currentlySpawnedAvatar.isIKAvatar && (_trackedDeviceManager.waist.tracked || _trackedDeviceManager.leftFoot.tracked || _trackedDeviceManager.rightFoot.tracked);
        }
    }
}