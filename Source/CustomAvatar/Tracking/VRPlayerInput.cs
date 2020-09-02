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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using DynamicOpenVR.IO;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Tracking
{
    /// <summary>
    /// The player's <see cref="IAvatarInput"/> with calibration and other settings applied.
    /// </summary>
    public class VRPlayerInput : IDisposable, IAvatarInput
    {
        public static readonly float kDefaultPlayerArmSpan = 1.8f;

        public bool allowMaintainPelvisPosition => _avatarSettings.allowMaintainPelvisPosition;

        public event Action inputChanged;

        private readonly ILogger<VRPlayerInput> _logger;
        private readonly ITrackedDeviceManager _deviceManager;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;
        private readonly BeatSaberUtilities _beatSaberUtilities;

        private readonly SkeletalInput _leftHandAnimAction;
        private readonly SkeletalInput _rightHandAnimAction;

        private Settings.AvatarSpecificSettings _avatarSettings;
        private CalibrationData.FullBodyCalibration _manualCalibration;

        private Pose _previousWaistPose;
        private Pose _previousLeftFootPose;
        private Pose _previousRightFootPose;

        private bool _shouldTrackFullBody =>
            _avatarSettings.bypassCalibration ||
            !_avatarSettings.useAutomaticCalibration && _manualCalibration.isCalibrated ||
            _avatarSettings.useAutomaticCalibration && _calibrationData.automaticCalibration.isCalibrated;

        [Inject]
        internal VRPlayerInput(ILoggerProvider loggerProvider, ITrackedDeviceManager trackedDeviceManager, PlayerAvatarManager avatarManager, Settings settings, CalibrationData calibrationData, BeatSaberUtilities beatSaberUtilities)
        {
            _logger = loggerProvider.CreateLogger<VRPlayerInput>();
            _deviceManager = trackedDeviceManager;
            _avatarManager = avatarManager;
            _settings = settings;
            _calibrationData = calibrationData;
            _beatSaberUtilities = beatSaberUtilities;

            _deviceManager.deviceAdded += OnDevicesUpdated;
            _deviceManager.deviceRemoved += OnDevicesUpdated;
            _deviceManager.deviceTrackingAcquired += OnDevicesUpdated;
            _deviceManager.deviceTrackingLost += OnDevicesUpdated;
            
            _leftHandAnimAction  = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");

            _avatarManager.avatarChanged += OnAvatarChanged;
        }

        public void Dispose()
        {
            _deviceManager.deviceAdded -= OnDevicesUpdated;
            _deviceManager.deviceRemoved -= OnDevicesUpdated;
            _deviceManager.deviceTrackingAcquired -= OnDevicesUpdated;
            _deviceManager.deviceTrackingLost -= OnDevicesUpdated;

            _leftHandAnimAction.Dispose();
            _rightHandAnimAction.Dispose();
        }

        public bool TryGetPose(DeviceUse use, out Pose pose)
        {
            switch (use)
            {
                case DeviceUse.Head:
                    return TryGetUncalibratedPose(use, out pose);

                case DeviceUse.LeftHand:
                case DeviceUse.RightHand:
                    return TryGetCalibratedHandPose(use, out pose);

                case DeviceUse.Waist:
                    return TryGetCalibratedWaistPose(out pose);

                case DeviceUse.LeftFoot:
                    return TryGetCalibratedLeftFootPose(out pose);

                case DeviceUse.RightFoot:
                    return TryGetCalibratedRightFootPose(out pose);

                default:
                    pose = Pose.identity;
                    return false;
            }
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl)
        {
            SkeletalInput handAnim;

            switch (use)
            {
                case DeviceUse.LeftHand:
                    handAnim = _leftHandAnimAction;
                    break;

                case DeviceUse.RightHand:
                    handAnim = _rightHandAnimAction;
                    break;

                default:
                    curl = null;
                    return false;
            }

            if (!handAnim.isActive || handAnim.summaryData == null)
            {
                curl = null;
                return false;
            }

            curl = new FingerCurl(handAnim.summaryData.thumbCurl, handAnim.summaryData.indexCurl, handAnim.summaryData.middleCurl, handAnim.summaryData.ringCurl, handAnim.summaryData.littleCurl);
            return true;
        }

        internal bool TryGetUncalibratedPose(DeviceUse use, out Pose pose)
        {
            if (!_deviceManager.TryGetDeviceState(use, out ITrackedDeviceState device) || !device.isConnected || !device.isTracking)
            {
                pose = Pose.identity;
                return false;
            }

            Vector3 roomCenter = _beatSaberUtilities.roomCenter;
            Quaternion roomRotation = _beatSaberUtilities.roomRotation;

            pose = new Pose(device.position + roomRotation * roomCenter, device.rotation * roomRotation);

            if (_settings.moveFloorWithRoomAdjust)
            {
                pose.position.y -= roomCenter.y;
            }

            SpawnedAvatar spawnedAvatar = _avatarManager.currentlySpawnedAvatar;

            switch (use)
            {
                case DeviceUse.Head:
                case DeviceUse.LeftHand:
                case DeviceUse.RightHand:
                    ApplyFloorOffset(spawnedAvatar, ref pose.position);
                    break;

                default:
                    ApplyTrackerFloorOffset(spawnedAvatar, ref pose.position);
                    break;
            }

            return true;
        }

        internal void CalibrateFullBodyTrackingManual(SpawnedAvatar spawnedAvatar)
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            if (TryGetUncalibratedPose(DeviceUse.Waist, out Pose waist))
            {
                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.pelvis.rotation) * spawnedAvatar.pelvis.position;
                Quaternion rotationOffset = Quaternion.Inverse(waist.rotation) * spawnedAvatar.pelvis.rotation;

                fullBodyCalibration.waist = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set waist pose correction " + fullBodyCalibration.waist);
            }

            if (TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose leftFoot))
            {
                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.leftLeg.rotation) * spawnedAvatar.leftLeg.position;
                Quaternion rotationOffset = Quaternion.Inverse(leftFoot.rotation) * spawnedAvatar.leftLeg.rotation;

                fullBodyCalibration.leftFoot = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose rightFoot))
            {
                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.rightLeg.rotation) * spawnedAvatar.rightLeg.position;
                Quaternion rotationOffset = Quaternion.Inverse(rightFoot.rotation) * spawnedAvatar.rightLeg.rotation;

                fullBodyCalibration.rightFoot = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }
        }

        internal void CalibrateFullBodyTrackingAuto()
        {
            _logger.Info("Calibrating full body tracking");

            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            Vector3 floorNormal = Vector3.up;
            float floorPosition = _settings.moveFloorWithRoomAdjust ? _beatSaberUtilities.roomCenter.y : 0;

            if (TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose leftFoot))
            {
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                fullBodyCalibration.leftFoot = new Pose((leftFoot.position.y - floorPosition) * Vector3.back, leftRotationCorrection);
                _logger.Info("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose rightFoot))
            {
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                fullBodyCalibration.rightFoot = new Pose((rightFoot.position.y - floorPosition) * Vector3.back, rightRotationCorrection);
                _logger.Info("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }

            if (TryGetUncalibratedPose(DeviceUse.Head, out Pose head) && TryGetUncalibratedPose(DeviceUse.Waist, out Pose waist))
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = head.position.y - floorPosition;

                Vector3 wantedWaistPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
                Vector3 waistPositionCorrection = wantedWaistPosition - Vector3.up * (waist.position.y - floorPosition);

                Vector3 waistForward = waist.rotation * Vector3.forward;
                Vector3 waistStraightForward = Vector3.ProjectOnPlane(waistForward, floorNormal);
                Quaternion waistRotationCorrection = Quaternion.Inverse(waist.rotation) * Quaternion.LookRotation(waistStraightForward, Vector3.up);

                fullBodyCalibration.waist = new Pose(waistPositionCorrection, waistRotationCorrection);
                _logger.Info("Set waist pose correction " + fullBodyCalibration.waist);
            }
        }

        internal void ClearManualFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            fullBodyCalibration.leftFoot = Pose.identity;
            fullBodyCalibration.rightFoot = Pose.identity;
            fullBodyCalibration.waist = Pose.identity;
        }

        internal void ClearAutomaticFullBodyTrackingData()
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            fullBodyCalibration.leftFoot = Pose.identity;
            fullBodyCalibration.rightFoot = Pose.identity;
            fullBodyCalibration.waist = Pose.identity;
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            if (!spawnedAvatar)
            {
                _avatarSettings = null;
                _manualCalibration = null;

                return;
            }
           
            _avatarSettings = _settings.GetAvatarSettings(spawnedAvatar.avatar.fileName);
            _manualCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);
        }

        private bool TryGetCalibratedHandPose(DeviceUse use, out Pose pose)
        {
            if (!TryGetUncalibratedPose(use, out pose))
            {
                pose = Pose.identity;
                return false;
            }

            _beatSaberUtilities.AdjustPlatformSpecificControllerPose(use, ref pose);

            return true;
        }

        private bool TryGetCalibratedWaistPose(out Pose pose)
        {
            Pose correction;

            if (_avatarSettings.useAutomaticCalibration)
            {
                correction = _calibrationData.automaticCalibration.waist;

                Quaternion rotationOffset = Quaternion.Euler(0, (int)_settings.automaticCalibration.waistTrackerPosition, 0);

                correction.position -= Quaternion.Inverse(rotationOffset) * (Vector3.forward * _settings.automaticCalibration.pelvisOffset);
                correction.rotation *= rotationOffset;
            }
            else
            {
                correction = _manualCalibration.waist;
            }

            if (!TryGetTrackerPose(DeviceUse.Waist, _previousWaistPose, correction, _settings.fullBodyMotionSmoothing.waist, out pose))
            {
                return false;
            }

            _previousWaistPose = pose;
            return true;
        }

        private bool TryGetCalibratedLeftFootPose(out Pose pose)
        {
            Pose correction;

            if (_avatarSettings.useAutomaticCalibration)
            {
                correction = _calibrationData.automaticCalibration.leftFoot;
                correction.position -= Vector3.up * _settings.automaticCalibration.legOffset;
            }
            else
            {
                correction = _manualCalibration.leftFoot;
            }

            if (!TryGetTrackerPose(DeviceUse.LeftFoot, _previousLeftFootPose, correction, _settings.fullBodyMotionSmoothing.feet, out pose))
            {
                return false;
            }

            _previousLeftFootPose = pose;
            return true;
        }

        private bool TryGetCalibratedRightFootPose(out Pose pose)
        {
            Pose correction;

            if (_avatarSettings.useAutomaticCalibration)
            {
                correction = _calibrationData.automaticCalibration.rightFoot;
                correction.position -= Vector3.up * _settings.automaticCalibration.legOffset;
            }
            else
            {
                correction = _manualCalibration.rightFoot;
            }

            if (!TryGetTrackerPose(DeviceUse.RightFoot, _previousRightFootPose, correction, _settings.fullBodyMotionSmoothing.feet, out pose))
            {
                return false;
            }

            _previousRightFootPose = pose;
            return true;
        }

        private bool TryGetTrackerPose(DeviceUse use, Pose previousPose, Pose correction, Settings.TrackedPointSmoothing smoothing, out Pose pose)
        {
            if (!_shouldTrackFullBody || !TryGetUncalibratedPose(use, out Pose currentPose))
            {
                pose = Pose.identity;
                return false;
            }

            Quaternion correctedRotation = currentPose.rotation * correction.rotation;
            Vector3 correctedPosition = currentPose.position + correctedRotation * correction.position; // correction is forward-facing by definition

            pose = new Pose(Vector3.Lerp(previousPose.position, correctedPosition, smoothing.position), Quaternion.Slerp(previousPose.rotation, correctedRotation, smoothing.rotation));
            return true;
        }

        private void OnDevicesUpdated(ITrackedDeviceState state)
        {
            inputChanged?.Invoke();
        }

        /// <summary>
        /// Move tracked point upwards based on the difference between avatar height and player height.
        /// This essentially moves the trackers up as if the player was on stilts.
        /// </summary>
        private void ApplyFloorOffset(SpawnedAvatar spawnedAvatar, ref Vector3 position)
        {
            if (!_settings.enableFloorAdjust) return;

            position.y += spawnedAvatar.scaledEyeHeight - _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight();
        }

        /// <summary>
        /// Scales the vertical movement of a tracked point based on the quotient of avatar height and player height.
        /// This moves the trackers as if the player was the height of the avatar, but only vertically (no horizontal scaling).
        /// </summary>
        private void ApplyTrackerFloorOffset(SpawnedAvatar spawnedAvatar, ref Vector3 position)
        {
            if (!_settings.enableFloorAdjust) return;

            position.y *= spawnedAvatar.scaledEyeHeight / _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight();
        }
    }
}
