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

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomAvatar.Tracking;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Zenject;

namespace CustomAvatar.UI
{
    internal class TrackerStatusHost : ViewControllerHost
    {
        private static readonly InternedString[] kFullBodyTrackingUsages = new InternedString[] { new InternedString("Waist"), new InternedString("LeftFoot"), new InternedString("RightFoot") };

        private readonly TrackingRig _trackingRig;

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private TrackerStatusHost(TrackingRig trackingRig)
        {
            _trackingRig = trackingRig;
        }

        protected bool trackersNotSupported => _isOpenXR && !InputSystem.devices.Any(d => d.usages.Any(u => kFullBodyTrackingUsages.Contains(u)));

        protected bool noTrackersDetected => !trackersNotSupported && !_trackingRig.areAnyFullBodyTrackersTracking;

        protected string noTrackersDetectedMessage
        {
            get
            {
                string message = "No trackers detected. Did you assign roles to your trackers in SteamVR?";

                if (_isOpenXR)
                {
                    message += "\n" + openXRHint;
                }

                return message;
            }
        }

        protected bool showOpenXRHint => _isOpenXR && !trackersNotSupported && !noTrackersDetected;

        protected string openXRHint { get; } = "If you turned on your tracker(s) after launching the game, you will need to restart the game.";

        private bool _isOpenXR => UnityEngine.XR.XRSettings.loadedDeviceName.IndexOf("OpenXR", System.StringComparison.OrdinalIgnoreCase) != -1;

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _trackingRig.trackingChanged += OnTrackingChanged;
            InputSystem.onDeviceChange += OnInputSystemDeviceChange;
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _trackingRig.trackingChanged -= OnTrackingChanged;
            InputSystem.onDeviceChange -= OnInputSystemDeviceChange;
        }

        private void OnTrackingChanged()
        {
            NotifyPropertyChanged(nameof(noTrackersDetected));
            NotifyPropertyChanged(nameof(showOpenXRHint));
        }

        private void OnInputSystemDeviceChange(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
        {
            NotifyPropertyChanged(nameof(trackersNotSupported));
            NotifyPropertyChanged(nameof(noTrackersDetected));
            NotifyPropertyChanged(nameof(noTrackersDetectedMessage));
            NotifyPropertyChanged(nameof(showOpenXRHint));
        }
    }
}
