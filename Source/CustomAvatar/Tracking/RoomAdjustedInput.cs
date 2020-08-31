//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Utilities;
using System;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class RoomAdjustedInput : IAvatarInput
    {
        private IAvatarInput _input;
        private BeatSaberUtilities _beatSaberUtilities;
        private FloorController _floorController;

        public RoomAdjustedInput(IAvatarInput input, BeatSaberUtilities beatSaberUtilities, FloorController floorController)
        {
            _input = input;
            _beatSaberUtilities = beatSaberUtilities;
            _floorController = floorController;

            inputChanged += OnInputChanged;
        }

        public bool allowMaintainPelvisPosition => _input.allowMaintainPelvisPosition;

        public event Action inputChanged;

        public bool TryGetPose(DeviceUse use, out Pose pose)
        {
            if (!_input.TryGetPose(use, out pose))
            {
                pose = Pose.identity;
                return false;
            }

            Vector3 origin = _beatSaberUtilities.roomCenter;
            Quaternion originRotation = _beatSaberUtilities.roomRotation;

            pose.position = origin + originRotation * pose.position;
            pose.rotation = originRotation * pose.rotation;

            pose.position.y += _floorController.floorOffset;

            return true;
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl)
        {
            return _input.TryGetFingerCurl(use, out curl);
        }

        private void OnInputChanged()
        {
            inputChanged?.Invoke();
        }
    }
}
