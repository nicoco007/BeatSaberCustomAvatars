extern alias BeatSaberFinalIK;

using System;
using System.Reflection;
using BeatSaberFinalIK::RootMotion;
using CustomAvatar.Logging;
using UnityEngine;
using UnityEngine.Events;
using static BeatSaberFinalIK::RootMotion.FinalIK.IKSolverVR.Arm;
using ILogger = CustomAvatar.Logging.ILogger;
using VRIK = BeatSaberFinalIK::RootMotion.FinalIK.VRIK;

#if !UNITY_EDITOR
using Zenject;
#endif

// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
namespace CustomAvatar
{
    [Serializable]
    public class VRIKManager : MonoBehaviour
    {
        [Tooltip("If true, will fix all the Transforms used by the solver to their initial state in each Update. This prevents potential problems with unanimated bones and animator culling with a small cost of performance. Not recommended for CCD and FABRIK solvers.")]
        public bool fixTransforms = true;

        #region References
        
        [Header("References")]

        public Transform references_root;
        public Transform references_pelvis;
        public Transform references_spine;

        [Tooltip("Optional")]
        public Transform references_chest;

        [Tooltip("Optional")]
        public Transform references_neck;
        public Transform references_head;

        [Tooltip("Optional")]
        public Transform references_leftShoulder;
        public Transform references_leftUpperArm;
        public Transform references_leftForearm;
        public Transform references_leftHand;

        [Tooltip("Optional")]
        public Transform references_rightShoulder;
        public Transform references_rightUpperArm;
        public Transform references_rightForearm;
        public Transform references_rightHand;

        [Tooltip("VRIK also supports legless characters. If you do not wish to use legs, leave all leg references empty.")]
        public Transform references_leftThigh;

        [Tooltip("VRIK also supports legless characters. If you do not wish to use legs, leave all leg references empty.")]
        public Transform references_leftCalf;

        [Tooltip("VRIK also supports legless characters. If you do not wish to use legs, leave all leg references empty.")]
        public Transform references_leftFoot;

        [Tooltip("Optional")]
        public Transform references_leftToes;

        [Tooltip("VRIK also supports legless characters. If you do not wish to use legs, leave all leg references empty.")]
        public Transform references_rightThigh;

        [Tooltip("VRIK also supports legless characters. If you do not wish to use legs, leave all leg references empty.")]
        public Transform references_rightCalf;

        [Tooltip("VRIK also supports legless characters. If you do not wish to use legs, leave all leg references empty.")]
        public Transform references_rightFoot;

        [Tooltip("Optional")]
        public Transform references_rightToes;
        
        [ContextMenu("Auto-detect References")]
        public void AutoDetectReferences() {
            var animator = transform.GetComponentInChildren<Animator>();

            if (animator == null || !animator.isHuman) {
                _logger.Error("VRIK needs a Humanoid Animator to auto-detect biped references. Please assign references manually.");
                return;
            }

            references_root = transform;
            references_pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
            references_spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            references_chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            references_neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            references_head = animator.GetBoneTransform(HumanBodyBones.Head);
            references_leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            references_leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            references_leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            references_leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            references_rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            references_rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            references_rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            references_rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            references_leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            references_leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            references_leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            references_leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            references_rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            references_rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            references_rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            references_rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        }

        #endregion

        [Header("Solver")]
        [Tooltip("If true, will keep the toes planted even if head target is out of reach.")]
        public bool solver_plantFeet = true;

        #region Spine

        [Header("Spine")]

        [Tooltip("The head target.")]
        public Transform solver_spine_headTarget;

        [Tooltip("Positional weight of the head target.")]
        [Range(0f, 1f)] public float solver_spine_positionWeight = 1f;

        [Tooltip("Rotational weight of the head target.")]
        [Range(0f, 1f)] public float solver_spine_rotationWeight = 1f;

        [Tooltip("The pelvis target, useful with seated rigs.")]
        public Transform solver_spine_pelvisTarget;

        [Tooltip("Positional weight of the pelvis target.")]
        [Range(0f, 1f)] public float solver_spine_pelvisPositionWeight;

        [Tooltip("Rotational weight of the pelvis target.")]
        [Range(0f, 1f)] public float solver_spine_pelvisRotationWeight;

        [Tooltip("If 'Chest Goal Weight' is greater than 0, the chest will be turned towards this Transform.")]
        public Transform solver_spine_chestGoal;

        [Tooltip("Rotational weight of the chest target.")]
        [Range(0f, 1f)] public float solver_spine_chestGoalWeight;

