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
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;
using VRUIControls;
using Zenject;

namespace CustomAvatar.Tracking
{
    internal class HumanoidCalibrator : MonoBehaviour
    {
        // Average human is 7.5 heads tall. Eyes are at 7 heads from the floor (middle of the head) and pelvis is at 4 heads from the floor.
        // Note that the actual value doesn't really matter that much, it's mostly to make the positioning easier to understand.
        internal const float kEyeHeightToPelvisHeightRatio = 4f / 7f;

        private const float kCenterToFoot = 0.08f;

        private const float kMinPressValue = VRInputModule.kMinPressValue;

        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _center;

        private TrackingRig _trackingRig;
        private CalibrationData _calibrationData;
        private Settings _settings;
        private PlayerAvatarManager _playerAvatarManager;

        [field: SerializeField]
        internal CalibrationPoint pelvis { get; private set; }

        [field: SerializeField]
        internal CalibrationPoint leftFoot { get; private set; }

        [field: SerializeField]
        internal CalibrationPoint rightFoot { get; private set; }

        [Inject]
        internal void Construct(TrackingRig trackingRig, CalibrationData calibrationData, Settings settings, PlayerAvatarManager playerAvatarManager)
        {
            _trackingRig = trackingRig;
            _calibrationData = calibrationData;
            _settings = settings;
            _playerAvatarManager = playerAvatarManager;
        }

        internal void ApplyCalibration()
        {
            ReadCalibrationTransforms(_calibrationData.calibration);
        }

        internal void ApplyAdjust()
        {
            ReadAdjustTransforms(_calibrationData.manualOffset);
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

        protected void OnEnable()
        {
            _trackingRig.trackingChanged += OnTrackingChanged;

            OnTrackingChanged();
        }

        protected void Update()
        {
            float playerEyeHeight = _trackingRig.eyeHeight;

            _playerAvatarManager.ResizeCurrentAvatar(playerEyeHeight);

            Transform center = _center;
            Transform head = _trackingRig.head.transform;

            // We want the user's head rotation to match the avatar's head directly rather than assuming whatever
            // position they're in is forward. I'm not sure how I feel about doing that, but it's what VRChat does.
            Vector3 up = _parent.up;
            Vector3 position = _parent.position;

            center.SetPositionAndRotation(
                Vector3.ProjectOnPlane(head.position - position, up) + position,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(head.forward, up), up));

            UpdateCalibrationPoint(pelvis, new Vector3(0, playerEyeHeight * kEyeHeightToPelvisHeightRatio, 0));
            UpdateCalibrationPoint(leftFoot, new Vector3(playerEyeHeight * -kCenterToFoot, 0, 0));
            UpdateCalibrationPoint(rightFoot, new Vector3(playerEyeHeight * kCenterToFoot, 0, 0));

            // need to use trigger rather than triggerButton since the latter is not bound on all controller types with SteamVR
            if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue) &&
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue) &&
                leftTriggerValue >= kMinPressValue && rightTriggerValue >= kMinPressValue)
            {
                _settings.playerEyeHeight.value = playerEyeHeight;

                ApplyCalibration(pelvis);
                ApplyCalibration(leftFoot);
                ApplyCalibration(rightFoot);

                WriteCalibrationTransforms(_calibrationData.calibration);

                _trackingRig.EndCalibration();
            }
        }

        protected void OnDisable()
        {
            _trackingRig.trackingChanged -= OnTrackingChanged;
        }

        private void OnTrackingChanged()
        {
            SetActive(pelvis);
            SetActive(leftFoot);
            SetActive(rightFoot);
        }

        private void SetActive(CalibrationPoint calibrationPoint)
        {
            calibrationPoint.gameObject.SetActive(calibrationPoint.node.isTracking);
        }

        private void UpdateCalibrationPoint(CalibrationPoint calibrationPoint, Vector3 targetPosition)
        {
            calibrationPoint.transform.SetPositionAndRotation(_center.TransformPoint(targetPosition), _center.rotation);
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

        private void ApplyCalibration(CalibrationPoint calibrationPoint)
        {
            CalibratableNode node = calibrationPoint.node;

            if (node.isTracking)
            {
                calibrationPoint.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                node.calibration.SetPositionAndRotation(position, rotation);
            }
            else
            {
                node.calibration.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        internal class Mirrored : MonoBehaviour
        {
            [field: SerializeField]
            internal MirroredCalibrationPoint pelvis { get; private set; }

            [field: SerializeField]
            internal MirroredCalibrationPoint leftFoot { get; private set; }

            [field: SerializeField]
            internal MirroredCalibrationPoint rightFoot { get; private set; }

            private HumanoidCalibrator _humanoidCalibrator;

            internal void Init(HumanoidCalibrator humanoidCalibrator)
            {
                _humanoidCalibrator = humanoidCalibrator;
            }

            internal void OnBeforeRender()
            {
                transform.CopyLocalPositionAndRotationFrom(_humanoidCalibrator.transform);

                pelvis.CopyFrom(_humanoidCalibrator.pelvis);
                leftFoot.CopyFrom(_humanoidCalibrator.leftFoot);
                rightFoot.CopyFrom(_humanoidCalibrator.rightFoot);
            }
        }
    }
}
