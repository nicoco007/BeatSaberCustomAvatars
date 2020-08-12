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

using System;
using System.Collections;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar
{
    internal class AvatarTailor
    {
        public const float kDefaultPlayerArmSpan = 1.7f;
        
        private readonly ILogger<AvatarTailor> _logger;
        private readonly MainSettingsModelSO _mainSettingsModel;
        private readonly PlayerDataModel _playerDataModel;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;
        private readonly TrackedDeviceManager _trackedDeviceManager;

        private Vector3? _initialPlatformPosition;
        
        private Vector3 _roomCenter => _mainSettingsModel.roomCenter.value;
        private float _playerEyeHeight => _playerDataModel.playerData.playerSpecificSettings.playerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;

        [Inject]
        private AvatarTailor(ILoggerProvider loggerProvider, MainSettingsModelSO mainSettingsModel, PlayerDataModel playerDataModel, Settings settings, CalibrationData calibrationData, TrackedDeviceManager trackedDeviceManager)
        {
            _logger = loggerProvider.CreateLogger<AvatarTailor>();
            _mainSettingsModel = mainSettingsModel;
            _playerDataModel = playerDataModel;
            _settings = settings;
            _calibrationData = calibrationData;
            _trackedDeviceManager = trackedDeviceManager;
        }

        public void ResizeAvatar(SpawnedAvatar spawnedAvatar)
        {
            if (!spawnedAvatar.avatar.descriptor.allowHeightCalibration || !spawnedAvatar.avatar.isIKAvatar) return;

            // compute scale
            float scale;
            AvatarResizeMode resizeMode = _settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = spawnedAvatar.avatar.armSpan;

                    if (avatarArmLength > 0)
                    {
                        scale = _settings.playerArmSpan / avatarArmLength;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                case AvatarResizeMode.Height:
                    float avatarEyeHeight = spawnedAvatar.avatar.eyeHeight;

                    if (avatarEyeHeight > 0)
                    {
                        scale = _playerEyeHeight / avatarEyeHeight;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                default:
                    scale = 1.0f;
                    break;
            }

            if (scale <= 0)
            {
                _logger.Warning("Calculated scale is <= 0; reverting to 1");
                scale = 1.0f;
            }

            // apply scale
            spawnedAvatar.scale = scale;

            SharedCoroutineStarter.instance.StartCoroutine(FloorMendingWithDelay(spawnedAvatar));
        }

        private IEnumerator FloorMendingWithDelay(SpawnedAvatar spawnedAvatar)
        {
            yield return new WaitForEndOfFrame(); // wait for CustomFloorPlugin:PlatformManager:Start to hide original platform
            
            if (!spawnedAvatar) yield break;

            float floorOffset = 0f;

            if (_settings.enableFloorAdjust && spawnedAvatar.avatar.isIKAvatar)
            {
                float playerEyeHeight = _playerEyeHeight;
                float avatarEyeHeight = spawnedAvatar.avatar.eyeHeight;

                floorOffset = playerEyeHeight - avatarEyeHeight * spawnedAvatar.scale;

                if (_settings.moveFloorWithRoomAdjust)
                {
                    floorOffset += _roomCenter.y;
                }
            }

            floorOffset = (float) Math.Round(floorOffset, 3); // round to millimeter

            // apply offset
			spawnedAvatar.verticalPosition = floorOffset;
            
            // ReSharper disable Unity.PerformanceCriticalCodeInvocation
            GameObject menuPlayersPlace = GameObject.Find("MenuPlayersPlace");
            GameObject originalFloor = GameObject.Find("Environment/PlayersPlace");
            GameObject customFloor = GameObject.Find("Platform Loader");
            // ReSharper disable restore Unity.PerformanceCriticalCodeInvocation

            if (menuPlayersPlace)
            {
                _logger.Info($"Moving MenuPlayersPlace floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                menuPlayersPlace.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (originalFloor)
            {
                _logger.Info($"Moving PlayersPlace {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                originalFloor.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (customFloor)
            {
                _logger.Info($"Moving Custom Platforms floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");

                _initialPlatformPosition = _initialPlatformPosition ?? customFloor.transform.position;
                customFloor.transform.position = (Vector3.up * floorOffset) + _initialPlatformPosition ?? Vector3.zero;
            }
        }
        
        public void CalibrateFullBodyTrackingManual(SpawnedAvatar spawnedAvatar)
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            if (_trackedDeviceManager.waist.tracked)
            {
                TrackedDeviceState pelvis = _trackedDeviceManager.waist;

                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.pelvis.rotation) * (spawnedAvatar.pelvis.position - ApplyTrackedPointFloorOffset(spawnedAvatar, pelvis.position));
                Quaternion rotationOffset = Quaternion.Inverse(pelvis.rotation) * spawnedAvatar.pelvis.rotation;

                fullBodyCalibration.waist = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set waist pose correction " + fullBodyCalibration.waist);
            }

            if (_trackedDeviceManager.leftFoot.tracked)
            {
                TrackedDeviceState leftFoot = _trackedDeviceManager.leftFoot;

                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.leftLeg.rotation) * (spawnedAvatar.leftLeg.position - ApplyTrackedPointFloorOffset(spawnedAvatar, leftFoot.position));
                Quaternion rotationOffset = Quaternion.Inverse(leftFoot.rotation) * spawnedAvatar.leftLeg.rotation;

                fullBodyCalibration.leftFoot = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (_trackedDeviceManager.rightFoot.tracked)
            {
                TrackedDeviceState rightFoot = _trackedDeviceManager.rightFoot;

                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.rightLeg.rotation) * (spawnedAvatar.rightLeg.position - ApplyTrackedPointFloorOffset(spawnedAvatar, rightFoot.position));
                Quaternion rotationOffset = Quaternion.Inverse(rightFoot.rotation) * spawnedAvatar.rightLeg.rotation;

                fullBodyCalibration.rightFoot = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }
        }

        public void CalibrateFullBodyTrackingAuto()
        {
            _logger.Info("Calibrating full body tracking");

            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            Vector3 floorNormal = Vector3.up;
            float floorPosition = _settings.moveFloorWithRoomAdjust ? _roomCenter.y : 0;

            if (_trackedDeviceManager.leftFoot.tracked)
            {
                TrackedDeviceState leftFoot = _trackedDeviceManager.leftFoot;

                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                fullBodyCalibration.leftFoot = new Pose((leftFoot.position.y - floorPosition) * Vector3.back, leftRotationCorrection);
                _logger.Info("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (_trackedDeviceManager.rightFoot.tracked)
            {
                TrackedDeviceState rightFoot = _trackedDeviceManager.rightFoot;

                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                fullBodyCalibration.rightFoot = new Pose((rightFoot.position.y - floorPosition) * Vector3.back, rightRotationCorrection);
                _logger.Info("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }

            if (_trackedDeviceManager.head.tracked && _trackedDeviceManager.waist.tracked)
            {
                TrackedDeviceState head = _trackedDeviceManager.head;
                TrackedDeviceState pelvis = _trackedDeviceManager.waist;

                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = head.position.y - floorPosition;

                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * (pelvis.position.y - floorPosition);

                Vector3 pelvisForward = pelvis.rotation * Vector3.forward;
                Vector3 pelvisStraightForward = Vector3.ProjectOnPlane(pelvisForward, floorNormal);
                Quaternion pelvisRotationCorrection = Quaternion.Inverse(pelvis.rotation) * Quaternion.LookRotation(pelvisStraightForward, Vector3.up);

                fullBodyCalibration.waist = new Pose(pelvisPositionCorrection, pelvisRotationCorrection);
                _logger.Info("Set waist pose correction " + fullBodyCalibration.waist);
            }
        }

        public void ClearManualFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            fullBodyCalibration.leftFoot = Pose.identity;
            fullBodyCalibration.rightFoot = Pose.identity;
            fullBodyCalibration.waist = Pose.identity;
        }

        public void ClearAutomaticFullBodyTrackingData()
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            fullBodyCalibration.leftFoot = Pose.identity;
            fullBodyCalibration.rightFoot = Pose.identity;
            fullBodyCalibration.waist = Pose.identity;
        }

        public Vector3 ApplyTrackedPointFloorOffset(SpawnedAvatar spawnedAvatar, Vector3 position)
        {
            if (!_settings.enableFloorAdjust) return position;

            float scaledEyeHeight = spawnedAvatar.avatar.eyeHeight * spawnedAvatar.scale;
            float yOffset = spawnedAvatar.verticalPosition;

            if (_settings.moveFloorWithRoomAdjust)
            {
                yOffset -= _mainSettingsModel.roomCenter.value.y;
            }

            float yOffsetScale = scaledEyeHeight / (scaledEyeHeight + yOffset);

            return new Vector3(position.x, position.y * yOffsetScale + yOffset, position.z);
        }
    }
}