        [Tooltip("Minimum height of the head from the root of the character.")]
        public float solver_spine_minHeadHeight = 0.8f;

        [Tooltip("Determines how much the body will follow the position of the head.")]
        [Range(0f, 1f)] public float solver_spine_bodyPosStiffness = 0.55f;

        [Tooltip("Determines how much the body will follow the rotation of the head.")]
        [Range(0f, 1f)] public float solver_spine_bodyRotStiffness = 0.1f;

        [Tooltip("Determines how much the chest will rotate to the rotation of the head.")]
        [Range(0f, 1f)] public float solver_spine_neckStiffness = 0.2f;

        [Tooltip("The amount of rotation applied to the chest based on hand positions.")]
        [Range(0f, 1f)] public float solver_spine_rotateChestByHands = 1f;

        [Tooltip("Clamps chest rotation.")]
        [Range(0f, 1f)] public float solver_spine_chestClampWeight = 0.5f;

        [Tooltip("Clamps head rotation.")]
        [Range(0f, 1f)] public float solver_spine_headClampWeight = 0.6f;

        [Tooltip("Moves the body horizontally along -character.forward axis by that value when the player is crouching.")]
        public float solver_spine_moveBodyBackWhenCrouching = 0.5f;

        [Tooltip("How much will the pelvis maintain it's animated position?")]
        [Range(0f, 1f)] public float solver_spine_maintainPelvisPosition = 0.2f;

        [Tooltip("Will automatically rotate the root of the character if the head target has turned past this angle.")]
        [Range(0f, 180f)] public float solver_spine_maxRootAngle = 25f;

        #endregion

        #region Left Arm

        [Header("Left Arm")]

        [Tooltip("The hand target")]
        public Transform solver_leftArm_target;

