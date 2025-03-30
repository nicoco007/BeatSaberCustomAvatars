//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

extern alias BeatSaberFinalIK;

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar.Tracking
{
    internal class HumanoidCalibrator
    {
        // Average human is 7.5 heads tall. Eyes are at 7 heads from the floor (middle of the head) and pelvis is at 4 heads from the floor.
        // Note that the actual value doesn't really matter that much, it's mostly to make the positioning easier to understand.
        internal const float kEyeHeightToPelvisHeightRatio = 4f / 7f;

        private readonly TrackingRig _trackingRig;
        private readonly CalibrationData _calibrationData;
        private readonly Settings _settings;
        private readonly ActiveOriginManager _activeOriginManager;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly PlayerAvatarManager _playerAvatarManager;

        internal HumanoidCalibrator(TrackingRig trackingRig, CalibrationData calibrationData, Settings settings, ActiveOriginManager activeOriginManager, BeatSaberUtilities beatSaberUtilities, PlayerAvatarManager playerAvatarManager)
        {
            _trackingRig = trackingRig;
            _calibrationData = calibrationData;
            _settings = settings;
            _activeOriginManager = activeOriginManager;
            _beatSaberUtilities = beatSaberUtilities;
            _playerAvatarManager = playerAvatarManager;
        }

        internal void ApplyAutomaticCalibration()
        {
            ReadCalibrationTransforms(_calibrationData.automaticCalibration);
        }

        internal void ApplyManualCalibration()
        {
            ReadCalibrationTransforms(_playerAvatarManager.currentManualCalibration);
        }

        internal void ApplyNoCalibration()
        {
            _trackingRig.head.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _trackingRig.pelvis.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _trackingRig.leftFoot.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _trackingRig.rightFoot.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        internal void CalibrateAutomatically()
        {
            _settings.playerEyeHeight.value = _trackingRig.eyeHeight;
            float playerEyeHeight = _trackingRig.eyeHeight;

            // temporarily reset tracking rig scale
            _trackingRig.fullBodyTracking.localScale = Vector3.one;
            _trackingRig.fullBodyTracking.localPosition = Vector3.zero;

            Transform center = CreateCenter();

            Vector3 leftFootPos = center.InverseTransformPoint(_trackingRig.leftFoot.transform.position);
            Vector3 rightFootPos = center.InverseTransformPoint(_trackingRig.rightFoot.transform.position);

            ApplyCalibration(center, _trackingRig.head, new Vector3(0, playerEyeHeight, 0), Quaternion.identity);
            ApplyCalibration(center, _trackingRig.pelvis, new Vector3(0, playerEyeHeight * kEyeHeightToPelvisHeightRatio, 0), Quaternion.identity);
            ApplyCalibration(center, _trackingRig.leftFoot, new Vector3(leftFootPos.x, 0, 0), Quaternion.Euler(0, -10f, 0));
            ApplyCalibration(center, _trackingRig.rightFoot, new Vector3(rightFootPos.x, 0, 0), Quaternion.Euler(0, 10f, 0));

            WriteCalibrationTransforms(_calibrationData.automaticCalibration);

            Object.Destroy(center.gameObject);
        }

        internal void CalibrateManually()
        {
            SpawnedAvatar spawnedAvatar = _playerAvatarManager.currentlySpawnedAvatar;

            if (spawnedAvatar == null || spawnedAvatar.ik == null)
            {
                return;
            }

            _settings.playerEyeHeight.value = _trackingRig.eyeHeight;

            VRIKManager vrikManager = spawnedAvatar.ik.vrikManager;

            ApplyManualCalibration(_trackingRig.head, vrikManager.references_head);
            ApplyManualCalibration(_trackingRig.pelvis, vrikManager.references_pelvis);
            ApplyManualCalibration(_trackingRig.leftFoot, UnityUtilities.FirstNonNullUnityObject(vrikManager.references_leftToes, vrikManager.references_leftFoot));
            ApplyManualCalibration(_trackingRig.rightFoot, UnityUtilities.FirstNonNullUnityObject(vrikManager.references_rightToes, vrikManager.references_rightFoot));

            WriteCalibrationTransforms(_playerAvatarManager.currentManualCalibration);
        }

        internal void ClearAutomaticFullBodyTrackingData()
        {
            WriteIdentity(_calibrationData.automaticCalibration);
        }

        internal void ClearManualFullBodyTrackingData()
        {
            WriteIdentity(_playerAvatarManager.currentManualCalibration);
        }

        internal void ReadCalibrationTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            _trackingRig.head.calibration.SetLocalPositionAndRotation(calibration.head.position, calibration.head.rotation);
            _trackingRig.pelvis.calibration.SetLocalPositionAndRotation(calibration.waist.position, calibration.waist.rotation);
            _trackingRig.leftFoot.calibration.SetLocalPositionAndRotation(calibration.leftFoot.position, calibration.leftFoot.rotation);
            _trackingRig.rightFoot.calibration.SetLocalPositionAndRotation(calibration.rightFoot.position, calibration.rightFoot.rotation);
        }

        private void WriteCalibrationTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.head = new Pose(_trackingRig.head.calibration.localPosition, _trackingRig.head.calibration.localRotation);
            calibration.waist = new Pose(_trackingRig.pelvis.calibration.localPosition, _trackingRig.pelvis.calibration.localRotation);
            calibration.leftFoot = new Pose(_trackingRig.leftFoot.calibration.localPosition, _trackingRig.leftFoot.calibration.localRotation);
            calibration.rightFoot = new Pose(_trackingRig.rightFoot.calibration.localPosition, _trackingRig.rightFoot.calibration.localRotation);
        }

        private void WriteIdentity(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.head = Pose.identity;
            calibration.waist = Pose.identity;
            calibration.leftFoot = Pose.identity;
            calibration.rightFoot = Pose.identity;
        }

        private void ApplyCalibration(Transform center, GenericNode device, Vector3 target, Quaternion targetRotation)
        {
            if (device.isTracking)
            {
                device.calibration.SetPositionAndRotation(center.TransformPoint(target), center.rotation * targetRotation);
            }
            else
            {
                device.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private void ApplyManualCalibration(GenericNode device, Transform target)
        {
            if (device.isTracking)
            {
                device.calibration.SetPositionAndRotation(target.position, target.rotation);
            }
            else
            {
                device.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private Transform CreateCenter()
        {
            Transform center = new GameObject("Center").transform;
            Transform head = _trackingRig.head.transform;
            Transform parent = _activeOriginManager.current;

            // We want the user's head rotation to match the avatar's head directly rather than assuming whatever
            // position they're in is forward. I'm not sure how I feel about doing that, but it's what VRChat does.
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(head.forward, parent.up), parent.up);
            head.rotation = rotation;

            center.SetParent(parent, false);
            center.SetPositionAndRotation(head.position, rotation);

            // put center on the ground
            center.localPosition = new Vector3(center.localPosition.x, 0, center.localPosition.z);

            if (_settings.moveFloorWithRoomAdjust)
            {
                center.position += center.TransformVector(0, _beatSaberUtilities.roomCenter.y, 0);
            }

            return center;
        }
    }
}
