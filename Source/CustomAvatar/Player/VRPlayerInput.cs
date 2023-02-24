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
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
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
        private readonly Settings _settings;
        private readonly TrackingRig _trackingRig;
        private readonly IFingerTrackingProvider _fingerTrackingProvider;

        private Settings.AvatarSpecificSettings _avatarSettings;

        internal VRPlayerInput(
            PlayerAvatarManager avatarManager,
            Settings settings,
            TrackingRig trackingRig,
            IFingerTrackingProvider fingerTrackingProvider)
        {
            _avatarManager = avatarManager;
            _settings = settings;
            _trackingRig = trackingRig;
            _fingerTrackingProvider = fingerTrackingProvider;
        }

        public event Action inputChanged;

        public bool allowMaintainPelvisPosition => _avatarSettings?.allowMaintainPelvisPosition ?? false;

        public void Initialize()
        {
            _avatarManager.avatarChanged += OnAvatarChanged;
            _trackingRig.trackingChanged += OnTrackingRigChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
        }

        public void Dispose()
        {
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _trackingRig.trackingChanged -= OnTrackingRigChanged;
        }

        public bool TryGetTransform(DeviceUse use, out Transform transform)
        {
            transform = use switch
            {
                DeviceUse.Head => _trackingRig.headOffset,
                DeviceUse.LeftHand => _trackingRig.leftHandOffset,
                DeviceUse.RightHand => _trackingRig.rightHandOffset,
                DeviceUse.Waist => _trackingRig.pelvisOffset,
                DeviceUse.LeftFoot => _trackingRig.leftFootOffset,
                DeviceUse.RightFoot => _trackingRig.rightFootOffset,
                _ => throw new InvalidOperationException($"Unexpected device use {use}"),
            };

            return transform.gameObject.activeInHierarchy;
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl) => _fingerTrackingProvider.TryGetFingerCurl(use, out curl);

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            _avatarSettings = spawnedAvatar != null ? _settings.GetAvatarSettings(spawnedAvatar.prefab.fileName) : null;
        }

        private void OnTrackingRigChanged()
        {
            inputChanged?.Invoke();
        }
    }
}