        [Tooltip("The elbow will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform solver_leftArm_bendGoal;

        [Tooltip("Positional weight of the hand target.")]
        [Range(0f, 1f)] public float solver_leftArm_positionWeight = 1f;

        [Tooltip("Rotational weight of the hand target")]
        [Range(0f, 1f)] public float solver_leftArm_rotationWeight = 1f;

        [Tooltip("Different techniques for shoulder bone rotation.")]
        public ShoulderRotationMode solver_leftArm_shoulderRotationMode = ShoulderRotationMode.YawPitch;

        [Tooltip("The weight of shoulder rotation")]
        [Range(0f, 1f)] public float solver_leftArm_shoulderRotationWeight = 1f;

        [Tooltip("The weight of twisting the shoulders back when arms are lifted up.")]
        [Range(0f, 1f)] public float solver_leftArm_shoulderTwistWeight = 1f;

        [Tooltip("If greater than 0, will bend the elbow towards the 'Bend Goal' Transform.")]
        [Range(0f, 1f)] public float solver_leftArm_bendGoalWeight;

        [Tooltip("Angular offset of the elbow bending direction.")]
        [Range(-180f, 180f)] public float solver_leftArm_swivelOffset;

        [Tooltip("Local axis of the hand bone that points from the wrist towards the palm. Used for defining hand bone orientation.")]
        public Vector3 solver_leftArm_wristToPalmAxis = Vector3.zero;

        [Tooltip("Local axis of the hand bone that points from the palm towards the thumb. Used for defining hand bone orientation.")]
        public Vector3 solver_leftArm_palmToThumbAxis = Vector3.zero;

        [Tooltip("Use this to make the arm shorter/longer.")]
        [Range(0.01f, 2f)]
        public float solver_leftArm_armLengthMlp = 1f;

        [Tooltip("Evaluates stretching of the arm by target distance relative to arm length. Value at time 1 represents stretching amount at the point where distance to the target is equal to arm length. Value at time 2 represents stretching amount at the point where distance to the target is double the arm length. Value represents the amount of stretching. Linear stretching would be achieved with a linear curve going up by 45 degrees. Increase the range of stretching by moving the last key up and right at the same amount. Smoothing in the curve can help reduce elbow snapping (start stretching the arm slightly before target distance reaches arm length).")]
        public AnimationCurve solver_leftArm_stretchCurve = new AnimationCurve();

        #endregion

        #region Right Arm

        [Header("Right Arm")]

        [Tooltip("The hand target")]
        public Transform solver_rightArm_target;

        [Tooltip("The elbow will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform solver_rightArm_bendGoal;

        [Tooltip("Positional weight of the hand target.")]
        [Range(0f, 1f)] public float solver_rightArm_positionWeight = 1f;

        [Tooltip("Rotational weight of the hand target")]
        [Range(0f, 1f)] public float solver_rightArm_rotationWeight = 1f;

        [Tooltip("Different techniques for shoulder bone rotation.")]
        public ShoulderRotationMode solver_rightArm_shoulderRotationMode = ShoulderRotationMode.YawPitch;

        [Tooltip("The weight of shoulder rotation")]
        [Range(0f, 1f)] public float solver_rightArm_shoulderRotationWeight = 1f;

        [Tooltip("The weight of twisting the shoulders back when arms are lifted up.")]
        [Range(0f, 1f)] public float solver_rightArm_shoulderTwistWeight = 1f;

        [Tooltip("If greater than 0, will bend the elbow towards the 'Bend Goal' Transform.")]
        [Range(0f, 1f)] public float solver_rightArm_bendGoalWeight;

        [Tooltip("Angular offset of the elbow bending direction.")]
        [Range(-180f, 180f)] public float solver_rightArm_swivelOffset;

        [Tooltip("Local axis of the hand bone that points from the wrist towards the palm. Used for defining hand bone orientation.")]
        public Vector3 solver_rightArm_wristToPalmAxis = Vector3.zero;

        [Tooltip("Local axis of the hand bone that points from the palm towards the thumb. Used for defining hand bone orientation.")]
        public Vector3 solver_rightArm_palmToThumbAxis = Vector3.zero;

        [Tooltip("Use this to make the arm shorter/longer.")]
        [Range(0.01f, 2f)]
        public float solver_rightArm_armLengthMlp = 1f;

        [Tooltip("Evaluates stretching of the arm by target distance relative to arm length. Value at time 1 represents stretching amount at the point where distance to the target is equal to arm length. Value at time 2 represents stretching amount at the point where distance to the target is double the arm length. Value represents the amount of stretching. Linear stretching would be achieved with a linear curve going up by 45 degrees. Increase the range of stretching by moving the last key up and right at the same amount. Smoothing in the curve can help reduce elbow snapping (start stretching the arm slightly before target distance reaches arm length).")]
        public AnimationCurve solver_rightArm_stretchCurve = new AnimationCurve();

        #endregion

        #region Left Leg

        [Header("Left Leg")]

        [Tooltip("The toe/foot target.")]
        public Transform solver_leftLeg_target;

        [Tooltip("The knee will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform solver_leftLeg_bendGoal;

        [Tooltip("Positional weight of the toe/foot target.")]
        [Range(0f, 1f)] public float solver_leftLeg_positionWeight;

        [Tooltip("Rotational weight of the toe/foot target.")]
        [Range(0f, 1f)] public float solver_leftLeg_rotationWeight;

        [Tooltip("If greater than 0, will bend the knee towards the 'Bend Goal' Transform.")]
        [Range(0f, 1f)] public float solver_leftLeg_bendGoalWeight;

        [Tooltip("Angular offset of the knee bending direction.")]
        [Range(-180f, 180f)] public float solver_leftLeg_swivelOffset;

        [Tooltip("If 0, the bend plane will be locked to the rotation of the pelvis and rotating the foot will have no effect on the knee direction. If 1, to the target rotation of the leg so that the knee will bend towards the forward axis of the foot. Values in between will be slerped between the two.")]
        [Range(0f, 1f)] public float solver_leftLeg_bendToTargetWeight = 0.5f;

        [Tooltip("Use this to make the leg shorter/longer.")]
        [Range(0.01f, 2f)]
        public float solver_leftLeg_legLengthMlp = 1f;

        [Tooltip("Evaluates stretching of the leg by target distance relative to leg length. Value at time 1 represents stretching amount at the point where distance to the target is equal to leg length. Value at time 1 represents stretching amount at the point where distance to the target is double the leg length. Value represents the amount of stretching. Linear stretching would be achieved with a linear curve going up by 45 degrees. Increase the range of stretching by moving the last key up and right at the same amount. Smoothing in the curve can help reduce knee snapping (start stretching the arm slightly before target distance reaches leg length).")]
        public AnimationCurve solver_leftLeg_stretchCurve = new AnimationCurve();

        #endregion

        #region Right Leg

        [Header("Right Leg")]

        [Tooltip("The toe/foot target.")]
        public Transform solver_rightLeg_target;

        [Tooltip("The knee will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform solver_rightLeg_bendGoal;

        [Tooltip("Positional weight of the toe/foot target.")]
        [Range(0f, 1f)] public float solver_rightLeg_positionWeight;

        [Tooltip("Rotational weight of the toe/foot target.")]
        [Range(0f, 1f)] public float solver_rightLeg_rotationWeight;

        [Tooltip("If greater than 0, will bend the knee towards the 'Bend Goal' Transform.")]
        [Range(0f, 1f)] public float solver_rightLeg_bendGoalWeight;

        [Tooltip("Angular offset of the knee bending direction.")]
        [Range(-180f, 180f)] public float solver_rightLeg_swivelOffset;

        [Tooltip("If 0, the bend plane will be locked to the rotation of the pelvis and rotating the foot will have no effect on the knee direction. If 1, to the target rotation of the leg so that the knee will bend towards the forward axis of the foot. Values in between will be slerped between the two.")]
        [Range(0f, 1f)] public float solver_rightLeg_bendToTargetWeight = 0.5f;

        [Tooltip("Use this to make the leg shorter/longer.")]
        [Range(0.01f, 2f)]
        public float solver_rightLeg_legLengthMlp = 1f;

        [Tooltip("Evaluates stretching of the leg by target distance relative to leg length. Value at time 1 represents stretching amount at the point where distance to the target is equal to leg length. Value at time 1 represents stretching amount at the point where distance to the target is double the leg length. Value represents the amount of stretching. Linear stretching would be achieved with a linear curve going up by 45 degrees. Increase the range of stretching by moving the last key up and right at the same amount. Smoothing in the curve can help reduce knee snapping (start stretching the arm slightly before target distance reaches leg length).")]
        public AnimationCurve solver_rightLeg_stretchCurve = new AnimationCurve();

        #endregion

        #region Locomotion

        [Header("Locomotion")]

        [Tooltip("Used for blending in/out of procedural locomotion.")]
        [Range(0f, 1f)] public float solver_locomotion_weight = 1f;

        [Tooltip("Tries to maintain this distance between the legs.")]
        public float solver_locomotion_footDistance = 0.3f;

        [Tooltip("Makes a step only if step target position is at least this far from the current footstep or the foot does not reach the current footstep anymore or footstep angle is past the 'Angle Threshold'.")]
        public float solver_locomotion_stepThreshold = 0.4f;

        [Tooltip("Makes a step only if step target position is at least 'Step Threshold' far from the current footstep or the foot does not reach the current footstep anymore or footstep angle is past this value.")]
        public float solver_locomotion_angleThreshold = 60f;

        [Tooltip("Multiplies angle of the center of mass - center of pressure vector. Larger value makes the character step sooner if losing balance.")]
        public float solver_locomotion_comAngleMlp = 1f;

        [Tooltip("Maximum magnitude of head/hand target velocity used in prediction.")]
        public float solver_locomotion_maxVelocity = 0.4f;

        [Tooltip("The amount of head/hand target velocity prediction.")]
        public float solver_locomotion_velocityFactor = 0.4f;

        [Tooltip("How much can a leg be extended before it is forced to step to another position? 1 means fully stretched.")]
        [Range(0.9f, 1f)]
        public float solver_locomotion_maxLegStretch = 1f;

        [Tooltip("The speed of lerping the root of the character towards the horizontal mid-point of the footsteps.")]
        public float solver_locomotion_rootSpeed = 20f;

        [Tooltip("The speed of steps.")]
        public float solver_locomotion_stepSpeed = 3f;

        [Tooltip("The height of the foot by normalized step progress (0 - 1).")]
        public AnimationCurve solver_locomotion_stepHeight = new AnimationCurve();

        [Tooltip("The height offset of the heel by normalized step progress (0 - 1).")]
        public AnimationCurve solver_locomotion_heelHeight = new AnimationCurve();

        [Tooltip("Rotates the foot while the leg is not stepping to relax the twist rotation of the leg if ideal rotation is past this angle.")]
        [Range(0f, 180f)] public float solver_locomotion_relaxLegTwistMinAngle = 20f;

        [Tooltip("The speed of rotating the foot while the leg is not stepping to relax the twist rotation of the leg.")]
        public float solver_locomotion_relaxLegTwistSpeed = 400f;

        [Tooltip("Interpolation mode of the step.")]
        public InterpolationMode solver_locomotion_stepInterpolation = InterpolationMode.InOutSine;

        [Tooltip("Offset for the approximated center of mass.")]
        public Vector3 solver_locomotion_offset;

        [Tooltip("Called when the left foot has finished a step.")]
        public UnityEvent solver_locomotion_onLeftFootstep = new UnityEvent();
        
        [Tooltip("Called when the right foot has finished a step")]
        public UnityEvent solver_locomotion_onRightFootstep = new UnityEvent();

        #endregion
        
        internal VRIK vrik;

        private ILogger _logger = new UnityDebugLogger<VRIKManager>();

        public bool areReferencesFilled => references_root != null &&
                                           references_pelvis != null &&
                                           references_spine != null &&
                                           references_head != null &&
                                           references_leftUpperArm != null &&
                                           references_leftForearm != null &&
                                           references_leftHand != null &&
                                           references_rightUpperArm != null &&
                                           references_rightForearm != null &&
                                           references_rightHand != null &&
                                           (
                                               (references_leftThigh == null && references_leftCalf == null && references_leftFoot == null && references_rightThigh == null && references_rightCalf == null && references_rightFoot == null) ||
                                               (references_leftThigh != null && references_leftCalf != null && references_leftFoot != null && references_rightThigh != null && references_rightCalf != null && references_rightFoot != null)
                                           );

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        #if !UNITY_EDITOR
        [Inject]
        private void Inject(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<VRIKManager>();
        }
        #endif

        private void Reset()
        {
            AutoDetectReferences();
        }

        public void Awake()
        {
            foreach (VRIK vrik in GetComponentsInChildren<VRIK>())
            {
                if (!areReferencesFilled && vrik.references.isFilled)
                {
                    CopyReferencesFromExistingVrik(vrik.references);
                }

                Destroy(vrik);
            }

            vrik = gameObject.AddComponent<VRIK>();
        }

        private void Start()
        {
            SetVrikFields();
        }

        #pragma warning restore IDE0051
        #endregion

        private void CopyReferencesFromExistingVrik(VRIK.References references)
        {
            references_root          = references.root;
            references_pelvis        = references.pelvis;
            references_spine         = references.spine;
            references_chest         = references.chest;
            references_neck          = references.neck;
            references_head          = references.head;
            references_leftShoulder  = references.leftShoulder;
            references_leftUpperArm  = references.leftUpperArm;
            references_leftForearm   = references.leftForearm;
            references_leftHand      = references.leftHand;
            references_rightShoulder = references.rightShoulder;
            references_rightUpperArm = references.rightUpperArm;
            references_rightForearm  = references.rightForearm;
            references_rightHand     = references.rightHand;
            references_leftThigh     = references.leftThigh;
            references_leftCalf      = references.leftCalf;
            references_leftFoot      = references.leftFoot;
            references_leftToes      = references.leftToes;
            references_rightThigh    = references.rightThigh;
            references_rightCalf     = references.rightCalf;
            references_rightFoot     = references.rightFoot;
            references_rightToes     = references.rightToes;
        }

        private void SetVrikFields()
        {
            _logger.Info($"Setting VRIK references on '{name}'");

            foreach (FieldInfo sourceField in GetType().GetFields())
            {
                string[] parts = sourceField.Name.Split('_');
                object target = vrik;

                try
                {
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        target = target.GetType().GetField(parts[i])?.GetValue(target);

                        if (target == null)
                        {
                            _logger.Warning($"Target {parts[i]} is null");
                            break;
                        }
                    }

                    if (target == null) continue;

                    FieldInfo targetField = target.GetType().GetField(parts[parts.Length - 1]);
                    object value = sourceField.GetValue(this);

                    Type sourceType = sourceField.FieldType;
                    Type targetType = targetField.FieldType;

                    _logger.Trace($"Set {string.Join(".", parts)} = {value}");

                    if (value == null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    {
                        _logger.Warning($"Tried setting non-nullable type {targetType.FullName} to null");
                        continue;
                    }
                
                    if (sourceType != targetType)
                    {
                        _logger.Warning($"Converting value from {sourceType.FullName} to {targetType.FullName}");
                    }

                    if (sourceType.IsEnum)
                    {
                        Type sourceUnderlyingType = Enum.GetUnderlyingType(sourceType);
                        _logger.Trace($"Underlying type for source {sourceType.FullName} is {sourceUnderlyingType.FullName}");
                    }

                    if (targetType.IsEnum)
                    {
                        Type targetUnderlyingType = Enum.GetUnderlyingType(targetType);
                        _logger.Trace($"Underlying type for target {targetType.FullName} is {targetUnderlyingType.FullName}");

                        targetType = targetUnderlyingType;
                    }
                
                    targetField.SetValue(target, Convert.ChangeType(value, targetType));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            if (!vrik.references.isFilled)
            {
                _logger.Warning("Some required references are missing; auto detecting references");
                vrik.AutoDetectReferences();
            }
        }
    }
}
