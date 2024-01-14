//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar.Tracking
{
    internal class HumanoidCalibrator
    {
        private const float kEyeHeightToPelvisHeightRatio = 3.5f / 7f;

        private readonly TrackingRig _trackingRig;
        private readonly CalibrationData _calibrationData;
        private readonly Settings _settings;
        private readonly ActiveOriginManager _activeOriginManager;
        private readonly MainSettingsModelSO _mainSettingsModel;

        internal HumanoidCalibrator(TrackingRig trackingRig, CalibrationData calibrationData, Settings settings, ActiveOriginManager activeOriginManager, MainSettingsModelSO mainSettingsModel)
        {
            _trackingRig = trackingRig;
            _calibrationData = calibrationData;
            _settings = settings;
            _activeOriginManager = activeOriginManager;
            _mainSettingsModel = mainSettingsModel;
        }

        internal void ApplyAutomaticCalibration()
        {
            ReadCalibrationTransforms(_calibrationData.automaticCalibration);
        }

        internal void ApplyManualCalibration(SpawnedAvatar spawnedAvatar)
        {
            ReadCalibrationTransforms(_calibrationData.GetAvatarManualCalibration(spawnedAvatar));
        }

        internal void ApplyNoCalibration()
        {
            _trackingRig.headCalibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _trackingRig.pelvisCalibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _trackingRig.leftFootCalibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _trackingRig.rightFootCalibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        internal void CalibrateAutomatically()
        {
            _settings.playerEyeHeight.value = _trackingRig.eyeHeight;
            float playerEyeHeight = _trackingRig.eyeHeight;

            // temporarily reset tracking rig scale
            _trackingRig.fullBodyTracking.localScale = Vector3.one;
            _trackingRig.fullBodyTracking.localPosition = Vector3.zero;

            Transform center = CreateCenter(_activeOriginManager.current, playerEyeHeight);

            if (_settings.moveFloorWithRoomAdjust)
            {
                center.position += center.TransformVector(0, _mainSettingsModel.roomCenter.value.y, 0);
            }

            Vector3 leftFootPos = center.InverseTransformPoint(_trackingRig.leftFoot.transform.position);
            Vector3 rightFootPos = center.InverseTransformPoint(_trackingRig.rightFoot.transform.position);

            ApplyCalibration(center, _trackingRig.head, _trackingRig.headCalibration, new Vector3(0, playerEyeHeight, 0), Quaternion.Inverse(center.rotation) * _trackingRig.head.transform.rotation);
            ApplyCalibration(center, _trackingRig.pelvis, _trackingRig.pelvisCalibration, new Vector3(0, playerEyeHeight * kEyeHeightToPelvisHeightRatio, 0), Quaternion.identity);
            ApplyCalibration(center, _trackingRig.leftFoot, _trackingRig.leftFootCalibration, new Vector3(leftFootPos.x, 0, 0), Quaternion.Euler(0, -10f, 0));
            ApplyCalibration(center, _trackingRig.rightFoot, _trackingRig.rightFootCalibration, new Vector3(rightFootPos.x, 0, 0), Quaternion.Euler(0, 10f, 0));

            WriteCalibrationTransforms(_calibrationData.automaticCalibration);

            Object.Destroy(center.gameObject);
        }

        internal void CalibrateManually(SpawnedAvatar spawnedAvatar)
        {
            if (spawnedAvatar == null || spawnedAvatar.ik == null)
            {
                return;
            }

            _settings.playerEyeHeight.value = _trackingRig.eyeHeight;

            VRIKManager vrikManager = spawnedAvatar.ik.vrikManager;

            ApplyManualCalibration(_trackingRig.head, _trackingRig.headCalibration, vrikManager.references_head);
            ApplyManualCalibration(_trackingRig.pelvis, _trackingRig.pelvisCalibration, vrikManager.references_pelvis);
            ApplyManualCalibration(_trackingRig.leftFoot, _trackingRig.leftFootCalibration, FirstNonNullUnityObject(vrikManager.references_leftToes, vrikManager.references_leftFoot));
            ApplyManualCalibration(_trackingRig.rightFoot, _trackingRig.rightFootCalibration, FirstNonNullUnityObject(vrikManager.references_rightToes, vrikManager.references_rightFoot));

            WriteCalibrationTransforms(_calibrationData.GetAvatarManualCalibration(spawnedAvatar));
        }

        internal void ClearAutomaticFullBodyTrackingData()
        {
            WriteIdentity(_calibrationData.automaticCalibration);
        }

        internal void ClearManualFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            WriteIdentity(_calibrationData.GetAvatarManualCalibration(spawnedAvatar));
        }

        internal void ReadCalibrationTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            _trackingRig.headCalibration.SetLocalPositionAndRotation(calibration.head.position, calibration.head.rotation);
            _trackingRig.pelvisCalibration.SetLocalPositionAndRotation(calibration.waist.position, calibration.waist.rotation);
            _trackingRig.leftFootCalibration.SetLocalPositionAndRotation(calibration.leftFoot.position, calibration.leftFoot.rotation);
            _trackingRig.rightFootCalibration.SetLocalPositionAndRotation(calibration.rightFoot.position, calibration.rightFoot.rotation);
        }

        private void WriteCalibrationTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.head = new Pose(_trackingRig.headCalibration.localPosition, _trackingRig.headCalibration.localRotation);
            calibration.waist = new Pose(_trackingRig.pelvisCalibration.localPosition, _trackingRig.pelvisCalibration.localRotation);
            calibration.leftFoot = new Pose(_trackingRig.leftFootCalibration.localPosition, _trackingRig.leftFootCalibration.localRotation);
            calibration.rightFoot = new Pose(_trackingRig.rightFootCalibration.localPosition, _trackingRig.rightFootCalibration.localRotation);
        }

        private void WriteIdentity(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.head = Pose.identity;
            calibration.waist = Pose.identity;
            calibration.leftFoot = Pose.identity;
            calibration.rightFoot = Pose.identity;
        }

        private void ApplyCalibration(Transform center, TrackedNode device, Transform calibration, Vector3 target, Quaternion targetRotation)
        {
            if (device.isTracking)
            {
                calibration.SetLocalPositionAndRotation(
                    device.transform.InverseTransformPoint(center.TransformPoint(target)),
                    Quaternion.Inverse(device.transform.rotation) * (center.rotation * targetRotation));
            }
            else
            {
                calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private void ApplyManualCalibration(TrackedNode device, Transform calibration, Transform target)
        {
            if (device.isTracking)
            {
                calibration.SetLocalPositionAndRotation(
                    device.transform.InverseTransformPoint(target.position),
                    Quaternion.Inverse(device.transform.rotation) * target.rotation);
            }
            else
            {
                calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private Transform CreateCenter(Transform parent, float playerEyeHeight)
        {
            Transform center = new GameObject("Center").transform;

            center.SetParent(parent, false);
            center.SetPositionAndRotation(
                _trackingRig.head.transform.position - _trackingRig.head.transform.TransformVector(new Vector3(0, 0, GetHeadOffsetFromHeight(playerEyeHeight))),
                Quaternion.LookRotation(Vector3.ProjectOnPlane(_trackingRig.head.transform.forward, parent.up), parent.up));

            // put center on the ground
            center.localPosition = new Vector3(center.localPosition.x, 0, center.localPosition.z);

            return center;
        }

        private float GetHeadOffsetFromHeight(float eyeHeight)
        {
            // this is loosely based on height vs head circumference derived from average height & head circumference at different ages
            return 0.08f + 0.000175f * eyeHeight;
        }

        // TODO: shove this into a shared helper
        private T FirstNonNullUnityObject<T>(params T[] objects) where T : Object => objects.FirstOrDefault(o => o != null);
    }
}
