using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    /// <summary>
    /// The player's <see cref="IAvatarInput"/> with calibration and other settings applied.
    /// </summary>
    public class VRPlayerInput : IInitializable, IDisposable, IAvatarInput
    {
        public static readonly float kDefaultPlayerArmSpan = 1.8f;

        public bool allowMaintainPelvisPosition => _internalPlayerInput.allowMaintainPelvisPosition;

        public event Action inputChanged;

        private VRPlayerInputInternal _internalPlayerInput;
        private TrackingHelper _trackingHelper;

        internal VRPlayerInput(VRPlayerInputInternal internalPlayerInput, TrackingHelper trackingHelper)
        {
            _internalPlayerInput = internalPlayerInput;
            _trackingHelper = trackingHelper;
        }

        public void Initialize()
        {
            _internalPlayerInput.inputChanged += inputChanged;
        }

        public void Dispose()
        {
            _internalPlayerInput.inputChanged -= inputChanged;
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl)
        {
            return _internalPlayerInput.TryGetFingerCurl(use, out curl);
        }

        public bool TryGetPose(DeviceUse use, out Pose pose)
        {
            if (!_internalPlayerInput.TryGetPose(use, out pose)) return false;

            _trackingHelper.ApplyRoomAdjust(ref pose.position, ref pose.rotation);

            return true;
        }
    }
}
