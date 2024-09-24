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

using System;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    /// <summary>
    /// The player's <see cref="IAvatarInput"/> with calibration and other settings applied.
    /// </summary>
    internal class VRPlayerInput : IInitializable, IDisposable, IAvatarInput
    {
        private readonly PlayerAvatarManager _avatarManager;
        private readonly TrackingRig _trackingRig;
        private readonly IFingerTrackingProvider _fingerTrackingProvider;

        internal VRPlayerInput(
            PlayerAvatarManager avatarManager,
            TrackingRig trackingRig,
            IFingerTrackingProvider fingerTrackingProvider)
        {
            _avatarManager = avatarManager;
            _trackingRig = trackingRig;
            _fingerTrackingProvider = fingerTrackingProvider;
        }

        public event Action inputChanged;

        public bool allowMaintainPelvisPosition => _avatarManager.currentAvatarSettings?.allowMaintainPelvisPosition ?? false;

        public void Initialize()
        {
            _trackingRig.trackingChanged += OnTrackingRigChanged;
        }

        public void Dispose()
        {
            _trackingRig.trackingChanged -= OnTrackingRigChanged;
        }

        public bool TryGetTransform(DeviceUse use, out Transform transform)
        {
            ITrackedNode node = use switch
            {
                DeviceUse.Head => _trackingRig.head,
                DeviceUse.LeftHand => _trackingRig.leftHand,
                DeviceUse.RightHand => _trackingRig.rightHand,
                DeviceUse.Waist => _trackingRig.pelvis,
                DeviceUse.LeftFoot => _trackingRig.leftFoot,
                DeviceUse.RightFoot => _trackingRig.rightFoot,
                _ => throw new InvalidOperationException($"Unexpected device use {use}"),
            };

            transform = node.offset;

            return node.isTracking && node.isCalibrated;
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl) => _fingerTrackingProvider.TryGetFingerCurl(use, out curl);

        private void OnTrackingRigChanged()
        {
            inputChanged?.Invoke();
        }
    }
}
