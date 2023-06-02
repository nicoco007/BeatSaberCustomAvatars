using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Zenject;

namespace CustomAvatar.UI
{
    internal class TrackerStatusHost : ViewControllerHost
    {
        private static readonly InternedString[] kFullBodyTrackingUsages = new InternedString[] { new InternedString("Waist"), new InternedString("LeftFoot"), new InternedString("RightFoot") };

        private readonly VRPlayerInputInternal _playerInput;

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private TrackerStatusHost(VRPlayerInputInternal playerInput)
        {
            _playerInput = playerInput;
        }

        protected bool trackersNotSupported => UnityEngine.XR.XRSettings.loadedDeviceName.IndexOf("OpenVR", System.StringComparison.OrdinalIgnoreCase) == -1 && !InputSystem.devices.Any(d => d.usages.Any(u => kFullBodyTrackingUsages.Contains(u)));

        protected bool noTrackersDetected => !trackersNotSupported && !_playerInput.TryGetUncalibratedPose(DeviceUse.Waist, out Pose _) && !_playerInput.TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose _) && !_playerInput.TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose _);

        protected string noTrackersDetectedMessage
        {
            get
            {
                string message = "No trackers detected. Did you assign roles to your trackers in SteamVR?";

                if (UnityEngine.XR.XRSettings.loadedDeviceName.IndexOf("OpenXR", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    message += "\nIf you turned your trackers on after launching the game, you will need to restart the game.";
                }

                return message;
            }
        }

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _playerInput.inputChanged += OnInputChanged;
            InputSystem.onDeviceChange += OnInputSystemDeviceChange;
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _playerInput.inputChanged -= OnInputChanged;
            InputSystem.onDeviceChange -= OnInputSystemDeviceChange;
        }

        private void OnInputChanged()
        {
            NotifyPropertyChanged(nameof(noTrackersDetected));
        }

        private void OnInputSystemDeviceChange(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
        {
            NotifyPropertyChanged(nameof(trackersNotSupported));
            NotifyPropertyChanged(nameof(noTrackersDetected));
            NotifyPropertyChanged(nameof(noTrackersDetectedMessage));
        }
    }
}
