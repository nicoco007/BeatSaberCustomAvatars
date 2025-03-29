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

extern alias BeatSaberDynamicBone;
extern alias BeatSaberFinalIK;

using System.Collections.Generic;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using CustomAvatar.Scripts;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
    public class AvatarIK : VRIK
    {
        public bool isLocomotionEnabled
        {
            get => _isLocomotionEnabled;
            set
            {
                _isLocomotionEnabled = value;
                UpdateLocomotion();
            }
        }

        internal VRIKManager vrikManager { get; private set; }

        private readonly List<BeatSaberDynamicBone::DynamicBone> _dynamicBones = new();
        private TwistRelaxer[] _twistRelaxers;
        private TwistRelaxerV2[] _twistRelaxersV2;

        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private ILogger<AvatarIK> _logger;

        private bool _isLocomotionEnabled = false;
        private Pose _defaultRootPose;
        private Pose _previousParentPose;

        private bool _hasPelvisTarget;
        private bool _hasBothLegTargets;

        public AvatarIK() : base()
        {
            solver = new CustomIKSolverVR();
        }

        #region Behaviour Lifecycle

        protected void Awake()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>())
            {
                if (!dynamicBone.enabled)
                {
                    continue;
                }

                dynamicBone.enabled = false;

                _dynamicBones.Add(dynamicBone);
            }

            _twistRelaxers = GetComponentsInChildren<TwistRelaxer>();
            _twistRelaxersV2 = GetComponentsInChildren<TwistRelaxerV2>();

            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                twistRelaxer.ik = this;
            }

            foreach (TwistRelaxerV2 twistRelaxer in _twistRelaxersV2)
            {
                twistRelaxer.ik = this;
            }

            vrikManager = GetComponentInChildren<VRIKManager>();
            _defaultRootPose = new Pose(vrikManager.references_root.localPosition, vrikManager.references_root.localRotation);
            CopyManagerFieldsToVRIK(vrikManager, this);
        }

        protected void OnEnable()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnEnable();
            }
        }

        protected new void Start()
        {
            solver.OnPreUpdate += OnPreUpdate;
            solver.OnPostUpdate += OnPostUpdate;

            _input.inputChanged += OnInputChanged;

            UpdateSolverTargets();

            base.Start();

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnEnable();
                dynamicBone.Start();
            }
        }

        protected new void OnDisable()
        {
            references.root.SetLocalPose(_defaultRootPose);
            solver.FixTransforms();

            foreach (TwistRelaxerV2 twistRelaxer in _twistRelaxersV2)
            {
                twistRelaxer.FixTransforms();
            }

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnDisable();
            }

            base.OnDisable();
        }

        protected void OnDestroy()
        {
            solver.OnPreUpdate -= OnPreUpdate;
            solver.OnPostUpdate -= OnPostUpdate;

            _input.inputChanged -= OnInputChanged;
        }

        #endregion

        [Inject]
        [UsedImplicitly]
        private void Construct(IAvatarInput input, SpawnedAvatar avatar, ILoggerFactory loggerFactory)
        {
            _input = input;
            _avatar = avatar;
            _logger = loggerFactory.CreateLogger<AvatarIK>(_avatar.prefab.descriptor.name);
        }

        private void OnPreUpdate()
        {
            ApplyRootPose();

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.Update();
            }

            ApplyPlatformMotion();
        }

        private void OnPostUpdate()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.LateUpdate();
            }
        }

        // adapted from VRIKRootController to work when IK is rotated by parent
        private void ApplyRootPose()
        {
            // don't move the root if locomotion is disabled and both feet aren't being tracked
            // (i.e. keep previous behaviour of sticking to the origin when locomotion is disabled and we're missing one or more FBT trackers)
            if (!_isLocomotionEnabled && !_hasBothLegTargets)
            {
                return;
            }

            if (!_hasPelvisTarget)
            {
                return;
            }

            Transform pelvisTarget = solver.spine.pelvisTarget;
            Transform root = references.root;
            Transform parent = root.parent;
            bool hasParent = parent != null;

            Vector3 up = hasParent ? parent.up : Vector3.up;
            root.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(pelvisTarget.rotation * _avatar.prefab.pelvisRootForward, up), up);

            var position = Vector3.ProjectOnPlane(pelvisTarget.position - root.TransformVector(_avatar.prefab.pelvisRootOffset), up);

            if (hasParent)
            {
                position = parent.InverseTransformPoint(position);
            }

            root.localPosition = new Vector3(position.x, root.localPosition.y, position.z);
        }

        private void ApplyPlatformMotion()
        {
            Transform parent = references.root.parent;

            if (parent == null)
            {
                return;
            }

            parent.GetPositionAndRotation(out Vector3 parentPosition, out Quaternion parentRotation);

            if (_previousParentPose.position == parentPosition && _previousParentPose.rotation == parentRotation)
            {
                return;
            }

            Vector3 deltaPosition = parentPosition - _previousParentPose.position;
            Quaternion deltaRotation = Quaternion.Inverse(_previousParentPose.rotation) * parentRotation;

            solver.AddPlatformMotion(deltaPosition, deltaRotation, parentPosition);

            _previousParentPose = new Pose(parentPosition, parentRotation);
        }

        private void UpdateLocomotion()
        {
            // don't enable locomotion if FBT is applied
            bool shouldEnableLocomotion = _isLocomotionEnabled && !(_hasPelvisTarget && _hasBothLegTargets);

            if (shouldEnableLocomotion)
            {
                solver.locomotion.weight = 1;
            }
            else
            {
                solver.locomotion.weight = 0;
                references.root.SetLocalPose(_defaultRootPose);
            }
        }

        private void OnInputChanged()
        {
            UpdateSolverTargets();
        }

        private void UpdateSolverTargets()
        {
            _logger.LogTrace("Updating solver targets");

            UpdateSolverTarget(DeviceUse.Head, ref solver.spine.headTarget, ref solver.spine.positionWeight, ref solver.spine.rotationWeight);
            UpdateSolverTarget(DeviceUse.LeftHand, ref solver.leftArm.target, ref solver.leftArm.positionWeight, ref solver.leftArm.rotationWeight);
            UpdateSolverTarget(DeviceUse.RightHand, ref solver.rightArm.target, ref solver.rightArm.positionWeight, ref solver.rightArm.rotationWeight);

            _hasBothLegTargets = true;
            _hasBothLegTargets &= UpdateSolverTarget(DeviceUse.LeftFoot, ref solver.leftLeg.target, ref solver.leftLeg.positionWeight, ref solver.leftLeg.rotationWeight);
            _hasBothLegTargets &= UpdateSolverTarget(DeviceUse.RightFoot, ref solver.rightLeg.target, ref solver.rightLeg.positionWeight, ref solver.rightLeg.rotationWeight);

            if (_hasPelvisTarget = UpdateSolverTarget(DeviceUse.Waist, ref solver.spine.pelvisTarget, ref solver.spine.pelvisPositionWeight, ref solver.spine.pelvisRotationWeight))
            {
                solver.plantFeet = false;
                solver.spine.maintainPelvisPosition = 0;
            }
            else
            {
                solver.plantFeet = vrikManager.solver_plantFeet;
                solver.spine.maintainPelvisPosition = vrikManager.solver_spine_maintainPelvisPosition;
            }

            UpdateLocomotion();
        }

        private bool UpdateSolverTarget(DeviceUse deviceUse, ref Transform target, ref float positionWeight, ref float rotationWeight)
        {
            if (_input.TryGetTransform(deviceUse, out Transform transform))
            {
                target = transform;
                positionWeight = 1;
                rotationWeight = 1;
                return true;
            }
            else
            {
                target = null;
                positionWeight = 0;
                rotationWeight = 0;
                return false;
            }
        }

        private static void CopyManagerFieldsToVRIK(VRIKManager vrikManager, VRIK vrik)
        {
            vrik.references.root = vrikManager.references_root;
            vrik.references.pelvis = vrikManager.references_pelvis;
            vrik.references.spine = vrikManager.references_spine;
            vrik.references.chest = vrikManager.references_chest;
            vrik.references.neck = vrikManager.references_neck;
            vrik.references.head = vrikManager.references_head;
            vrik.references.leftShoulder = vrikManager.references_leftShoulder;
            vrik.references.leftUpperArm = vrikManager.references_leftUpperArm;
            vrik.references.leftForearm = vrikManager.references_leftForearm;
            vrik.references.leftHand = vrikManager.references_leftHand;
            vrik.references.rightShoulder = vrikManager.references_rightShoulder;
            vrik.references.rightUpperArm = vrikManager.references_rightUpperArm;
            vrik.references.rightForearm = vrikManager.references_rightForearm;
            vrik.references.rightHand = vrikManager.references_rightHand;
            vrik.references.leftThigh = vrikManager.references_leftThigh;
            vrik.references.leftCalf = vrikManager.references_leftCalf;
            vrik.references.leftFoot = vrikManager.references_leftFoot;
            vrik.references.leftToes = vrikManager.references_leftToes;
            vrik.references.rightThigh = vrikManager.references_rightThigh;
            vrik.references.rightCalf = vrikManager.references_rightCalf;
            vrik.references.rightFoot = vrikManager.references_rightFoot;
            vrik.references.rightToes = vrikManager.references_rightToes;

            vrik.solver.plantFeet = vrikManager.solver_plantFeet;
            vrik.solver.spine.headTarget = vrikManager.solver_spine_headTarget;
            vrik.solver.spine.positionWeight = vrikManager.solver_spine_positionWeight;
            vrik.solver.spine.rotationWeight = vrikManager.solver_spine_rotationWeight;
            vrik.solver.spine.pelvisTarget = vrikManager.solver_spine_pelvisTarget;
            vrik.solver.spine.pelvisPositionWeight = vrikManager.solver_spine_pelvisPositionWeight;
            vrik.solver.spine.pelvisRotationWeight = vrikManager.solver_spine_pelvisRotationWeight;
            vrik.solver.spine.chestGoal = vrikManager.solver_spine_chestGoal;
            vrik.solver.spine.chestGoalWeight = vrikManager.solver_spine_chestGoalWeight;
            vrik.solver.spine.minHeadHeight = vrikManager.solver_spine_minHeadHeight;
            vrik.solver.spine.bodyPosStiffness = vrikManager.solver_spine_bodyPosStiffness;
            vrik.solver.spine.bodyRotStiffness = vrikManager.solver_spine_bodyRotStiffness;
            vrik.solver.spine.neckStiffness = vrikManager.solver_spine_neckStiffness;
            vrik.solver.spine.rotateChestByHands = vrikManager.solver_spine_rotateChestByHands;
            vrik.solver.spine.chestClampWeight = vrikManager.solver_spine_chestClampWeight;
            vrik.solver.spine.headClampWeight = vrikManager.solver_spine_headClampWeight;
            vrik.solver.spine.moveBodyBackWhenCrouching = vrikManager.solver_spine_moveBodyBackWhenCrouching;
            vrik.solver.spine.maintainPelvisPosition = vrikManager.solver_spine_maintainPelvisPosition;
            vrik.solver.spine.maxRootAngle = vrikManager.solver_spine_maxRootAngle;

            var leftArm = (CustomIKSolverVR.CustomArm)vrik.solver.leftArm;
            leftArm.shoulderPitchOffset = CalculatePitchOffset(true, vrik.references.root, vrik.references.leftShoulder, vrik.references.leftUpperArm);
            leftArm.target = vrikManager.solver_leftArm_target;
            leftArm.bendGoal = vrikManager.solver_leftArm_bendGoal;
            leftArm.positionWeight = vrikManager.solver_leftArm_positionWeight;
            leftArm.rotationWeight = vrikManager.solver_leftArm_rotationWeight;
            leftArm.shoulderRotationMode = vrikManager.solver_leftArm_shoulderRotationMode;
            leftArm.shoulderRotationWeight = vrikManager.solver_leftArm_shoulderRotationWeight;
            leftArm.shoulderTwistWeight = vrikManager.solver_leftArm_shoulderTwistWeight;
            leftArm.bendGoalWeight = vrikManager.solver_leftArm_bendGoalWeight;
            leftArm.swivelOffset = vrikManager.solver_leftArm_swivelOffset;
            leftArm.wristToPalmAxis = vrikManager.solver_leftArm_wristToPalmAxis;
            leftArm.palmToThumbAxis = vrikManager.solver_leftArm_palmToThumbAxis;
            leftArm.armLengthMlp = vrikManager.solver_leftArm_armLengthMlp;
            leftArm.stretchCurve = vrikManager.solver_leftArm_stretchCurve;

            var rightArm = (CustomIKSolverVR.CustomArm)vrik.solver.rightArm;
            rightArm.shoulderPitchOffset = CalculatePitchOffset(false, vrik.references.root, vrik.references.rightShoulder, vrik.references.rightUpperArm);
            rightArm.target = vrikManager.solver_rightArm_target;
            rightArm.bendGoal = vrikManager.solver_rightArm_bendGoal;
            rightArm.positionWeight = vrikManager.solver_rightArm_positionWeight;
            rightArm.rotationWeight = vrikManager.solver_rightArm_rotationWeight;
            rightArm.shoulderRotationMode = vrikManager.solver_rightArm_shoulderRotationMode;
            rightArm.shoulderRotationWeight = vrikManager.solver_rightArm_shoulderRotationWeight;
            rightArm.shoulderTwistWeight = vrikManager.solver_rightArm_shoulderTwistWeight;
            rightArm.bendGoalWeight = vrikManager.solver_rightArm_bendGoalWeight;
            rightArm.swivelOffset = vrikManager.solver_rightArm_swivelOffset;
            rightArm.wristToPalmAxis = vrikManager.solver_rightArm_wristToPalmAxis;
            rightArm.palmToThumbAxis = vrikManager.solver_rightArm_palmToThumbAxis;
            rightArm.armLengthMlp = vrikManager.solver_rightArm_armLengthMlp;
            rightArm.stretchCurve = vrikManager.solver_rightArm_stretchCurve;

            vrik.solver.leftLeg.target = vrikManager.solver_leftLeg_target;
            vrik.solver.leftLeg.bendGoal = vrikManager.solver_leftLeg_bendGoal;
            vrik.solver.leftLeg.positionWeight = vrikManager.solver_leftLeg_positionWeight;
            vrik.solver.leftLeg.rotationWeight = vrikManager.solver_leftLeg_rotationWeight;
            vrik.solver.leftLeg.bendGoalWeight = vrikManager.solver_leftLeg_bendGoalWeight;
            vrik.solver.leftLeg.swivelOffset = vrikManager.solver_leftLeg_swivelOffset;
            vrik.solver.leftLeg.bendToTargetWeight = vrikManager.solver_leftLeg_bendToTargetWeight;
            vrik.solver.leftLeg.legLengthMlp = vrikManager.solver_leftLeg_legLengthMlp;
            vrik.solver.leftLeg.stretchCurve = vrikManager.solver_leftLeg_stretchCurve;
            vrik.solver.rightLeg.target = vrikManager.solver_rightLeg_target;
            vrik.solver.rightLeg.bendGoal = vrikManager.solver_rightLeg_bendGoal;
            vrik.solver.rightLeg.positionWeight = vrikManager.solver_rightLeg_positionWeight;
            vrik.solver.rightLeg.rotationWeight = vrikManager.solver_rightLeg_rotationWeight;
            vrik.solver.rightLeg.bendGoalWeight = vrikManager.solver_rightLeg_bendGoalWeight;
            vrik.solver.rightLeg.swivelOffset = vrikManager.solver_rightLeg_swivelOffset;
            vrik.solver.rightLeg.bendToTargetWeight = vrikManager.solver_rightLeg_bendToTargetWeight;
            vrik.solver.rightLeg.legLengthMlp = vrikManager.solver_rightLeg_legLengthMlp;
            vrik.solver.rightLeg.stretchCurve = vrikManager.solver_rightLeg_stretchCurve;
            vrik.solver.locomotion.weight = vrikManager.solver_locomotion_weight;
            vrik.solver.locomotion.footDistance = vrikManager.solver_locomotion_footDistance;
            vrik.solver.locomotion.stepThreshold = vrikManager.solver_locomotion_stepThreshold;
            vrik.solver.locomotion.angleThreshold = vrikManager.solver_locomotion_angleThreshold;
            vrik.solver.locomotion.comAngleMlp = vrikManager.solver_locomotion_comAngleMlp;
            vrik.solver.locomotion.maxVelocity = vrikManager.solver_locomotion_maxVelocity;
            vrik.solver.locomotion.velocityFactor = vrikManager.solver_locomotion_velocityFactor;
            vrik.solver.locomotion.maxLegStretch = vrikManager.solver_locomotion_maxLegStretch;
            vrik.solver.locomotion.rootSpeed = vrikManager.solver_locomotion_rootSpeed;
            vrik.solver.locomotion.stepSpeed = vrikManager.solver_locomotion_stepSpeed;
            vrik.solver.locomotion.stepHeight = vrikManager.solver_locomotion_stepHeight;
            vrik.solver.locomotion.heelHeight = vrikManager.solver_locomotion_heelHeight;
            vrik.solver.locomotion.relaxLegTwistMinAngle = vrikManager.solver_locomotion_relaxLegTwistMinAngle;
            vrik.solver.locomotion.relaxLegTwistSpeed = vrikManager.solver_locomotion_relaxLegTwistSpeed;
            vrik.solver.locomotion.stepInterpolation = vrikManager.solver_locomotion_stepInterpolation;
            vrik.solver.locomotion.offset = vrikManager.solver_locomotion_offset;
            vrik.solver.locomotion.onLeftFootstep = vrikManager.solver_locomotion_onLeftFootstep;
            vrik.solver.locomotion.onRightFootstep = vrikManager.solver_locomotion_onRightFootstep;
        }

        private static float CalculatePitchOffset(bool isLeft, Transform root, Transform shoulder, Transform upperArm)
        {
            if (root == null || shoulder == null || upperArm == null)
            {
                return -30f;
            }

            // Reading IKSolverVR.Arm's Shoulder Pitch section, it looks like:
            // - chestForward is essentially just the root forward
            // - pitch is always (90 degrees to either side of chestUp) * chestRotation, so effectively left and right of the chest.
            Vector3 forward = root.forward;
            Vector3 right = root.right;

            return Vector3.SignedAngle(isLeft ? -right : right, upperArm.position - shoulder.position, isLeft ? forward : -forward) - 45f;
        }
    }
}
