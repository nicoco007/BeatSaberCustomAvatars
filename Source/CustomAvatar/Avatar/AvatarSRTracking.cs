using DynamicOpenVR.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
using ViveSR;
using ViveSR.anipal;
using ViveSR.anipal.Eye;

namespace CustomAvatar.Avatar
{
    internal class AvatarSRTracking : MonoBehaviour
    {
        private Animator _animator;
        public enum FrameworkStatus { STOP, START, WORKING, ERROR, NOT_SUPPORT }
        public FrameworkStatus Status { get; protected set; }
        Quaternion basicLeftEyeRot;
        Quaternion basicRightEyeRot;
        private void Start()
        {
            if (!SRanipal_Eye_API.IsViveProEye())
            {
                return;
            }
            if (Status == FrameworkStatus.WORKING)
            {
                return;
            }
            _animator = GetComponentInChildren<Animator>();
            if (!_animator)
            {
                return;
            }
            var leftEyeTransform = _animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var rightEyeTransform = _animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (!leftEyeTransform || !rightEyeTransform)
            {
                return;
            }
            basicLeftEyeRot = leftEyeTransform.localRotation;
            basicRightEyeRot = rightEyeTransform.localRotation;

            Error result = SRanipal_API.Initial(SRanipal_Eye.ANIPAL_TYPE_EYE, IntPtr.Zero);
            if (result == Error.WORK)
            {
                Plugin.logger.Info($"[SRanipal] Initial Eye: {result}");
                Status = FrameworkStatus.WORKING;
            }
            else
            {
                Plugin.logger.Error($"[SRanipal] Initial Eye: {result}");
                Status = FrameworkStatus.ERROR;
            }
        }
        private void OnDestroy()
        {
            Error result = SRanipal_API.Release(SRanipal_Eye.ANIPAL_TYPE_EYE);
            if (result == Error.WORK)
                Plugin.logger.Info($"[SRanipal] Release Eye: {result}");
            else
                Plugin.logger.Error($"[SRanipal] Release Eye: {result}");
        }
        private void Update()
        {
            ApplyEyeTracking();
        }

        private void ApplyEyeTracking()
        {
            if (Status != FrameworkStatus.WORKING)
            {
                return;
            }
            EyeData EyeData_ = new EyeData();
            var result = SRanipal_Eye_API.GetEyeData(ref EyeData_);
            UpdateEye(EyeData_);
        }

        private Vector2 EyeDataToRotation(float perx, float pery)
        {
            Vector2 result = new Vector2();
            float xOffset = perx * 2.0f - 1.0f;
            float yOffset = (pery + 0.03f) * 2.0f - 1.0f;

            result.x = xOffset * -9.9f; // plus y
            result.y = yOffset * -6.6f; // plus x
            return result;
        }
        private void UpdateEye(EyeData eyeData)
        {
            SingleEyeData leftEyeData = eyeData.verbose_data.left;
            if(leftEyeData.eye_openness > 0.3f && leftEyeData.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY))
            {
                Vector2 Rotation = EyeDataToRotation(leftEyeData.pupil_position_in_sensor_area.x, leftEyeData.pupil_position_in_sensor_area.y);
                float scale = (0.2f - Math.Min(0.0f, 0.5f - leftEyeData.eye_openness)) * 2.5f + 0.5f;
                Rotation *= scale;

                Transform boneTransform = _animator.GetBoneTransform(HumanBodyBones.LeftEye);
                Vector3 angles = basicLeftEyeRot.eulerAngles;
                boneTransform.localRotation = Quaternion.Euler(angles.x + Rotation.y, angles.y + Rotation.x, angles.z);
            }

            SingleEyeData rightEyeData = eyeData.verbose_data.right;
            if (rightEyeData.eye_openness > 0.3f && rightEyeData.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY))
            {
                Vector2 Rotation = EyeDataToRotation(rightEyeData.pupil_position_in_sensor_area.x, rightEyeData.pupil_position_in_sensor_area.y);
                float scale = (0.2f - Math.Min(0.0f, 0.5f - rightEyeData.eye_openness)) * 2.5f + 0.5f;
                Rotation *= scale;

                Transform boneTransform = _animator.GetBoneTransform(HumanBodyBones.RightEye);
                Vector3 angles = basicRightEyeRot.eulerAngles;
                boneTransform.localRotation = Quaternion.Euler(angles.x + Rotation.y, angles.y + Rotation.x, angles.z);
            }
        }
    }
}
