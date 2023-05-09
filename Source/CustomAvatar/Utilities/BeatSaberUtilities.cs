//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Utilities
{
    internal class BeatSaberUtilities : IInitializable, IDisposable
    {
        public static readonly float kDefaultPlayerHeight = MainSettingsModelSO.kDefaultPlayerHeight;
        public static readonly float kHeadPosToPlayerHeightOffset = MainSettingsModelSO.kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerEyeHeight = kDefaultPlayerHeight - kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerArmSpan = kDefaultPlayerHeight;

        public Vector3 roomCenter => _mainSettingsModel.roomCenter;
        public Quaternion roomRotation => Quaternion.Euler(0, _mainSettingsModel.roomRotation, 0);

        public event Action<Vector3, Quaternion> roomAdjustChanged;

        private readonly MainSettingsModelSO _mainSettingsModel;
        private readonly Settings _settings;
        private readonly IVRPlatformHelper _vrPlatformHelper;

        internal BeatSaberUtilities(MainSettingsModelSO mainSettingsModel, Settings settings, IVRPlatformHelper vrPlatformHelper)
        {
            _mainSettingsModel = mainSettingsModel;
            _settings = settings;
            _vrPlatformHelper = vrPlatformHelper;
        }

        public void Initialize()
        {
            _mainSettingsModel.roomCenter.didChangeEvent += OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent += OnRoomRotationChanged;
        }

        public void Dispose()
        {
            _mainSettingsModel.roomCenter.didChangeEvent -= OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent -= OnRoomRotationChanged;
        }

        /// <summary>
        /// Gets the current player's height, taking into account whether the floor is being moved with the room or not.
        /// </summary>
        public float GetRoomAdjustedPlayerEyeHeight()
        {
            if (_settings.moveFloorWithRoomAdjust)
            {
                return _settings.playerEyeHeight - _mainSettingsModel.roomCenter.value.y;
            }

            return _settings.playerEyeHeight;
        }

        /// <summary>
        /// Similar to the various implementations of <see cref="VRController.UpdateAnchorPosition()"/> except it updates a pose instead of adjusting a transform.
        /// </summary>
        public void AdjustPlatformSpecificControllerPose(DeviceUse use, ref Pose pose)
        {
            EulerPose controllerOffset = use switch
            {
                DeviceUse.LeftHand => _vrPlatformHelper.GetPoseOffsetForNode(XRNode.LeftHand),
                DeviceUse.RightHand => _vrPlatformHelper.GetPoseOffsetForNode(XRNode.RightHand),
                _ => EulerPose.identity,
            };

            controllerOffset += new EulerPose(_mainSettingsModel.controllerPosition, _mainSettingsModel.controllerRotation);

            if (use == DeviceUse.LeftHand)
            {
                controllerOffset = controllerOffset.MirrorOnYZPlane();
            }

            var rotation = Quaternion.Euler(controllerOffset.rotation);
            pose.position += pose.rotation * controllerOffset.position;
            pose.rotation *= rotation;
        }

        private void OnRoomCenterChanged()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }

        private void OnRoomRotationChanged()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }
    }
}
