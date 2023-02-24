//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Avatar;
using CustomAvatar.Tracking;
using CustomAvatar.Player;
using CustomAvatar.Configuration;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CustomAvatar.UI
{
    internal class AvatarSpecificSettingsHost : ViewControllerHost
    {
        #region Values

        protected readonly List<object> calibrationModeOptions = new() { CalibrationMode.None, CalibrationMode.Automatic, CalibrationMode.Manual };
        protected readonly TrackerStatusHost trackerStatusHost;

        #endregion

        private readonly PlayerAvatarManager _avatarManager;
        private readonly TrackingRig _trackingRig;
        private readonly CalibrationData _calibrationData;

        private bool _isLoaderActive;

        internal AvatarSpecificSettingsHost(TrackerStatusHost trackerStatusHost, PlayerAvatarManager avatarManager, TrackingRig trackingRig, CalibrationData calibrationData)
        {
            this.trackerStatusHost = trackerStatusHost;

            _avatarManager = avatarManager;
            _trackingRig = trackingRig;
            _calibrationData = calibrationData;
        }

        protected bool areCurrentAvatarSettingsLoaded => _avatarManager.currentlySpawnedAvatar != null;

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
            get => _avatarManager.ignoreFirstPersonExclusions;
            set
            {
                _avatarManager.ignoreFirstPersonExclusions = value;
                NotifyPropertyChanged();
            }
        }

        protected CalibrationMode calibrationMode
        {
            get => _trackingRig.calibrationMode;
            set
            {
                _trackingRig.calibrationMode = value;
                NotifyPropertyChanged();
            }
        }

        protected bool isCalibrateButtonEnabled => _avatarManager.currentlySpawnedAvatar != null && _trackingRig.activeCalibrationMode != CalibrationMode.Manual && _trackingRig.areAnyFullBodyTrackersTracking;

        protected string calibrateButtonHoverHint => _avatarManager.currentlySpawnedAvatar != null ? (_trackingRig.areAnyFullBodyTrackersTracking ? "Start full body calibration" : "No trackers detected") : "No avatar selected";

        protected bool isClearButtonEnabled => _avatarManager.currentlySpawnedAvatar != null && _calibrationData.GetAvatarManualCalibration(_avatarManager.currentlySpawnedAvatar).isCalibrated;

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _avatarManager.avatarLoading += OnAvatarLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _trackingRig.trackingChanged += OnTrackingChanged;
            _trackingRig.calibrationModeChanged += OnCalibrationModeChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnTrackingChanged();

            trackerStatusHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _trackingRig.trackingChanged -= OnTrackingChanged;

            trackerStatusHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void OnAvatarLoading(string fullPath, string name)
        {
            isLoaderActive = true;

            NotifyPropertyChanged(nameof(ignoreExclusions));
            NotifyPropertyChanged(nameof(calibrationMode));
            NotifyPropertyChanged(nameof(areCurrentAvatarSettingsLoaded));
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            isLoaderActive = false;

            NotifyPropertyChanged(nameof(ignoreExclusions));
            NotifyPropertyChanged(nameof(calibrationMode));
            NotifyPropertyChanged(nameof(areCurrentAvatarSettingsLoaded));
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        private void OnTrackingChanged()
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        private void OnCalibrationModeChanged(CalibrationMode _)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(calibrationMode));
        }

        [UsedImplicitly]
        private string CalibrationModeFormatter(object value) => value switch
        {
            CalibrationMode.None => "None",
            CalibrationMode.Automatic => "Automatic",
            CalibrationMode.Manual => "Manual",
            _ => null,
        };

        [UsedImplicitly]
        private void OnCalibrateFullBodyTrackingClicked()
        {
            _trackingRig.BeginCalibration(CalibrationMode.Manual);

            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        [UsedImplicitly]
        private void OnClearFullBodyTrackingCalibrationDataClicked()
        {
            _trackingRig.ClearCalibrationData(CalibrationMode.Manual);

            NotifyPropertyChanged(nameof(calibrateButtonHoverHint));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }
    }
}
