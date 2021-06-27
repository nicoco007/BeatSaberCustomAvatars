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
using CustomAvatar.Tracking;
using CustomAvatar.Player;
using CustomAvatar.Configuration;
using UnityEngine;

namespace CustomAvatar.UI
{
    internal class AvatarSpecificSettingsHost : ViewControllerHost
    {
        private readonly PlayerAvatarManager _avatarManager;
        private readonly VRPlayerInputInternal _playerInput;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;
        private readonly ManualCalibrationHelper _manualCalibrationHelper;

        private bool _isLoaderActive;
        private bool _calibrating;
        private SpawnedAvatar _currentAvatar;
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

        internal bool useAutomaticCalibration
        {
            get => _currentAvatarSettings?.useAutomaticCalibration ?? false;
            set
            {
                if (_currentAvatarSettings == null) return;

                _currentAvatarSettings.useAutomaticCalibration.value = value;
                NotifyPropertyChanged();
            }
        }

        protected bool isAvatarSpecificSettingsAvailable => _currentAvatar && _currentAvatarSettings != null && _currentAvatarManualCalibration != null;

        protected bool isLoaderActive
        {
            get => _isLoaderActive;
            set
            {
                _isLoaderActive = value;
                NotifyPropertyChanged();
            }
        }

        protected bool ignoreExclusions
        {
            get => _currentAvatarSettings?.ignoreExclusions ?? false;
            set
            {
                if (_currentAvatarSettings == null) return;

                _currentAvatarSettings.ignoreExclusions.value = value;
                NotifyPropertyChanged();
            }
        }

        protected bool bypassCalibration
        {
            get => _currentAvatarSettings?.bypassCalibration ?? false;
            set
            {
                if (_currentAvatarSettings == null) return;

                _currentAvatarSettings.bypassCalibration.value = value;
                NotifyPropertyChanged();
            }
        }

        protected bool isAutomaticCalibrationAvailable => isAvatarSpecificSettingsAvailable && (_currentAvatar?.prefab.descriptor.supportsAutomaticCalibration ?? false);

        protected string useAutomaticCalibrationHoverHint => isAutomaticCalibrationAvailable ? "Use automatic calibration instead of manual calibration." : "Not supported by current avatar";

        protected bool isCalibrateButtonEnabled => _currentAvatar && _areTrackersDetected;

        protected string calibrateButtonText => _calibrating ? "Save" : (_currentAvatarManualCalibration?.isCalibrated == true ? "Recalibrate" : "Calibrate");

        protected string calibrateButtonHoverHint => _currentAvatar ? (_areTrackersDetected ? (_calibrating ? "Save full body calibration" : "Start full body calibration") : "No trackers detected") : "No avatar selected";

        protected bool isClearButtonEnabled => _calibrating || _currentAvatarManualCalibration?.isCalibrated == true;

        protected string clearButtonText => _calibrating ? "Cancel" : "Clear";

        protected string clearButtonHoverHint => _calibrating ? "Cancel calibration" : "Clear calibration data";

        private bool _areTrackersDetected => _playerInput.TryGetUncalibratedPose(DeviceUse.Waist, out Pose _) || _playerInput.TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose _) || _playerInput.TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose _);

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _playerInput.inputChanged += OnInputChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnInputChanged();
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            DisableCalibrationMode(false);

            _avatarManager.avatarStartedLoading -= OnAvatarStartedLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _playerInput.inputChanged -= OnInputChanged;
        }

        private void OnAvatarStartedLoading(string path)
        {
            DisableCalibrationMode(false);

            isLoaderActive = true;

            _currentAvatar = null;
            _currentAvatarSettings = null;
            _currentAvatarManualCalibration = null;

            NotifyPropertyChanged(nameof(ignoreExclusions));
            NotifyPropertyChanged(nameof(bypassCalibration));
            NotifyPropertyChanged(nameof(useAutomaticCalibration));
            NotifyPropertyChanged(nameof(isAvatarSpecificSettingsAvailable));
            NotifyPropertyChanged(nameof(isAutomaticCalibrationAvailable));
            NotifyPropertyChanged(nameof(useAutomaticCalibrationHoverHint));
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonText));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            isLoaderActive = false;

            if (!avatar)
            {
                _currentAvatar = null;
                _currentAvatarSettings = null;
                _currentAvatarManualCalibration = null;
            }
            else
            {
                _currentAvatar = avatar;
                _currentAvatarSettings = _settings.GetAvatarSettings(avatar.prefab.fileName);
                _currentAvatarManualCalibration = _calibrationData.GetAvatarManualCalibration(avatar.prefab.fileName);
            }

            NotifyPropertyChanged(nameof(ignoreExclusions));
            NotifyPropertyChanged(nameof(bypassCalibration));
            NotifyPropertyChanged(nameof(useAutomaticCalibration));
            NotifyPropertyChanged(nameof(isAvatarSpecificSettingsAvailable));
            NotifyPropertyChanged(nameof(isAutomaticCalibrationAvailable));
            NotifyPropertyChanged(nameof(useAutomaticCalibrationHoverHint));
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonText));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        private void OnInputChanged()
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
        }

        #region Actions
#pragma warning disable IDE0051

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
                NotifyPropertyChanged(nameof(calibrateButtonText));
                NotifyPropertyChanged(nameof(isClearButtonEnabled));
            }
        }

#pragma warning restore IDE0051
        #endregion

        private void EnableCalibrationMode()
        {
            if (!_avatarManager.currentlySpawnedAvatar) return;

            SetCalibrationMode(true);
        }

        private void DisableCalibrationMode(bool save)
        {
            if (_avatarManager.currentlySpawnedAvatar && save)
            {
                _playerInput.CalibrateFullBodyTrackingManual(_avatarManager.currentlySpawnedAvatar);

                useAutomaticCalibration = false;
            }

            SetCalibrationMode(false);
        }

        private void SetCalibrationMode(bool enabled)
        {
            _calibrating = enabled;
            _manualCalibrationHelper.enabled = enabled;
            _playerInput.isCalibrationModeEnabled = enabled;

            NotifyPropertyChanged(nameof(calibrateButtonText));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
            NotifyPropertyChanged(nameof(clearButtonText));
        }
    }
}
