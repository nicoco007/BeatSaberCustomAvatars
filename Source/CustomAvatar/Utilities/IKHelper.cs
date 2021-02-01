extern alias BeatSaberFinalIK;

using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class IKHelper
    {
        private ILogger<IKHelper> _logger;

        public IKHelper(ILogger<IKHelper> logger)
        {
            _logger = logger;
        }

        public void CreateOffsetTargetsIfMissing(VRIKManager vrikManager, Transform root)
        {
            CreateOffsetTargetIfMissing(root, "Pelvis",   vrikManager.references_pelvis,                                        ref vrikManager.solver_spine_pelvisPositionWeight, ref vrikManager.solver_spine_pelvisRotationWeight);
            CreateOffsetTargetIfMissing(root, "LeftLeg",  vrikManager.references_leftToes  ?? vrikManager.references_leftFoot,  ref vrikManager.solver_leftLeg_positionWeight,     ref vrikManager.solver_leftLeg_rotationWeight);
            CreateOffsetTargetIfMissing(root, "RightLeg", vrikManager.references_rightToes ?? vrikManager.references_rightFoot, ref vrikManager.solver_rightLeg_positionWeight,    ref vrikManager.solver_rightLeg_rotationWeight);
        }

        public VRIK InitializeVRIK(VRIKManager vrikManager, Transform root)
        {
            VRIK vrik = vrikManager.gameObject.AddComponent<VRIK>();

            CreateTargetsIfMissing(vrikManager, root);
            CopyManagerFieldsToVRIK(vrikManager, vrik);

            return vrik;
        }

        private void CreateOffsetTargetIfMissing(Transform root, string name, Transform reference, ref float positionWeight, ref float rotationWeight)
        {
            if (root.Find(name)) return;

            Transform offsetTarget = new GameObject(name).transform;

            offsetTarget.SetParent(root, false);
            offsetTarget.SetPositionAndRotation(reference.position, reference.rotation);

            positionWeight = 1;
            rotationWeight = 1;

            _logger.Trace($"Created offset IK target for '{name}'");
        }
        
        private void CreateTargetsIfMissing(VRIKManager vrikManager, Transform root)
        {
            vrikManager.solver_spine_headTarget   = CreateTargetIfMissing(vrikManager.solver_spine_headTarget,   vrikManager.references_head,                                          root.Find("Head"));
            vrikManager.solver_leftArm_target     = CreateTargetIfMissing(vrikManager.solver_leftArm_target,     vrikManager.references_leftHand,                                      root.Find("LeftHand"));
            vrikManager.solver_rightArm_target    = CreateTargetIfMissing(vrikManager.solver_rightArm_target,    vrikManager.references_rightHand,                                     root.Find("RightHand"));
            vrikManager.solver_spine_pelvisTarget = CreateTargetIfMissing(vrikManager.solver_spine_pelvisTarget, vrikManager.references_pelvis,                                        root.Find("Pelvis"));
            vrikManager.solver_leftLeg_target     = CreateTargetIfMissing(vrikManager.solver_leftLeg_target,     vrikManager.references_leftToes  ?? vrikManager.references_leftFoot,  root.Find("LeftLeg"));
            vrikManager.solver_rightLeg_target    = CreateTargetIfMissing(vrikManager.solver_rightLeg_target,    vrikManager.references_rightToes ?? vrikManager.references_rightFoot, root.Find("RightLeg"));
        }

        private Transform CreateTargetIfMissing(Transform target, Transform reference, Transform parent)
        {
            if (target || !parent) return target;

            Transform newTarget = new GameObject($"{parent.name} Target").transform;

            newTarget.SetParent(parent, false);
            newTarget.position = reference.position;
            newTarget.rotation = reference.rotation;

            _logger.Trace($"Created IK target for '{parent.name}'");

            return newTarget;
        }

        private void CopyManagerFieldsToVRIK(VRIKManager vrikManager, VRIK vrik)
        {
            vrik.references.root          = vrikManager.references_root;
            vrik.references.pelvis        = vrikManager.references_pelvis;
            vrik.references.spine         = vrikManager.references_spine;
            vrik.references.chest         = vrikManager.references_chest;
            vrik.references.neck          = vrikManager.references_neck;
            vrik.references.head          = vrikManager.references_head;
            vrik.references.leftShoulder  = vrikManager.references_leftShoulder;
            vrik.references.leftUpperArm  = vrikManager.references_leftUpperArm;
            vrik.references.leftForearm   = vrikManager.references_leftForearm;
            vrik.references.leftHand      = vrikManager.references_leftHand;
            vrik.references.rightShoulder = vrikManager.references_rightShoulder;
            vrik.references.rightUpperArm = vrikManager.references_rightUpperArm;
            vrik.references.rightForearm  = vrikManager.references_rightForearm;
            vrik.references.rightHand     = vrikManager.references_rightHand;
            vrik.references.leftThigh     = vrikManager.references_leftThigh;
            vrik.references.leftCalf      = vrikManager.references_leftCalf;
            vrik.references.leftFoot      = vrikManager.references_leftFoot;
            vrik.references.leftToes      = vrikManager.references_leftToes;
            vrik.references.rightThigh    = vrikManager.references_rightThigh;
            vrik.references.rightCalf     = vrikManager.references_rightCalf;
            vrik.references.rightFoot     = vrikManager.references_rightFoot;
            vrik.references.rightToes     = vrikManager.references_rightToes;

            vrik.solver.plantFeet = vrikManager.solver_plantFeet;
            
            vrik.solver.spine.headTarget                = vrikManager.solver_spine_headTarget;
            vrik.solver.spine.positionWeight            = vrikManager.solver_spine_positionWeight;
            vrik.solver.spine.rotationWeight            = vrikManager.solver_spine_rotationWeight;
            vrik.solver.spine.pelvisTarget              = vrikManager.solver_spine_pelvisTarget;
            vrik.solver.spine.pelvisPositionWeight      = vrikManager.solver_spine_pelvisPositionWeight;
            vrik.solver.spine.pelvisRotationWeight      = vrikManager.solver_spine_pelvisRotationWeight;
            vrik.solver.spine.chestGoal                 = vrikManager.solver_spine_chestGoal;
            vrik.solver.spine.chestGoalWeight           = vrikManager.solver_spine_chestGoalWeight;
            vrik.solver.spine.minHeadHeight             = vrikManager.solver_spine_minHeadHeight;
            vrik.solver.spine.bodyPosStiffness          = vrikManager.solver_spine_bodyPosStiffness;
            vrik.solver.spine.bodyRotStiffness          = vrikManager.solver_spine_bodyRotStiffness;
            vrik.solver.spine.neckStiffness             = vrikManager.solver_spine_neckStiffness;
            vrik.solver.spine.rotateChestByHands        = vrikManager.solver_spine_rotateChestByHands;
            vrik.solver.spine.chestClampWeight          = vrikManager.solver_spine_chestClampWeight;
            vrik.solver.spine.headClampWeight           = vrikManager.solver_spine_headClampWeight;
            vrik.solver.spine.moveBodyBackWhenCrouching = vrikManager.solver_spine_moveBodyBackWhenCrouching;
            vrik.solver.spine.maintainPelvisPosition    = vrikManager.solver_spine_maintainPelvisPosition;
            vrik.solver.spine.maxRootAngle              = vrikManager.solver_spine_maxRootAngle;
            
            vrik.solver.leftArm.target                 = vrikManager.solver_leftArm_target;
            vrik.solver.leftArm.bendGoal               = vrikManager.solver_leftArm_bendGoal;
            vrik.solver.leftArm.positionWeight         = vrikManager.solver_leftArm_positionWeight;
            vrik.solver.leftArm.rotationWeight         = vrikManager.solver_leftArm_rotationWeight;
            vrik.solver.leftArm.shoulderRotationMode   = vrikManager.solver_leftArm_shoulderRotationMode;
            vrik.solver.leftArm.shoulderRotationWeight = vrikManager.solver_leftArm_shoulderRotationWeight;
            vrik.solver.leftArm.shoulderTwistWeight    = vrikManager.solver_leftArm_shoulderTwistWeight;
            vrik.solver.leftArm.bendGoalWeight         = vrikManager.solver_leftArm_bendGoalWeight;
            vrik.solver.leftArm.swivelOffset           = vrikManager.solver_leftArm_swivelOffset;
            vrik.solver.leftArm.wristToPalmAxis        = vrikManager.solver_leftArm_wristToPalmAxis;
            vrik.solver.leftArm.palmToThumbAxis        = vrikManager.solver_leftArm_palmToThumbAxis;
            vrik.solver.leftArm.armLengthMlp           = vrikManager.solver_leftArm_armLengthMlp;
            vrik.solver.leftArm.stretchCurve           = vrikManager.solver_leftArm_stretchCurve;
            
            vrik.solver.rightArm.target                 = vrikManager.solver_rightArm_target;
            vrik.solver.rightArm.bendGoal               = vrikManager.solver_rightArm_bendGoal;
            vrik.solver.rightArm.positionWeight         = vrikManager.solver_rightArm_positionWeight;
            vrik.solver.rightArm.rotationWeight         = vrikManager.solver_rightArm_rotationWeight;
            vrik.solver.rightArm.shoulderRotationMode   = vrikManager.solver_rightArm_shoulderRotationMode;
            vrik.solver.rightArm.shoulderRotationWeight = vrikManager.solver_rightArm_shoulderRotationWeight;
            vrik.solver.rightArm.shoulderTwistWeight    = vrikManager.solver_rightArm_shoulderTwistWeight;
            vrik.solver.rightArm.bendGoalWeight         = vrikManager.solver_rightArm_bendGoalWeight;
            vrik.solver.rightArm.swivelOffset           = vrikManager.solver_rightArm_swivelOffset;
            vrik.solver.rightArm.wristToPalmAxis        = vrikManager.solver_rightArm_wristToPalmAxis;
            vrik.solver.rightArm.palmToThumbAxis        = vrikManager.solver_rightArm_palmToThumbAxis;
            vrik.solver.rightArm.armLengthMlp           = vrikManager.solver_rightArm_armLengthMlp;
            vrik.solver.rightArm.stretchCurve           = vrikManager.solver_rightArm_stretchCurve;
            
            vrik.solver.leftLeg.target             = vrikManager.solver_leftLeg_target;
            vrik.solver.leftLeg.bendGoal           = vrikManager.solver_leftLeg_bendGoal;
            vrik.solver.leftLeg.positionWeight     = vrikManager.solver_leftLeg_positionWeight;
            vrik.solver.leftLeg.rotationWeight     = vrikManager.solver_leftLeg_rotationWeight;
            vrik.solver.leftLeg.bendGoalWeight     = vrikManager.solver_leftLeg_bendGoalWeight;
            vrik.solver.leftLeg.swivelOffset       = vrikManager.solver_leftLeg_swivelOffset;
            vrik.solver.leftLeg.bendToTargetWeight = vrikManager.solver_leftLeg_bendToTargetWeight;
            vrik.solver.leftLeg.legLengthMlp       = vrikManager.solver_leftLeg_legLengthMlp;
            vrik.solver.leftLeg.stretchCurve       = vrikManager.solver_leftLeg_stretchCurve;
            
            vrik.solver.rightLeg.target             = vrikManager.solver_rightLeg_target;
            vrik.solver.rightLeg.bendGoal           = vrikManager.solver_rightLeg_bendGoal;
            vrik.solver.rightLeg.positionWeight     = vrikManager.solver_rightLeg_positionWeight;
            vrik.solver.rightLeg.rotationWeight     = vrikManager.solver_rightLeg_rotationWeight;
            vrik.solver.rightLeg.bendGoalWeight     = vrikManager.solver_rightLeg_bendGoalWeight;
            vrik.solver.rightLeg.swivelOffset       = vrikManager.solver_rightLeg_swivelOffset;
            vrik.solver.rightLeg.bendToTargetWeight = vrikManager.solver_rightLeg_bendToTargetWeight;
            vrik.solver.rightLeg.legLengthMlp       = vrikManager.solver_rightLeg_legLengthMlp;
            vrik.solver.rightLeg.stretchCurve       = vrikManager.solver_rightLeg_stretchCurve;
            
            vrik.solver.locomotion.weight                = vrikManager.solver_locomotion_weight;
            vrik.solver.locomotion.footDistance          = vrikManager.solver_locomotion_footDistance;
            vrik.solver.locomotion.stepThreshold         = vrikManager.solver_locomotion_stepThreshold;
            vrik.solver.locomotion.angleThreshold        = vrikManager.solver_locomotion_angleThreshold;
            vrik.solver.locomotion.comAngleMlp           = vrikManager.solver_locomotion_comAngleMlp;
            vrik.solver.locomotion.maxVelocity           = vrikManager.solver_locomotion_maxVelocity;
            vrik.solver.locomotion.velocityFactor        = vrikManager.solver_locomotion_velocityFactor;
            vrik.solver.locomotion.maxLegStretch         = vrikManager.solver_locomotion_maxLegStretch;
            vrik.solver.locomotion.rootSpeed             = vrikManager.solver_locomotion_rootSpeed;
            vrik.solver.locomotion.stepSpeed             = vrikManager.solver_locomotion_stepSpeed;
            vrik.solver.locomotion.stepHeight            = vrikManager.solver_locomotion_stepHeight;
            vrik.solver.locomotion.heelHeight            = vrikManager.solver_locomotion_heelHeight;
            vrik.solver.locomotion.relaxLegTwistMinAngle = vrikManager.solver_locomotion_relaxLegTwistMinAngle;
            vrik.solver.locomotion.relaxLegTwistSpeed    = vrikManager.solver_locomotion_relaxLegTwistSpeed;
            vrik.solver.locomotion.stepInterpolation     = vrikManager.solver_locomotion_stepInterpolation;
            vrik.solver.locomotion.offset                = vrikManager.solver_locomotion_offset;
            vrik.solver.locomotion.onLeftFootstep        = vrikManager.solver_locomotion_onLeftFootstep;
            vrik.solver.locomotion.onRightFootstep       = vrikManager.solver_locomotion_onRightFootstep;
        }
    }
}
