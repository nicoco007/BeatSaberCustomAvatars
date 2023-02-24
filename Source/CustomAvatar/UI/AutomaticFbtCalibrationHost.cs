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
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using JetBrains.Annotations;

namespace CustomAvatar.UI
{
    internal class AutomaticFbtCalibrationHost : ViewControllerHost
    {
        #region Values

        protected readonly TrackerStatusHost trackerStatusHost;

        #endregion

        private readonly PlayerAvatarManager _avatarManager;
        private readonly CalibrationData _calibrationData;
        private readonly TrackingRig _trackingRig;

        internal AutomaticFbtCalibrationHost(TrackerStatusHost trackerStatusHost, PlayerAvatarManager avatarManager, CalibrationData calibrationData, TrackingRig trackingRig)
        {
            this.trackerStatusHost = trackerStatusHost;

            _avatarManager = avatarManager;
            _calibrationData = calibrationData;
            _trackingRig = trackingRig;
        }

        protected bool isCalibrateButtonEnabled => _trackingRig.areAnyFullBodyTrackersTracking && _trackingRig.activeCalibrationMode != CalibrationMode.Automatic;

        protected string calibrateButtonHoverHint => _trackingRig.areAnyFullBodyTrackersTracking ? "Start full body calibration" : "No trackers detected";

        protected bool isClearButtonEnabled => _calibrationData.automaticCalibration.isCalibrated;

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _avatarManager.avatarLoading += OnAvatarLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _trackingRig.trackingChanged += OnTrackingChanged;
            _trackingRig.activeCalibrationModeChanged += OnActiveCalibrationModeChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnTrackingChanged();

            trackerStatusHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _trackingRig.trackingChanged -= OnTrackingChanged;
            _trackingRig.activeCalibrationModeChanged -= OnActiveCalibrationModeChanged;

            trackerStatusHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void OnAvatarLoading(string filePath, string name)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }

        private void OnTrackingChanged()
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        private void OnActiveCalibrationModeChanged(CalibrationMode calibrationMode)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }

        [UsedImplicitly]
        private void OnCalibrateAutoFullBodyTrackingClicked()
        {
            _trackingRig.BeginCalibration(CalibrationMode.Automatic);

            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }

        [UsedImplicitly]
        private void OnClearAutoFullBodyTrackingCalibrationDataClicked()
        {
            _trackingRig.ClearCalibrationData(CalibrationMode.Automatic);

            NotifyPropertyChanged(nameof(isClearButtonEnabled));
        }
    }
}
