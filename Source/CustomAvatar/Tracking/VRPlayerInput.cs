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
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using DynamicOpenVR.IO;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Tracking
{
    /// <summary>
    /// The player's <see cref="IAvatarInput"/> with calibration and other settings applied.
    /// </summary>
    public class VRPlayerInput : IAvatarInput
    {
        public bool allowMaintainPelvisPosition => _avatarSettings.allowMaintainPelvisPosition;

        public event Action inputChanged;

        private readonly ITrackedDeviceManager _deviceManager;
        private readonly Settings _settings;
        private readonly Settings.AvatarSpecificSettings _avatarSettings;
        private readonly CalibrationData _calibrationData;
        private readonly CalibrationData.FullBodyCalibration _manualCalibration;

        private readonly SkeletalInput _leftHandAnimAction;
        private readonly SkeletalInput _rightHandAnimAction;

        private Pose _previousWaistPose;
        private Pose _previousLeftFootPose;
        private Pose _previousRightFootPose;

        private bool _shouldTrackFullBody =>
            _avatarSettings.bypassCalibration ||
            !_avatarSettings.useAutomaticCalibration && _manualCalibration.isCalibrated ||
            _avatarSettings.useAutomaticCalibration && _calibrationData.automaticCalibration.isCalibrated;

        [Inject]
        internal VRPlayerInput(ITrackedDeviceManager trackedDeviceManager, LoadedAvatar avatar, Settings settings, CalibrationData calibrationData)
        {
            _deviceManager = trackedDeviceManager;
            _settings = settings;
            _avatarSettings = settings.GetAvatarSettings(avatar.fileName);
            _calibrationData = calibrationData;
            _manualCalibration = calibrationData.GetAvatarManualCalibration(avatar.fileName);

            _deviceManager.deviceAdded += OnDevicesUpdated;
            _deviceManager.deviceRemoved += OnDevicesUpdated;
            _deviceManager.deviceTrackingAcquired += OnDevicesUpdated;
            _deviceManager.deviceTrackingLost += OnDevicesUpdated;
            
            _leftHandAnimAction  = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");
        }

        public bool TryGetPose(DeviceUse use, out Pose pose)
        {
            switch (use)
            {
                case DeviceUse.Head:
                case DeviceUse.LeftHand:
                case DeviceUse.RightHand:
                    return TryGetRawPose(use, out pose);

                case DeviceUse.Waist:
                    return TryGetWaistPose(out pose);

                case DeviceUse.LeftFoot:
                    return TryGetLeftFootPose(out pose);

                case DeviceUse.RightFoot:
                    return TryGetRightFootPose(out pose);

                default:
                    pose = Pose.identity;
                    return false;
            }
        }

        public bool TryGetRawPose(DeviceUse use, out Pose pose)
        {
            if (!_deviceManager.TryGetDeviceState(use, out ITrackedDeviceState device) || !device.isConnected || !device.isTracking)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(device.position, device.rotation);
            return true;
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

        public void Dispose()
        {
            _deviceManager.deviceAdded -= OnDevicesUpdated;
            _deviceManager.deviceRemoved -= OnDevicesUpdated;
            _deviceManager.deviceTrackingAcquired -= OnDevicesUpdated;
            _deviceManager.deviceTrackingLost -= OnDevicesUpdated;

            _leftHandAnimAction.Dispose();
            _rightHandAnimAction.Dispose();
        }

        private bool TryGetTrackerPose(DeviceUse use, Pose previousPose, Pose correction, Settings.TrackedPointSmoothing smoothing, out Pose pose)
        {
            if (!_shouldTrackFullBody || !TryGetRawPose(use, out Pose currentPose))
            {
                pose = Pose.identity;
                return false;
            }

            Quaternion correctedRotation = currentPose.rotation * correction.rotation;
            Vector3 correctedPosition = currentPose.position + correctedRotation * correction.position; // correction is forward-facing by definition

            pose = new Pose(Vector3.Lerp(previousPose.position, correctedPosition, smoothing.position), Quaternion.Slerp(previousPose.rotation, correctedRotation, smoothing.rotation));
            return true;
        }

        private bool TryGetWaistPose(out Pose pose)
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

        private bool TryGetLeftFootPose(out Pose pose)
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

        private bool TryGetRightFootPose(out Pose pose)
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

        private void OnDevicesUpdated(ITrackedDeviceState state)
        {
            inputChanged?.Invoke();
        }
    }
}
