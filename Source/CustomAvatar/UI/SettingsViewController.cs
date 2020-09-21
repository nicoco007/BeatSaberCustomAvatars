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

using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

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
        
        private TrackedDeviceManager _trackedDeviceManager;
        private PlayerAvatarManager _avatarManager;
        private AvatarTailor _avatarTailor;
        private Settings _settings;
        private CalibrationData _calibrationData;
        private ShaderLoader _shaderLoader;
        private ILogger<SettingsViewController> _logger;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        private CalibrationData.FullBodyCalibration _currentAvatarManualCalibration;

        [Inject]
        private void Inject(TrackedDeviceManager trackedDeviceManager, PlayerAvatarManager avatarManager, AvatarTailor avatarTailor, Settings settings, CalibrationData calibrationData, ShaderLoader shaderLoader, ILoggerProvider loggerProvider)
        {
            _trackedDeviceManager = trackedDeviceManager;
            _avatarManager = avatarManager;
            _avatarTailor = avatarTailor;
            _settings = settings;
            _calibrationData = calibrationData;
            _shaderLoader = shaderLoader;
            _logger = loggerProvider.CreateLogger<SettingsViewController>();
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            _visibleInFirstPerson.CheckboxValue = _settings.isAvatarVisibleInFirstPerson;
            _resizeMode.Value = _settings.resizeMode;
            _enableLocomotion.CheckboxValue = _settings.enableLocomotion;
            _floorHeightAdjust.CheckboxValue = _settings.enableFloorAdjust;
            _moveFloorWithRoomAdjust.CheckboxValue = _settings.moveFloorWithRoomAdjust;
            _calibrateFullBodyTrackingOnStart.CheckboxValue = _settings.calibrateFullBodyTrackingOnStart;
            _cameraNearClipPlane.Value = _settings.cameraNearClipPlane;

            UpdateUI(_avatarManager.currentlySpawnedAvatar?.avatar);
            OnInputDevicesChanged(null, DeviceUse.Unknown);

            _armSpanLabel.SetText($"{_settings.playerArmSpan:0.00} m");

            if (_shaderLoader.unlitShader)
            {
                _sphereMaterial = new Material(_shaderLoader.unlitShader);
                _redMaterial = new Material(_shaderLoader.unlitShader);
                _greenMaterial = new Material(_shaderLoader.unlitShader);
                _blueMaterial = new Material(_shaderLoader.unlitShader);

                _redMaterial.SetColor(kColor, new Color(0.8f, 0, 0, 1));
                _greenMaterial.SetColor(kColor, new Color(0, 0.8f, 0, 1));
                _blueMaterial.SetColor(kColor, new Color(0, 0.5f, 1, 1));
            }
            else
            {
                _logger.Error("Unlit shader not loaded; manual calibration points may not be visible");
            }

            _pelvisOffset.Value = _settings.automaticCalibration.pelvisOffset;
            _footOffset.Value = _settings.automaticCalibration.legOffset;

            _waistTrackerPosition.Value = _settings.automaticCalibration.waistTrackerPosition;

            _autoClearButton.interactable = _calibrationData.automaticCalibration.isCalibrated;

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

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            UpdateUI(spawnedAvatar?.avatar);
        }

        private void UpdateUI(LoadedAvatar avatar)
        {
            DisableCalibrationMode(false);

            if (avatar == null)
            {
                _clearButton.interactable = false;
                _calibrateButton.interactable = false;
                _automaticCalibrationSetting.checkbox.interactable = false;
                _automaticCalibrationHoverHint.text = "No avatar selected";

                return;
            }

            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.fileName);
            _currentAvatarManualCalibration = _calibrationData.GetAvatarManualCalibration(avatar.fileName);

            UpdateCalibrationButtons(avatar);

            _ignoreExclusionsSetting.CheckboxValue = _currentAvatarSettings.ignoreExclusions;

            _bypassCalibration.CheckboxValue = _currentAvatarSettings.bypassCalibration;
            _bypassCalibration.checkbox.interactable = avatar.supportsFullBodyTracking;
            _bypassCalibrationHoverHint.text = avatar.supportsFullBodyTracking ? "Disable the need for calibration before full body tracking is applied." : "Not supported by current avatar";

            _automaticCalibrationSetting.CheckboxValue = _currentAvatarSettings.useAutomaticCalibration;
            _automaticCalibrationSetting.checkbox.interactable = avatar.descriptor.supportsAutomaticCalibration;
            _automaticCalibrationHoverHint.text = avatar.descriptor.supportsAutomaticCalibration ? "Use automatic calibration instead of manual calibration." : "Not supported by current avatar";
        }

        private void OnInputDevicesChanged(TrackedDeviceState state, DeviceUse use)
        {
            UpdateCalibrationButtons(_avatarManager.currentlySpawnedAvatar?.avatar);
        }

        private void UpdateCalibrationButtons(LoadedAvatar avatar)
        {
            if (!_trackedDeviceManager.waist.tracked && !_trackedDeviceManager.leftFoot.tracked && !_trackedDeviceManager.rightFoot.tracked)
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = false;
                _autoCalibrateButtonHoverHint.text = "No trackers detected";

                _calibrateButton.interactable = false;
                _clearButton.interactable = false;
                _calibrateButtonHoverHint.text = "No trackers detected";

                return;
            }

            bool isManualCalibrationPossible = avatar != null && avatar.isIKAvatar && avatar.supportsFullBodyTracking;
            bool isAutomaticCalibrationPossible = isManualCalibrationPossible && avatar.descriptor.supportsAutomaticCalibration;

            if (isAutomaticCalibrationPossible)
            {
                _autoCalibrateButton.interactable = true;
                _autoClearButton.interactable = _currentAvatarManualCalibration.isCalibrated;
                _autoCalibrateButtonHoverHint.text = "Calibrate full body tracking automatically";
            }
            else
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = false;
                _autoCalibrateButtonHoverHint.text = "Not supported by current avatar";
            }

            if (isManualCalibrationPossible)
            {
                _calibrateButton.interactable = true;
                _clearButton.interactable = _currentAvatarManualCalibration.isCalibrated;
                _calibrateButtonHoverHint.text = "Start manual full body calibration";
            }
            else
            {
                _calibrateButton.interactable = false;
                _clearButton.interactable = false;
                _calibrateButtonHoverHint.text = "Not supported by current avatar";
            }
        }
    }
}
