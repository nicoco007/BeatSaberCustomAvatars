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

        private readonly VRPlayerInputInternal _internalPlayerInput;
        private readonly TrackingHelper _trackingHelper;

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
