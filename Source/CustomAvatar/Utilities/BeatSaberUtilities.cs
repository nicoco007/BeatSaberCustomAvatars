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

using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Utilities
{
    internal class BeatSaberUtilities : IInitializable, IDisposable
    {
        public static readonly float kDefaultPlayerEyeHeight = MainSettingsModelSO.kDefaultPlayerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerArmSpan = MainSettingsModelSO.kDefaultPlayerHeight;

        public Vector3 roomCenter => _mainSettingsModel.roomCenter;
        public Quaternion roomRotation => Quaternion.Euler(0, _mainSettingsModel.roomRotation, 0);

        public event Action<Vector3, Quaternion> roomAdjustChanged;

        private readonly MainSettingsModelSO _mainSettingsModel;
        private readonly PlayerDataModel _playerDataModel;
        private readonly Settings _settings;
        private readonly IVRPlatformHelper _vrPlatformHelper;

        private Func<OpenVRHelper, OpenVRHelper.VRControllerManufacturerName> _vrControllerManufacturerNameGetter;

        internal BeatSaberUtilities(MainSettingsModelSO mainSettingsModel, PlayerDataModel playerDataModel, Settings settings, IVRPlatformHelper vrPlatformHelper)
        {
            _mainSettingsModel = mainSettingsModel;
            _playerDataModel = playerDataModel;
            _settings = settings;
            _vrPlatformHelper = vrPlatformHelper;
        }

        public void Initialize()
        {
            _mainSettingsModel.roomCenter.didChangeEvent += OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent += OnRoomRotationChanged;

            _vrControllerManufacturerNameGetter = ReflectionExtensions.CreatePrivatePropertyGetter<OpenVRHelper, OpenVRHelper.VRControllerManufacturerName>("vrControllerManufacturerName");
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
            float playerEyeHeight = _playerDataModel.playerData.playerSpecificSettings.playerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;

            if (_settings.moveFloorWithRoomAdjust)
            {
                playerEyeHeight -= _mainSettingsModel.roomCenter.value.y;
            }

            return playerEyeHeight;
        }

        /// <summary>
        /// Similar to the various implementations of <see cref="IVRPlatformHelper.AdjustControllerTransform(UnityEngine.XR.XRNode, Transform, Vector3, Vector3)"/> except it updates a pose instead of adjusting a transform.
        /// </summary>
        public void AdjustPlatformSpecificControllerPose(DeviceUse use, ref Pose pose)
        {
            if (use != DeviceUse.LeftHand && use != DeviceUse.RightHand) return;

            Vector3 position = _mainSettingsModel.controllerPosition;
            Vector3 rotation = _mainSettingsModel.controllerRotation;

            if (_vrPlatformHelper.vrPlatformSDK == VRPlatformSDK.Oculus)
            {
                rotation += new Vector3(-40f, 0f, 0f);
                position += new Vector3(0f, 0f, 0.055f);
            }
            else if (_vrPlatformHelper.vrPlatformSDK == VRPlatformSDK.OpenVR)
            {
                if (_vrControllerManufacturerNameGetter((OpenVRHelper)_vrPlatformHelper) == OpenVRHelper.VRControllerManufacturerName.Valve)
                {
                    rotation += new Vector3(-16.3f, 0f, 0f);
                    position += new Vector3(0f, 0.022f, -0.01f);
                }
                else
                {
                    rotation += new Vector3(-4.3f, 0f, 0f);
                    position += new Vector3(0f, -0.008f, 0f);
                }
            }

            // mirror across YZ plane for left hand
            if (use == DeviceUse.LeftHand)
            {
                rotation.y = -rotation.y;
                rotation.z = -rotation.z;

                position.x = -position.x;
            }

            pose.rotation *= Quaternion.Euler(rotation);
            pose.position += pose.rotation * position;
        }

        /// <summary>
        /// Update the local player's height based on the head's current position. Similar to <see cref="PlayerHeightSettingsController.AutoSetHeight"/>
        /// </summary>
        public void UpdatePlayerHeight()
        {
            _vrPlatformHelper.GetNodePose(UnityEngine.XR.XRNode.Head, 0, out Vector3 position, out _);

            PlayerSpecificSettings currentSettings = _playerDataModel.playerData.playerSpecificSettings;
            float height = position.y + roomCenter.y + MainSettingsModelSO.kHeadPosToPlayerHeightOffset;

            _playerDataModel.playerData.SetPlayerSpecificSettings(currentSettings.CopyWith(null, height, false));
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
