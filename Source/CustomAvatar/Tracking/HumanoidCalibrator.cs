//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Configuration;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Tracking
{
    internal class HumanoidCalibrator : MonoBehaviour
    {
        // Average human is 7.5 heads tall. Eyes are at 7 heads from the floor (middle of the head) and pelvis is at 4 heads from the floor.
        // Note that the actual value doesn't really matter that much, it's mostly to make the positioning easier to understand.
        internal const float kEyeHeightToPelvisHeightRatio = 4f / 7f;

        private TrackingRig _trackingRig;
        private CalibrationData _calibrationData;
        private Settings _settings;
        private BeatSaberUtilities _beatSaberUtilities;

        [Inject]
        internal void Construct(TrackingRig trackingRig, CalibrationData calibrationData, Settings settings, BeatSaberUtilities beatSaberUtilities)
        {
            _trackingRig = trackingRig;
            _calibrationData = calibrationData;
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
        }

        internal void ApplyCalibration()
        {
            ReadCalibrationTransforms(_calibrationData.calibration);
        }

        internal void ApplyAdjust()
        {
            ReadAdjustTransforms(_calibrationData.manualOffset);
        }

        internal void Calibrate()
        {
            _settings.playerEyeHeight.value = _trackingRig.eyeHeight;
            float playerEyeHeight = _trackingRig.eyeHeight;

            // temporarily reset tracking rig scale
            _trackingRig.fullBodyTracking.localScale = Vector3.one;
            _trackingRig.fullBodyTracking.localPosition = Vector3.zero;

            Transform center = transform;
            Transform head = _trackingRig.head.transform;

            // We want the user's head rotation to match the avatar's head directly rather than assuming whatever
            // position they're in is forward. I'm not sure how I feel about doing that, but it's what VRChat does.
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(head.forward, center.up), center.up);
            head.rotation = rotation;

            center.SetPositionAndRotation(head.position, rotation);

            // put center on the ground
            center.localPosition = new Vector3(center.localPosition.x, 0, center.localPosition.z);

            if (_settings.moveFloorWithRoomAdjust)
            {
                center.position += center.TransformVector(0, _beatSaberUtilities.roomCenter.y, 0);
            }

            Vector3 leftFootPos = center.InverseTransformPoint(_trackingRig.leftFoot.transform.position);
            Vector3 rightFootPos = center.InverseTransformPoint(_trackingRig.rightFoot.transform.position);

            ApplyCalibration(center, _trackingRig.pelvis, new Vector3(0, playerEyeHeight * kEyeHeightToPelvisHeightRatio, 0), Quaternion.identity);
            ApplyCalibration(center, _trackingRig.leftFoot, new Vector3(leftFootPos.x, 0, 0), Quaternion.Euler(0, -10f, 0));
            ApplyCalibration(center, _trackingRig.rightFoot, new Vector3(rightFootPos.x, 0, 0), Quaternion.Euler(0, 10f, 0));

            WriteCalibrationTransforms(_calibrationData.calibration);
        }

        internal void Adjust()
        {
            WriteAdjustTransforms(_calibrationData.manualOffset);
        }

        internal void ClearAdjust()
        {
            WriteIdentity(_calibrationData.manualOffset);
            ReadAdjustTransforms(_calibrationData.manualOffset);
        }

        internal void ReadCalibrationTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            _trackingRig.pelvis.calibration.SetLocalPositionAndRotation(calibration.waist.position, calibration.waist.rotation);
            _trackingRig.leftFoot.calibration.SetLocalPositionAndRotation(calibration.leftFoot.position, calibration.leftFoot.rotation);
            _trackingRig.rightFoot.calibration.SetLocalPositionAndRotation(calibration.rightFoot.position, calibration.rightFoot.rotation);
        }

        internal void ReadAdjustTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            _trackingRig.pelvis.manualOffset.SetLocalPose(calibration.waist);
            _trackingRig.leftFoot.manualOffset.SetLocalPose(calibration.leftFoot);
            _trackingRig.rightFoot.manualOffset.SetLocalPose(calibration.rightFoot);
        }

        private void WriteCalibrationTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.waist = _trackingRig.pelvis.calibration.GetLocalPose();
            calibration.leftFoot = _trackingRig.leftFoot.calibration.GetLocalPose();
            calibration.rightFoot = _trackingRig.rightFoot.calibration.GetLocalPose();
        }

        private void WriteAdjustTransforms(CalibrationData.FullBodyCalibration calibration)
        {
            calibration.waist = _trackingRig.pelvis.manualOffset.GetLocalPose();
            calibration.leftFoot = _trackingRig.leftFoot.manualOffset.GetLocalPose();
            calibration.rightFoot = _trackingRig.rightFoot.manualOffset.GetLocalPose();
        }

        private void WriteIdentity(CalibrationData.FullBodyCalibration calibration)
        {
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
    }
}
