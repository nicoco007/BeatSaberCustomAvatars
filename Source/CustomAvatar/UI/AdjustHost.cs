//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using HMUI;

namespace CustomAvatar.UI
{
    internal class AdjustHost : ViewControllerHost
    {
        private readonly TrackingRig _trackingRig;
        private readonly PlayerAvatarManager _playerAvatarManager;
        private readonly CalibrationData _calibrationData;

        internal AdjustHost(TrackingRig trackingRig, PlayerAvatarManager playerAvatarManager, CalibrationData calibrationData)
        {
            _trackingRig = trackingRig;
            _playerAvatarManager = playerAvatarManager;
            _calibrationData = calibrationData;
        }

        protected bool isAdjustEnabled => _trackingRig.areAnyFullBodyTrackersTracking && !_trackingRig.isCalibrating && !_trackingRig.isAdjusting && _playerAvatarManager.currentlySpawnedAvatar != null && _playerAvatarManager.currentlySpawnedAvatar.prefab.isIKAvatar;

        protected bool isSaveEnabled => _trackingRig.isAdjusting;

        protected bool isClearEnabled => _calibrationData.manualOffset.isSet && !_trackingRig.isAdjusting;

        protected IList adjustModes { get; } = new string[] { "Position", "Rotation", "Position and Rotation" };

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _trackingRig.trackingChanged += OnTrackingChanged;
            _trackingRig.isCalibratingChanged += OnIsCalibratingChanged;
            _trackingRig.isAdjustingChanged += OnIsAdjustingChanged;

            _playerAvatarManager.avatarChanged += OnAvatarChanged;

            OnTrackingChanged();
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _trackingRig.trackingChanged -= OnTrackingChanged;
            _trackingRig.isCalibratingChanged -= OnIsCalibratingChanged;
            _trackingRig.isAdjustingChanged -= OnIsAdjustingChanged;

            _playerAvatarManager.avatarChanged -= OnAvatarChanged;

            SaveClicked();
        }

        protected void AdjustClicked()
        {
            _trackingRig.BeginAdjust();
        }

        protected void SaveClicked()
        {
            _trackingRig.EndAdjust();
        }

        protected void ClearClicked()
        {
            _trackingRig.ClearAdjust();

            NotifyPropertyChanged(nameof(isClearEnabled));
        }

        protected void AdjustModeTabSelected(TextSegmentedControl textSegmentedControl, int index)
        {
            _trackingRig.adjustMode = (AdjustMode)index;
        }

        private void OnTrackingChanged()
        {
            NotifyPropertyChanged(nameof(isAdjustEnabled));
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            NotifyPropertyChanged(nameof(isAdjustEnabled));
        }

        private void OnIsCalibratingChanged(bool isCalibrating)
        {
            NotifyPropertyChanged(nameof(isAdjustEnabled));
        }

        private void OnIsAdjustingChanged(bool isAdjusting)
        {
            NotifyPropertyChanged(nameof(isAdjustEnabled));
            NotifyPropertyChanged(nameof(isSaveEnabled));
            NotifyPropertyChanged(nameof(isClearEnabled));
        }
    }
}
