﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using System.Linq;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using VRIK = BeatSaberFinalIK::RootMotion.FinalIK.VRIK;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
    public class AvatarPrefab : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="AvatarDescriptor"/> retrieved from the root object on the prefab.
        /// </summary>
        public AvatarDescriptor descriptor { get; private set; }

        /// <summary>
        /// Whether or not this avatar has IK.
        /// </summary>
        public bool isIKAvatar { get; private set; }

        /// <summary>
        /// Whether or not this avatar supports finger tracking.
        /// </summary>
        public bool supportsFingerTracking { get; private set; }

        /// <summary>
        /// The avatar's eye height.
        /// </summary>
        public float eyeHeight { get; private set; }

        /// <summary>
        /// The avatar's estimated arm span.
        /// </summary>
        public float armSpan { get; private set; }

        internal Transform head { get; private set; }

        internal Transform leftHand { get; private set; }

        internal Transform rightHand { get; private set; }

        internal Transform leftLeg { get; private set; }

        internal Transform rightLeg { get; private set; }

        internal Transform pelvis { get; private set; }

        internal Pose headOffset { get; private set; }

        internal Pose leftHandOffset { get; private set; }

        internal Pose rightHandOffset { get; private set; }

        internal Pose pelvisOffset { get; private set; }

        internal Pose leftLegOffset { get; private set; }

        internal Pose rightLegOffset { get; private set; }

        internal Pose headCalibrationOffset { get; private set; }

        internal Pose pelvisCalibrationOffset { get; private set; }

        internal Pose leftFootCalibrationOffset { get; private set; }

        internal Pose rightFootCalibrationOffset { get; private set; }

        internal Vector3 pelvisRootForward { get; private set; }

        internal Vector3 pelvisRootOffset { get; private set; }

        private ILogger<AvatarPrefab> _logger;

        [Inject]
        [UsedImplicitly]
        private void Construct(ILoggerFactory loggerFactory, DiContainer container)
        {
            descriptor = GetComponent<AvatarDescriptor>();

            if (descriptor == null)
            {
                throw new AvatarLoadException($"Avatar '{descriptor.name}' does not have an AvatarDescriptor");
            }

            _logger = loggerFactory.CreateLogger<AvatarPrefab>(descriptor.name);

            head = transform.Find("Head");
            leftHand = transform.Find("LeftHand");
            rightHand = transform.Find("RightHand");
            pelvis = transform.Find("Pelvis");
            leftLeg = transform.Find("LeftLeg");
            rightLeg = transform.Find("RightLeg");

            VRIKManager vrikManager = GetComponentInChildren<VRIKManager>();
#pragma warning disable CS0618
            IKManager ikManager = GetComponentInChildren<IKManager>();
#pragma warning restore CS0618

            // migrate IKManager/IKManagerAdvanced to VRIKManager
            if (ikManager != null)
            {
                if (vrikManager == null)
                {
                    vrikManager = container.InstantiateComponent<VRIKManager>(ikManager.gameObject);
                }

                _logger.LogWarning("IKManager and IKManagerAdvanced are deprecated; please migrate to VRIKManager");

                ApplyIKManagerFields(vrikManager, ikManager);
                Destroy(ikManager);
            }

            // remove any existing VRIK instances
            foreach (VRIK existingVrik in GetComponentsInChildren<VRIK>())
            {
                _logger.LogWarning($"Found VRIK on '{existingVrik.name}'; VRIK no longer gets deserialized properly, please remove it");
                Destroy(existingVrik);
            }

            if (vrikManager != null)
            {
                if (!vrikManager.areReferencesFilled)
                {
                    _logger.LogWarning($"References are not filled on '{vrikManager.name}'; detecting references automatically");
                    vrikManager.AutoDetectReferences();

                    if (!vrikManager.areReferencesFilled)
                    {
                        _logger.LogWarning($"References are not filled on '{vrikManager.name}'; avatar will probably not work as expected");
                    }
                }

                FixTrackingReferences(vrikManager);
            }

            if (transform.localPosition.sqrMagnitude > 0)
            {
                _logger.LogWarning("Avatar root position is not at origin; this may cause unexpected issues");
            }

            PoseManager poseManager = GetComponentInChildren<PoseManager>();

            isIKAvatar = vrikManager != null && vrikManager.areReferencesFilled;
            supportsFingerTracking = poseManager && poseManager.isValid;

            eyeHeight = GetEyeHeight();
            armSpan = GetArmSpan(vrikManager);

            if (vrikManager != null)
            {
                headOffset = GetOffset(head, vrikManager.solver_spine_headTarget, vrikManager.references_head);
                leftHandOffset = GetOffset(leftHand, vrikManager.solver_leftArm_target, vrikManager.references_leftHand);
                rightHandOffset = GetOffset(rightHand, vrikManager.solver_rightArm_target, vrikManager.references_rightHand);
                pelvisOffset = GetOffset(pelvis, vrikManager.solver_spine_pelvisTarget, vrikManager.references_pelvis);
                leftLegOffset = GetOffset(leftLeg, vrikManager.solver_leftLeg_target, vrikManager.references_leftToes, vrikManager.references_leftFoot);
                rightLegOffset = GetOffset(rightLeg, vrikManager.solver_rightLeg_target, vrikManager.references_rightToes, vrikManager.references_rightFoot);

                // These offsets are in mostly arbitrary positions. The idea is that they should be in predictable
                // positions so the user can calibrate once and it'll apply to all avatars in the same way.
                if (head != null)
                {
                    Vector3 centerPosition = head.position;
                    GameObject targetObj = new("Target");
                    Transform target = targetObj.transform;

                    headCalibrationOffset = GetCalibrationOffset(target, new Pose(centerPosition, Quaternion.identity), vrikManager.references_head);
                    pelvisCalibrationOffset = GetCalibrationOffset(target, new Pose(new Vector3(centerPosition.x, centerPosition.y * HumanoidCalibrator.kEyeHeightToPelvisHeightRatio, centerPosition.z), Quaternion.identity), vrikManager.references_pelvis);
                    leftFootCalibrationOffset = GetCalibrationOffset(target, GetFootTarget(centerPosition, vrikManager.references_leftFoot, vrikManager.references_leftToes), vrikManager.references_leftToes, vrikManager.references_leftFoot);
                    rightFootCalibrationOffset = GetCalibrationOffset(target, GetFootTarget(centerPosition, vrikManager.references_rightFoot, vrikManager.references_rightToes), vrikManager.references_rightToes, vrikManager.references_rightFoot);

                    Destroy(targetObj);
                }

                pelvisRootForward = Quaternion.Inverse(vrikManager.references_pelvis.rotation) * vrikManager.references_root.forward;
                pelvisRootOffset = vrikManager.references_root.InverseTransformPoint(vrikManager.references_pelvis.position);
            }
        }

        /// <summary>
        /// Gets the offset between <paramref name="target"/> and the first non-null <paramref name="references"/> in the prefab root transform's space.
        /// </summary>
        /// <param name="target">The target to which the offset should point.</param>
        /// <param name="references">The reference(s) from which to derive the offset.</param>
        /// <returns>The offset between <paramref name="target"/> and the first non-null <paramref name="references"/>.</returns>
        private Pose GetOffset(Transform target, params Transform[] references)
        {
            if (target == null)
            {
                return Pose.identity;
            }

            Transform reference = references.FirstOrDefault(r => r != null);

            if (reference == null)
            {
                _logger.LogError($"No valid reference found for '{target.name}'");
                return Pose.identity;
            }

            return new Pose(
                target.InverseTransformPoint(reference.position),
                Quaternion.Inverse(target.rotation) * reference.rotation);
        }

        private Pose GetCalibrationOffset(Transform target, Pose targetLocalPose, params Transform[] references)
        {
            target.SetPositionAndRotation(targetLocalPose.position, targetLocalPose.rotation);
            return GetOffset(target, references);
        }

        /// <summary>
        /// Gets the pose of the foot target <b>in world space</b>.
        /// </summary>
        /// <param name="centerLocalPosition"></param>
        /// <param name="foot"></param>
        /// <param name="toes"></param>
        /// <returns></returns>
        private Pose GetFootTarget(Vector3 centerLocalPosition, Transform foot, Transform toes)
        {
            if (toes != null)
            {
                Vector3 localFootPosition = foot.position;
                Vector3 localToesPosition = toes.position;

                float a = localFootPosition.x;
                float b = localToesPosition.x;

                // find the point on the vector between the foot and the toes that crosses centerLocalPosition.z
                // centerLocalPosition.z = localFootPosition.z + (localToesPosition.z - localFootPosition.z) * t
                float t = (centerLocalPosition.z - localFootPosition.z) / (localToesPosition.z - localFootPosition.z);

                return new Pose(
                    new Vector3(Mathf.Lerp(a, b, t), 0, centerLocalPosition.z),
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(toes.position - foot.position, transform.up), transform.up));
            }
            else
            {
                return new Pose(
                    new Vector3(centerLocalPosition.x + foot.position.x, 0, centerLocalPosition.z),
                    Quaternion.identity);
            }
        }

        private float GetEyeHeight()
        {
            if (!head)
            {
                _logger.LogWarning("Avatar does not have a head tracking reference");
                return BeatSaberUtilities.kDefaultPlayerEyeHeight;
            }

            if (head.position.y <= 0)
            {
                return BeatSaberUtilities.kDefaultPlayerEyeHeight;
            }

            // many avatars rely on this being global because their root position isn't at (0, 0, 0)
            float eyeHeight = head.position.y;

            _logger.LogTrace($"Measured eye height: {eyeHeight} m");

            return eyeHeight;
        }

        private void FixTrackingReferences(VRIKManager vrikManager)
        {
            FixTrackingReference("Head", head, vrikManager.references_head, vrikManager.solver_spine_headTarget);
            FixTrackingReference("Left Hand", leftHand, vrikManager.references_leftHand, vrikManager.solver_leftArm_target);
            FixTrackingReference("Right Hand", rightHand, vrikManager.references_rightHand, vrikManager.solver_rightArm_target);
            FixTrackingReference("Waist", pelvis, vrikManager.references_pelvis, vrikManager.solver_spine_pelvisTarget);
            FixTrackingReference("Left Foot", leftLeg, UnityUtilities.FirstNonNullUnityObject(vrikManager.references_leftToes, vrikManager.references_leftFoot), vrikManager.solver_leftLeg_target);
            FixTrackingReference("Right Foot", rightLeg, UnityUtilities.FirstNonNullUnityObject(vrikManager.references_rightToes, vrikManager.references_rightFoot), vrikManager.solver_rightLeg_target);
        }

        private void FixTrackingReference(string name, Transform tracker, Transform reference, Transform target)
        {
            if (!reference)
            {
                _logger.LogWarning($"Could not find {name} reference");
                return;
            }

            if (!target)
            {
                // target will be added automatically, no need to adjust
                return;
            }

            Vector3 offset = target.position - reference.position;

            // only warn if offset is larger than 1 mm
            if (offset.magnitude > 0.001f)
            {
                // manually putting each coordinate gives more resolution
                _logger.LogWarning($"{name} bone and target are not at the same position; moving '{tracker.name}' by ({offset.x:0.000}, {offset.y:0.000}, {offset.z:0.000})");
                tracker.position -= offset;
            }
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        private float GetArmSpan(VRIKManager vrikManager)
        {
            if (!vrikManager) return BeatSaberUtilities.kDefaultPlayerArmSpan;

            Transform leftShoulder = vrikManager.references_leftShoulder;
            Transform leftUpperArm = vrikManager.references_leftUpperArm;
            Transform leftLowerArm = vrikManager.references_leftForearm;
            Transform leftWrist = vrikManager.references_leftHand;

            Transform rightShoulder = vrikManager.references_rightShoulder;
            Transform rightUpperArm = vrikManager.references_rightUpperArm;
            Transform rightLowerArm = vrikManager.references_rightForearm;
            Transform rightWrist = vrikManager.references_rightHand;

            if (!leftShoulder || !leftUpperArm || !leftLowerArm || !leftWrist || !rightShoulder || !rightUpperArm || !rightLowerArm || !rightWrist)
            {
                _logger.LogWarning("Could not calculate avatar arm span due to missing bones");
                return BeatSaberUtilities.kDefaultPlayerArmSpan;
            }

            if (!leftHand || !rightHand)
            {
                _logger.LogWarning("Could not calculate avatar arm span due to missing tracking references");
                return BeatSaberUtilities.kDefaultPlayerArmSpan;
            }

            float leftArmLength = Vector3.Distance(leftShoulder.position, leftUpperArm.position) + Vector3.Distance(leftUpperArm.position, leftLowerArm.position) + Vector3.Distance(leftLowerArm.position, leftWrist.position) + Vector3.Distance(leftWrist.position, leftHand.position);
            float rightArmLength = Vector3.Distance(rightShoulder.position, rightUpperArm.position) + Vector3.Distance(rightUpperArm.position, rightLowerArm.position) + Vector3.Distance(rightLowerArm.position, rightWrist.position) + Vector3.Distance(rightWrist.position, rightHand.position);
            float shoulderToShoulderDistance = Vector3.Distance(leftShoulder.position, rightShoulder.position);

            float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;

            _logger.LogTrace($"Measured arm span: {totalLength} m");

            return totalLength;
        }

#pragma warning disable CS0618
        private void ApplyIKManagerFields(VRIKManager vrikManager, IKManager ikManager)
        {
            vrikManager.solver_spine_headTarget = ikManager.HeadTarget;
            vrikManager.solver_leftArm_target = ikManager.LeftHandTarget;
            vrikManager.solver_rightArm_target = ikManager.RightHandTarget;

            if (ikManager is not IKManagerAdvanced ikManagerAdvanced) return;

            vrikManager.solver_spine_pelvisTarget = ikManagerAdvanced.Spine_pelvisTarget;
            vrikManager.solver_spine_pelvisPositionWeight = ikManagerAdvanced.Spine_pelvisPositionWeight;
            vrikManager.solver_spine_pelvisRotationWeight = ikManagerAdvanced.Spine_pelvisRotationWeight;
            vrikManager.solver_spine_positionWeight = ikManagerAdvanced.Head_positionWeight;
            vrikManager.solver_spine_rotationWeight = ikManagerAdvanced.Head_rotationWeight;
            vrikManager.solver_spine_chestGoal = ikManagerAdvanced.Spine_chestGoal;
            vrikManager.solver_spine_chestGoalWeight = ikManagerAdvanced.Spine_chestGoalWeight;
            vrikManager.solver_spine_minHeadHeight = ikManagerAdvanced.Spine_minHeadHeight;
            vrikManager.solver_spine_bodyPosStiffness = ikManagerAdvanced.Spine_bodyPosStiffness;
            vrikManager.solver_spine_bodyRotStiffness = ikManagerAdvanced.Spine_bodyRotStiffness;
            vrikManager.solver_spine_neckStiffness = ikManagerAdvanced.Spine_neckStiffness;
            vrikManager.solver_spine_chestClampWeight = ikManagerAdvanced.Spine_chestClampWeight;
            vrikManager.solver_spine_headClampWeight = ikManagerAdvanced.Spine_headClampWeight;
            vrikManager.solver_spine_maintainPelvisPosition = ikManagerAdvanced.Spine_maintainPelvisPosition;
            vrikManager.solver_spine_maxRootAngle = ikManagerAdvanced.Spine_maxRootAngle;

            vrikManager.solver_leftArm_bendGoal = ikManagerAdvanced.LeftArm_bendGoal;
            vrikManager.solver_leftArm_positionWeight = ikManagerAdvanced.LeftArm_positionWeight;
            vrikManager.solver_leftArm_rotationWeight = ikManagerAdvanced.LeftArm_rotationWeight;
            vrikManager.solver_leftArm_shoulderRotationMode = ikManagerAdvanced.LeftArm_shoulderRotationMode;
            vrikManager.solver_leftArm_shoulderRotationWeight = ikManagerAdvanced.LeftArm_shoulderRotationWeight;
            vrikManager.solver_leftArm_bendGoalWeight = ikManagerAdvanced.LeftArm_bendGoalWeight;
            vrikManager.solver_leftArm_swivelOffset = ikManagerAdvanced.LeftArm_swivelOffset;
            vrikManager.solver_leftArm_wristToPalmAxis = ikManagerAdvanced.LeftArm_wristToPalmAxis;
            vrikManager.solver_leftArm_palmToThumbAxis = ikManagerAdvanced.LeftArm_palmToThumbAxis;

            vrikManager.solver_rightArm_bendGoal = ikManagerAdvanced.RightArm_bendGoal;
            vrikManager.solver_rightArm_positionWeight = ikManagerAdvanced.RightArm_positionWeight;
            vrikManager.solver_rightArm_rotationWeight = ikManagerAdvanced.RightArm_rotationWeight;
            vrikManager.solver_rightArm_shoulderRotationMode = ikManagerAdvanced.RightArm_shoulderRotationMode;
            vrikManager.solver_rightArm_shoulderRotationWeight = ikManagerAdvanced.RightArm_shoulderRotationWeight;
            vrikManager.solver_rightArm_bendGoalWeight = ikManagerAdvanced.RightArm_bendGoalWeight;
            vrikManager.solver_rightArm_swivelOffset = ikManagerAdvanced.RightArm_swivelOffset;
            vrikManager.solver_rightArm_wristToPalmAxis = ikManagerAdvanced.RightArm_wristToPalmAxis;
            vrikManager.solver_rightArm_palmToThumbAxis = ikManagerAdvanced.RightArm_palmToThumbAxis;

            vrikManager.solver_leftLeg_target = ikManagerAdvanced.LeftLeg_target;
            vrikManager.solver_leftLeg_positionWeight = ikManagerAdvanced.LeftLeg_positionWeight;
            vrikManager.solver_leftLeg_rotationWeight = ikManagerAdvanced.LeftLeg_rotationWeight;
            vrikManager.solver_leftLeg_bendGoal = ikManagerAdvanced.LeftLeg_bendGoal;
            vrikManager.solver_leftLeg_bendGoalWeight = ikManagerAdvanced.LeftLeg_bendGoalWeight;
            vrikManager.solver_leftLeg_swivelOffset = ikManagerAdvanced.LeftLeg_swivelOffset;

            vrikManager.solver_rightLeg_target = ikManagerAdvanced.RightLeg_target;
            vrikManager.solver_rightLeg_positionWeight = ikManagerAdvanced.RightLeg_positionWeight;
            vrikManager.solver_rightLeg_rotationWeight = ikManagerAdvanced.RightLeg_rotationWeight;
            vrikManager.solver_rightLeg_bendGoal = ikManagerAdvanced.RightLeg_bendGoal;
            vrikManager.solver_rightLeg_bendGoalWeight = ikManagerAdvanced.RightLeg_bendGoalWeight;
            vrikManager.solver_rightLeg_swivelOffset = ikManagerAdvanced.RightLeg_swivelOffset;

            vrikManager.solver_locomotion_weight = ikManagerAdvanced.Locomotion_weight;
            vrikManager.solver_locomotion_footDistance = ikManagerAdvanced.Locomotion_footDistance;
            vrikManager.solver_locomotion_stepThreshold = ikManagerAdvanced.Locomotion_stepThreshold;
            vrikManager.solver_locomotion_angleThreshold = ikManagerAdvanced.Locomotion_angleThreshold;
            vrikManager.solver_locomotion_comAngleMlp = ikManagerAdvanced.Locomotion_comAngleMlp;
            vrikManager.solver_locomotion_maxVelocity = ikManagerAdvanced.Locomotion_maxVelocity;
            vrikManager.solver_locomotion_velocityFactor = ikManagerAdvanced.Locomotion_velocityFactor;
            vrikManager.solver_locomotion_maxLegStretch = ikManagerAdvanced.Locomotion_maxLegStretch;
            vrikManager.solver_locomotion_rootSpeed = ikManagerAdvanced.Locomotion_rootSpeed;
            vrikManager.solver_locomotion_stepSpeed = ikManagerAdvanced.Locomotion_stepSpeed;
            vrikManager.solver_locomotion_stepHeight = ikManagerAdvanced.Locomotion_stepHeight;
            vrikManager.solver_locomotion_heelHeight = ikManagerAdvanced.Locomotion_heelHeight;
            vrikManager.solver_locomotion_relaxLegTwistMinAngle = ikManagerAdvanced.Locomotion_relaxLegTwistMinAngle;
            vrikManager.solver_locomotion_relaxLegTwistSpeed = ikManagerAdvanced.Locomotion_relaxLegTwistSpeed;
            vrikManager.solver_locomotion_stepInterpolation = ikManagerAdvanced.Locomotion_stepInterpolation;
            vrikManager.solver_locomotion_offset = ikManagerAdvanced.Locomotion_offset;
            vrikManager.solver_locomotion_onLeftFootstep = ikManagerAdvanced.Locomotion_onLeftFootstep;
            vrikManager.solver_locomotion_onRightFootstep = ikManagerAdvanced.Locomotion_onRightFootstep;
        }
#pragma warning restore CS0618
    }
}
