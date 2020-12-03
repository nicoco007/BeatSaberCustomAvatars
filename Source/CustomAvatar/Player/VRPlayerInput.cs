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
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using DynamicOpenVR.IO;
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

        public bool allowMaintainPelvisPosition => _avatarSettings.allowMaintainPelvisPosition;

        public event Action inputChanged;

        private readonly ILogger<VRPlayerInput> _logger;
        private readonly DeviceManager _deviceManager;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly Settings _settings;
        private readonly CalibrationData _calibrationData;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly TrackingHelper _trackingHelper;

        private SkeletalInput _leftHandAnimAction;
        private SkeletalInput _rightHandAnimAction;

        private Settings.AvatarSpecificSettings _avatarSettings;
        private CalibrationData.FullBodyCalibration _manualCalibration;

        private Pose _previousWaistPose;
        private Pose _previousLeftFootPose;
        private Pose _previousRightFootPose;

        private bool _shouldTrackFullBody =>
            _avatarSettings != null && _manualCalibration != null &&
                (_avatarSettings.bypassCalibration ||
                !_avatarSettings.useAutomaticCalibration && _manualCalibration.isCalibrated ||
                _avatarSettings.useAutomaticCalibration && _calibrationData.automaticCalibration.isCalibrated);

        [Inject]
        internal VRPlayerInput(ILoggerProvider loggerProvider, DeviceManager trackedDeviceManager, PlayerAvatarManager avatarManager, Settings settings, CalibrationData calibrationData, BeatSaberUtilities beatSaberUtilities, TrackingHelper trackingHelper)
        {
            _logger = loggerProvider.CreateLogger<VRPlayerInput>();
            _deviceManager = trackedDeviceManager;
            _avatarManager = avatarManager;
            _settings = settings;
            _calibrationData = calibrationData;
            _beatSaberUtilities = beatSaberUtilities;
            _trackingHelper = trackingHelper;
        }

        public void Initialize()
        {
            _deviceManager.devicesChanged += OnDevicesUpdated;
            _avatarManager.avatarChanged  += OnAvatarChanged;

            _leftHandAnimAction = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnDevicesUpdated();
        }

        public void Dispose()
        {
            _deviceManager.devicesChanged -= OnDevicesUpdated;
            _avatarManager.avatarChanged  -= OnAvatarChanged;

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
                    throw new InvalidOperationException($"{nameof(TryGetFingerCurl)} only supports {nameof(DeviceUse.LeftHand)} and {nameof(DeviceUse.RightHand)}");
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
            if (!_deviceManager.TryGetDeviceState(use, out TrackedDevice device) || !device.isTracking)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(device.position, device.rotation);

            _trackingHelper.ApplyRoomAdjust(ref pose.position, ref pose.rotation);

            SpawnedAvatar spawnedAvatar = _avatarManager.currentlySpawnedAvatar;

            switch (use)
            {
                case DeviceUse.Head:
                case DeviceUse.LeftHand:
                case DeviceUse.RightHand:
                    _trackingHelper.ApplyFloorOffset(spawnedAvatar, ref pose.position);
                    break;

                default:
                    _trackingHelper.ApplyTrackerFloorOffset(spawnedAvatar, ref pose.position);
                    break;
            }

            return true;
        }

        internal bool TryGetUncalibratedPoseForAvatar(DeviceUse use, SpawnedAvatar spawnedAvatar, out Pose pose)
        {
            if (!TryGetUncalibratedPose(use, out pose))
            {
                return false;
            }

            if (spawnedAvatar)
            {
                _trackingHelper.ApplyLocalPose(ref pose.position, ref pose.rotation, spawnedAvatar.transform.parent);
            }

            return true;
        }

        internal void CalibrateFullBodyTrackingManual(SpawnedAvatar spawnedAvatar)
        {
            _logger.Info("Running manual full body tracking calibration");

            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            if (TryGetUncalibratedPoseForAvatar(DeviceUse.Waist, spawnedAvatar, out Pose waist))
            {
                Vector3 positionOffset = Quaternion.Inverse(waist.rotation) * (spawnedAvatar.pelvis.position - waist.position);
                Quaternion rotationOffset = Quaternion.Inverse(waist.rotation) * spawnedAvatar.pelvis.rotation;

                fullBodyCalibration.waist = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set waist pose correction " + fullBodyCalibration.waist);
            }

            if (TryGetUncalibratedPoseForAvatar(DeviceUse.LeftFoot, spawnedAvatar, out Pose leftFoot))
            {
                Vector3 positionOffset = Quaternion.Inverse(leftFoot.rotation) * (spawnedAvatar.leftLeg.position - leftFoot.position);
                Quaternion rotationOffset = Quaternion.Inverse(leftFoot.rotation) * spawnedAvatar.leftLeg.rotation;

                fullBodyCalibration.leftFoot = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (TryGetUncalibratedPoseForAvatar(DeviceUse.RightFoot, spawnedAvatar, out Pose rightFoot))
            {
                Vector3 positionOffset = Quaternion.Inverse(rightFoot.rotation) * (spawnedAvatar.rightLeg.position - rightFoot.position);
                Quaternion rotationOffset = Quaternion.Inverse(rightFoot.rotation) * spawnedAvatar.rightLeg.rotation;

                fullBodyCalibration.rightFoot = new Pose(positionOffset, rotationOffset);
                _logger.Info("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }

            inputChanged?.Invoke();
        }

        internal void CalibrateFullBodyTrackingAuto()
        {
            _logger.Info("Running automatic full body tracking calibration");

            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            Vector3 floorNormal = Vector3.up;

            if (TryGetUncalibratedPose(DeviceUse.LeftFoot, out Pose leftFoot))
            {
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                fullBodyCalibration.leftFoot = new Pose(leftFoot.position.y * Vector3.back, leftRotationCorrection);
                _logger.Info("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose rightFoot))
            {
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                fullBodyCalibration.rightFoot = new Pose(rightFoot.position.y * Vector3.back, rightRotationCorrection);
                _logger.Info("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }

            if (TryGetUncalibratedPose(DeviceUse.Head, out Pose head) && TryGetUncalibratedPose(DeviceUse.Waist, out Pose waist))
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = head.position.y;

                Vector3 wantedWaistPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
                Vector3 waistPositionCorrection = wantedWaistPosition - Vector3.up * waist.position.y;

                Vector3 waistForward = waist.rotation * Vector3.forward;
                Vector3 waistStraightForward = Vector3.ProjectOnPlane(waistForward, floorNormal);
                Quaternion waistRotationCorrection = Quaternion.Inverse(waist.rotation) * Quaternion.LookRotation(waistStraightForward, Vector3.up);

                fullBodyCalibration.waist = new Pose(waistPositionCorrection, waistRotationCorrection);
                _logger.Info("Set waist pose correction " + fullBodyCalibration.waist);

                _beatSaberUtilities.SetPlayerHeight(eyeHeight + MainSettingsModelSO.kHeadPosToPlayerHeightOffset);
            }

            inputChanged?.Invoke();
        }

        internal void ClearManualFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            fullBodyCalibration.leftFoot = Pose.identity;
            fullBodyCalibration.rightFoot = Pose.identity;
            fullBodyCalibration.waist = Pose.identity;

            inputChanged?.Invoke();
        }

        internal void ClearAutomaticFullBodyTrackingData()
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            fullBodyCalibration.leftFoot = Pose.identity;
            fullBodyCalibration.rightFoot = Pose.identity;
            fullBodyCalibration.waist = Pose.identity;

            inputChanged?.Invoke();
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            if (_avatarSettings != null)
            {
                _avatarSettings.useAutomaticCalibrationChanged -= OnUseAutomaticCalibrationChanged;
                _avatarSettings.bypassCalibrationChanged       -= OnBypassCalibrationChanged;
            }

            if (!spawnedAvatar)
            {
                _avatarSettings = null;
                _manualCalibration = null;

                return;
            }
           
            _avatarSettings = _settings.GetAvatarSettings(spawnedAvatar.avatar.fileName);
            _manualCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.avatar.fileName);

            _avatarSettings.useAutomaticCalibrationChanged += OnUseAutomaticCalibrationChanged;
            _avatarSettings.bypassCalibrationChanged       += OnBypassCalibrationChanged;
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
            Pose correction = Pose.identity;

            if (_avatarSettings?.useAutomaticCalibration == true)
            {
                correction = _calibrationData.automaticCalibration.waist;

                Quaternion rotationOffset = Quaternion.Euler(0, (int)_settings.automaticCalibration.waistTrackerPosition, 0);

                correction.position -= (_calibrationData.automaticCalibration.waist.rotation * Vector3.forward) * _settings.automaticCalibration.pelvisOffset;
                correction.rotation *= rotationOffset;
            }
            else if (_manualCalibration != null)
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
            Pose correction = Pose.identity;

            if (_avatarSettings?.useAutomaticCalibration == true)
            {
                correction = _calibrationData.automaticCalibration.leftFoot;
                correction.position -= Vector3.up * _settings.automaticCalibration.legOffset;
            }
            else if (_manualCalibration != null)
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
            Pose correction = Pose.identity;

            if (_avatarSettings?.useAutomaticCalibration == true)
            {
                correction = _calibrationData.automaticCalibration.rightFoot;
                correction.position -= Vector3.up * _settings.automaticCalibration.legOffset;
            }
            else if (_manualCalibration != null)
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
            if (!_shouldTrackFullBody || !_deviceManager.TryGetDeviceState(use, out TrackedDevice device) || !device.isTracking)
            {
                pose = Pose.identity;
                return false;
            }

            Vector3 position    = device.position + device.rotation * correction.position; // correction is forward-facing by definition
            Quaternion rotation = device.rotation * correction.rotation;

            _trackingHelper.ApplyRoomAdjust(ref position, ref rotation);
            _trackingHelper.ApplyTrackerFloorOffset(_avatarManager.currentlySpawnedAvatar, ref position);

            pose = new Pose(Vector3.Lerp(previousPose.position, position, smoothing.position), Quaternion.Slerp(previousPose.rotation, rotation, smoothing.rotation));

            return true;
        }

        private void OnDevicesUpdated()
        {
            inputChanged?.Invoke();
        }

        private void OnUseAutomaticCalibrationChanged(bool enabled)
        {
            inputChanged?.Invoke();
        }

        private void OnBypassCalibrationChanged(bool enabled)
        {
            inputChanged?.Invoke();
        }
    }
}
