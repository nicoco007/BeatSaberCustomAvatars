//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
    internal class VRPlayerInputInternal : IInitializable, IDisposable, IAvatarInput
    {
        public bool allowMaintainPelvisPosition => _avatarSettings.allowMaintainPelvisPosition;

        public bool isCalibrationModeEnabled
        {
            get => _isCalibrationModeEnabled;
            set
            {
                _isCalibrationModeEnabled = value;
                inputChanged?.Invoke();
            }
        }

        public event Action inputChanged;

        private readonly ILogger<VRPlayerInputInternal> _logger;
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

        private bool _isCalibrationModeEnabled;

        private Pose _previousWaistPose;
        private Pose _previousLeftFootPose;
        private Pose _previousRightFootPose;

        private bool _shouldTrackFullBody =>
            _avatarSettings != null &&
                (_avatarSettings.bypassCalibration ||
                !_avatarSettings.useAutomaticCalibration && _manualCalibration?.isCalibrated == true ||
                _avatarSettings.useAutomaticCalibration && _calibrationData.automaticCalibration.isCalibrated);

        internal VRPlayerInputInternal(ILogger<VRPlayerInputInternal> logger, DeviceManager trackedDeviceManager, PlayerAvatarManager avatarManager, Settings settings, CalibrationData calibrationData, BeatSaberUtilities beatSaberUtilities, TrackingHelper trackingHelper)
        {
            _logger = logger;
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
            _avatarManager.avatarChanged += OnAvatarChanged;

            _leftHandAnimAction = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnDevicesUpdated();
        }

        public void Dispose()
        {
            _deviceManager.devicesChanged -= OnDevicesUpdated;
            _avatarManager.avatarChanged -= OnAvatarChanged;

            _leftHandAnimAction.Dispose();
            _rightHandAnimAction.Dispose();
        }

        public bool TryGetPose(DeviceUse use, out Pose pose)
        {
            if (isCalibrationModeEnabled && (use == DeviceUse.Waist || use == DeviceUse.LeftFoot || use == DeviceUse.RightFoot))
            {
                pose = Pose.identity;
                return _deviceManager.TryGetDeviceState(use, out _) && GetPoseForAvatarTransform(use, out pose);
            }

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

        internal bool TryGetRawPose(DeviceUse use, out Pose pose)
        {
            if (!_deviceManager.TryGetDeviceState(use, out TrackedDevice device) || !device.isTracking)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(device.position, device.rotation);
            return true;
        }

        internal bool TryGetUncalibratedPose(DeviceUse use, out Pose pose)
        {
            if (!TryGetRawPose(use, out pose))
            {
                return false;
            }

            SpawnedAvatar spawnedAvatar = _avatarManager.currentlySpawnedAvatar;

            switch (use)
            {
                case DeviceUse.Head:
                case DeviceUse.LeftHand:
                case DeviceUse.RightHand:
                    _trackingHelper.ApplyFloorOffset(spawnedAvatar, ref pose.position);
                    break;

                default:
                    _trackingHelper.ApplyFloorScaling(spawnedAvatar, ref pose.position);
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
            _logger.LogInformation("Applying manual full body tracking calibration");

            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.prefab.fileName);

            SetCalibrationFromTarget(ref fullBodyCalibration.waist, DeviceUse.Waist, spawnedAvatar, spawnedAvatar.pelvis);
            SetCalibrationFromTarget(ref fullBodyCalibration.leftFoot, DeviceUse.LeftFoot, spawnedAvatar, spawnedAvatar.leftLeg);
            SetCalibrationFromTarget(ref fullBodyCalibration.rightFoot, DeviceUse.RightFoot, spawnedAvatar, spawnedAvatar.rightLeg);

            inputChanged?.Invoke();
        }

        internal void CalibrateFullBodyTrackingAuto()
        {
            _logger.LogInformation("Applying automatic full body tracking calibration");

            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.automaticCalibration;

            Vector3 floorNormal = Vector3.up;

            if (TryGetRawPose(DeviceUse.LeftFoot, out Pose leftFoot))
            {
                Vector3 leftFootPositionCorrection = leftFoot.position.y * Vector3.back;
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                var leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation

                fullBodyCalibration.leftFoot = new Pose(leftFootPositionCorrection, leftRotationCorrection);
                _logger.LogTrace("Set left foot pose correction " + fullBodyCalibration.leftFoot);
            }

            if (TryGetRawPose(DeviceUse.RightFoot, out Pose rightFoot))
            {
                Vector3 rightFootPositionCorrection = rightFoot.position.y * Vector3.back;
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                var rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);

                fullBodyCalibration.rightFoot = new Pose(rightFootPositionCorrection, rightRotationCorrection);
                _logger.LogTrace("Set right foot pose correction " + fullBodyCalibration.rightFoot);
            }

            if (TryGetRawPose(DeviceUse.Head, out Pose head) && TryGetRawPose(DeviceUse.Waist, out Pose waist))
            {
                // using "ideal" 8 head high body proportions w/ eyes at 1/2 head height for simplicity
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = _beatSaberUtilities.playerEyeHeight;

                // since hips are at 4 2/3, we multiply by 3 to have some nice numbers
                // hips @ 4 2/3 * 3 = 14
                // eyes @ 7 1/2 * 3 = 22.5
                var wantedWaistPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
                Vector3 waistPositionCorrection = wantedWaistPosition - Vector3.up * waist.position.y;

                Vector3 waistForward = waist.rotation * Vector3.forward;
                var waistStraightForward = Vector3.ProjectOnPlane(waistForward, floorNormal);
                Quaternion waistRotationCorrection = Quaternion.Inverse(waist.rotation) * Quaternion.LookRotation(waistStraightForward, Vector3.up);

                fullBodyCalibration.waist = new Pose(waistPositionCorrection, waistRotationCorrection);
                _logger.LogTrace("Set waist pose correction " + fullBodyCalibration.waist);
            }

            inputChanged?.Invoke();
        }

        internal void ClearManualFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            CalibrationData.FullBodyCalibration fullBodyCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.prefab.fileName);

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

        private void SetCalibrationFromTarget(ref Pose calibration, DeviceUse use, SpawnedAvatar spawnedAvatar, Transform target)
        {
            if (!TryGetRawPose(use, out Pose trackerPose)) return;

            Pose targetPose = GetUnscaledAvatarTargetPose(spawnedAvatar, target);

            Vector3 positionOffset = Quaternion.Inverse(trackerPose.rotation) * (targetPose.position - trackerPose.position);
            Quaternion rotationOffset = Quaternion.Inverse(trackerPose.rotation) * targetPose.rotation;

            calibration = new Pose(positionOffset, rotationOffset);

            _logger.LogTrace($"Set {use} pose correction: " + calibration);
        }

        private Pose GetUnscaledAvatarTargetPose(SpawnedAvatar spawnedAvatar, Transform target)
        {
            Vector3 targetPosition = target.position;
            Quaternion targetRotation = target.rotation;
            Transform parent = spawnedAvatar.transform.parent;

            if (parent)
            {
                targetPosition = parent.InverseTransformPoint(target.position);
                targetRotation = Quaternion.Inverse(parent.rotation) * target.rotation;
            }

            _trackingHelper.ApplyInverseFloorScaling(spawnedAvatar, ref targetPosition);

            return new Pose(targetPosition, targetRotation);
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            if (_avatarSettings != null)
            {
                _avatarSettings.useAutomaticCalibration.changed -= OnUseAutomaticCalibrationChanged;
                _avatarSettings.bypassCalibration.changed -= OnBypassCalibrationChanged;
            }

            if (!spawnedAvatar)
            {
                _avatarSettings = null;
                _manualCalibration = null;

                return;
            }

            _avatarSettings = _settings.GetAvatarSettings(spawnedAvatar.prefab.fileName);
            _manualCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar.prefab.fileName);

            _avatarSettings.useAutomaticCalibration.changed += OnUseAutomaticCalibrationChanged;
            _avatarSettings.bypassCalibration.changed += OnBypassCalibrationChanged;
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

            if (_avatarSettings?.useAutomaticCalibration)
            {
                correction = _calibrationData.automaticCalibration.waist;

                var rotationOffset = Quaternion.Euler(0, (int)_settings.automaticCalibration.waistTrackerPosition, 0);

                correction.position -= _calibrationData.automaticCalibration.waist.rotation * (Vector3.forward * _settings.automaticCalibration.pelvisOffset);
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

            if (_avatarSettings?.useAutomaticCalibration)
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

            if (_avatarSettings?.useAutomaticCalibration)
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

            Vector3 position = device.position + device.rotation * correction.position; // correction is forward-facing w/o scaling by definition
            Quaternion rotation = device.rotation * correction.rotation;

            _trackingHelper.ApplyFloorScaling(_avatarManager.currentlySpawnedAvatar, ref position);

            pose = new Pose(Vector3.Lerp(previousPose.position, position, smoothing.position), Quaternion.Slerp(previousPose.rotation, rotation, smoothing.rotation));

            return true;
        }

        private bool GetPoseForAvatarTransform(DeviceUse use, out Pose pose)
        {
            pose = Pose.identity;

            SpawnedAvatar avatar = _avatarManager.currentlySpawnedAvatar;

            if (!avatar) return false;

            AvatarPrefab prefab = avatar.prefab;
            Transform transform = null;

            switch (use)
            {
                case DeviceUse.Head:
                    transform = prefab.head;
                    break;

                case DeviceUse.LeftHand:
                    transform = prefab.leftHand;
                    break;

                case DeviceUse.RightHand:
                    transform = prefab.rightHand;
                    break;

                case DeviceUse.Waist:
                    transform = prefab.pelvis;
                    break;

                case DeviceUse.LeftFoot:
                    transform = prefab.leftLeg;
                    break;

                case DeviceUse.RightFoot:
                    transform = prefab.rightLeg;
                    break;
            }

            if (!transform) return false;

            pose = new Pose(transform.position * avatar.scale, transform.rotation);

            _trackingHelper.ApplyInverseRoomAdjust(ref pose.position, ref pose.rotation);

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
