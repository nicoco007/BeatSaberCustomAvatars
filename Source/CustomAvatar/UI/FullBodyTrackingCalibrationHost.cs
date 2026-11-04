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

using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using JetBrains.Annotations;

namespace CustomAvatar.UI
{
    internal class FullBodyTrackingCalibrationHost : ViewControllerHost
    {
        protected readonly TrackerStatusHost trackerStatusHost;
        protected readonly AdjustHost adjustHost;

        private readonly TrackingRig _trackingRig;

        protected FullBodyTrackingCalibrationHost(TrackerStatusHost trackerStatusHost, CalibrationData calibrationData, TrackingRig trackingRig)
        {
            this.trackerStatusHost = trackerStatusHost;
            this.adjustHost = new AdjustHost(trackingRig, calibrationData);

            _trackingRig = trackingRig;
        }

        protected bool isCalibrateButtonEnabled => _trackingRig.areAnyFullBodyTrackersTracking && !_trackingRig.isCalibrating && !_trackingRig.isAdjusting;

        protected string calibrateButtonHoverHint => _trackingRig.areAnyFullBodyTrackersTracking ? "Start full body calibration" : "No trackers detected";

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _trackingRig.trackingChanged += OnTrackingChanged;
            _trackingRig.isCalibratingChanged += OnIsCalibratingChanged;
            _trackingRig.isAdjustingChanged += OnIsAdjustingChanged;

            OnTrackingChanged();

            adjustHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            trackerStatusHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _trackingRig.trackingChanged -= OnTrackingChanged;
            _trackingRig.isCalibratingChanged -= OnIsCalibratingChanged;
            _trackingRig.isAdjustingChanged -= OnIsAdjustingChanged;

            adjustHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            trackerStatusHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void OnTrackingChanged()
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }

        private void OnIsCalibratingChanged(bool isCalibrating)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }

        private void OnIsAdjustingChanged(bool isAdjusting)
        {
            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }

        [UsedImplicitly]
        private void OnCalibrateClicked()
        {
            _trackingRig.BeginCalibration();

            NotifyPropertyChanged(nameof(isCalibrateButtonEnabled));
        }
    }
}
