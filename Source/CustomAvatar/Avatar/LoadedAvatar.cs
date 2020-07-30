using System;
using System.IO;
using System.Reflection;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar.Avatar
{
    public class LoadedAvatar
    {
        public readonly string fileName;
        public readonly string fullPath;
        public readonly GameObject prefab;
        public readonly AvatarDescriptor descriptor;

        public readonly bool isIKAvatar;
        public readonly bool supportsFullBodyTracking;
        public readonly bool supportsFingerTracking;

        public readonly float eyeHeight;
        public readonly float armSpan;

        private readonly Transform _head;
        private readonly Transform _leftHand;
        private readonly Transform _rightHand;
        private readonly Transform _leftLeg;
        private readonly Transform _rightLeg;
        private readonly Transform _pelvis;

        private ILogger<LoadedAvatar> _logger;

        internal LoadedAvatar(string fullPath, GameObject avatarGameObject, ILoggerProvider loggerProvider)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            prefab = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");

            fileName = Path.GetFileName(fullPath);

            _logger = loggerProvider.CreateLogger<LoadedAvatar>(descriptor.name);

            VRIKManager vrikManager = prefab.GetComponentInChildren<VRIKManager>();

            #pragma warning disable CS0618
            IKManager ikManager = prefab.GetComponentInChildren<IKManager>();
            #pragma warning restore CS0618

            // migrate IKManager/IKManagerAdvanced to VRIKManager
            if (ikManager)
            {
                if (!vrikManager) vrikManager = prefab.AddComponent<VRIKManager>();

                _logger.Warning("IKManager and IKManagerAdvanced are deprecated; please migrate to VRIKManager");

                ApplyIKManagerFields(vrikManager, ikManager);
                Object.Destroy(ikManager);
            }

            _head      = prefab.transform.Find("Head");
            _leftHand  = prefab.transform.Find("LeftHand");
            _rightHand = prefab.transform.Find("RightHand");
            _pelvis    = prefab.transform.Find("Pelvis");
            _leftLeg   = prefab.transform.Find("LeftLeg");
            _rightLeg  = prefab.transform.Find("RightLeg");

            if (vrikManager)
            {
                if (!vrikManager.areReferencesFilled)
                {
                    vrikManager.AutoDetectReferences();
                }

                FixTrackingReferences(vrikManager);
            }

            var poseManager = prefab.GetComponentInChildren<PoseManager>();

            isIKAvatar = vrikManager;
            supportsFullBodyTracking = prefab.transform.Find("Pelvis") || prefab.transform.Find("LeftLeg") || prefab.transform.Find("RightLeg");
            supportsFingerTracking = poseManager && poseManager.isValid;

            eyeHeight = GetEyeHeight();
            armSpan = GetArmSpan();
        }

        private float GetEyeHeight()
        {
            if (!_head)
            {
                _logger.Warning("Avatar does not have a head tracking reference");
                return MainSettingsModelSO.kDefaultPlayerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;
            }

            // many avatars rely on this being global because their root position isn't at (0, 0, 0)
            return _head.position.y;
        }

        private void FixTrackingReferences(VRIKManager vrikManager)
        {
            FixTrackingReference("Head",       _head,      vrikManager.references_head,                                          vrikManager.solver_spine_headTarget);
            FixTrackingReference("Left Hand",  _leftHand,  vrikManager.references_leftHand,                                      vrikManager.solver_leftArm_target);
            FixTrackingReference("Right Hand", _rightHand, vrikManager.references_rightHand,                                     vrikManager.solver_rightArm_target);
            FixTrackingReference("Waist",      _pelvis,    vrikManager.references_pelvis,                                        vrikManager.solver_spine_pelvisTarget);
            FixTrackingReference("Left Foot",  _leftLeg,   vrikManager.references_leftToes  ?? vrikManager.references_leftFoot,  vrikManager.solver_leftLeg_target);
            FixTrackingReference("Right Foot", _rightLeg,  vrikManager.references_rightToes ?? vrikManager.references_rightFoot, vrikManager.solver_rightLeg_target);
        }

        private void FixTrackingReference(string name, Transform tracker, Transform reference, Transform target)
        {
            if (!reference)
            {
                _logger.Warning($"Could not find {name} reference");
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
                _logger.Warning($"{name} bone and target are not at the same position; moving '{tracker.name}' by ({offset.x:0.000}, {offset.y:0.000}, {offset.z:0.000})");
                tracker.position -= offset;
            }
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        private float GetArmSpan()
        {
            // TODO using animator here probably isn't a good idea, use VRIKManager references instead?
            Animator animator = prefab.GetComponentInChildren<Animator>();

            if (!animator) return AvatarTailor.kDefaultPlayerArmSpan;

            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);

            Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

            if (!leftShoulder || !leftUpperArm || !leftLowerArm || !rightShoulder || !rightUpperArm || !rightLowerArm)
            {
                _logger.Warning("Could not calculate avatar arm span due to missing bones");
                return AvatarTailor.kDefaultPlayerArmSpan;
            }

            if (!_leftHand || !_rightHand)
            {
                _logger.Warning("Could not calculate avatar arm span due to missing tracking references");
                return AvatarTailor.kDefaultPlayerArmSpan;
            }

            float leftArmLength = Vector3.Distance(leftShoulder.position, leftUpperArm.position) + Vector3.Distance(leftUpperArm.position, leftLowerArm.position) + Vector3.Distance(leftLowerArm.position, _leftHand.position);
            float rightArmLength = Vector3.Distance(rightShoulder.position, rightUpperArm.position) + Vector3.Distance(rightUpperArm.position, rightLowerArm.position) + Vector3.Distance(rightLowerArm.position, _rightHand.position);
            float shoulderToShoulderDistance = Vector3.Distance(leftShoulder.position, rightShoulder.position);

            float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;

            return totalLength;
        }

        #pragma warning disable CS0618
        private void ApplyIKManagerFields(VRIKManager vrikManager, IKManager ikManager)
        {
            vrikManager.solver_spine_headTarget = ikManager.HeadTarget;
            vrikManager.solver_leftArm_target = ikManager.LeftHandTarget;
            vrikManager.solver_rightArm_target = ikManager.RightHandTarget;

            if (!(ikManager is IKManagerAdvanced ikManagerAdvanced)) return;

            FieldInfo[] fieldInfos = typeof(IKManagerAdvanced).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                string[] propertyName = fieldInfo.Name.Split('_');
                var value = fieldInfo.GetValue(ikManagerAdvanced);

                if (propertyName.Length > 1)
                {
                    if ("Spine" == propertyName[0])
                    {
                        SetField(vrikManager, "solver_spine_" + propertyName[1], value);
                    }
                    else if ("LeftArm" == propertyName[0])
                    {
                        SetField(vrikManager, "solver_leftArm_" + propertyName[1], value);
                    }
                    else if ("RightArm" == propertyName[0])
                    {
                        SetField(vrikManager, "solver_rightArm_" + propertyName[1], value);
                    }
                    else if ("LeftLeg" == propertyName[0])
                    {
                        SetField(vrikManager, "solver_leftLeg_" + propertyName[1], value);
                    }
                    else if ("RightLeg" == propertyName[0])
                    {
                        SetField(vrikManager, "solver_rightLeg_" + propertyName[1], value);
                    }
                    else if ("Locomotion" == propertyName[0])
                    {
                        SetField(vrikManager, "solver_locomotion_" + propertyName[1], value);
                    }
                }
            }
        }
        #pragma warning restore CS0618

        private void SetField(object target, string fieldName, object value)
        {
            if (target == null) throw new NullReferenceException(nameof(target));
            if (fieldName == null) throw new NullReferenceException(nameof(fieldName));

            try
            {
                _logger.Trace($"Set {fieldName} = {value}");

                Type targetObjectType = target.GetType();
                FieldInfo field = targetObjectType.GetField(fieldName);

                if (field == null)
                {
                    _logger.Warning($"{fieldName} does not exist on {targetObjectType.FullName}");
                    return;
                }

                Type sourceType = value?.GetType();
                Type targetType = field.FieldType;

                if (value == null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    _logger.Warning($"Tried setting non-nullable type {targetType.FullName} to null");
                    return;
                }

                if (sourceType != null)
                {
                    if (sourceType != targetType)
                    {
                        _logger.Warning($"Converting value from {sourceType.FullName} to {targetType.FullName}");
                    }

                    if (sourceType.IsEnum)
                    {
                        Type sourceUnderlyingType = Enum.GetUnderlyingType(sourceType);
                        _logger.Trace($"Underlying type for source {sourceType.FullName} is {sourceUnderlyingType.FullName}");
                    }
                }

                if (targetType.IsEnum)
                {
                    Type targetUnderlyingType = Enum.GetUnderlyingType(targetType);
                    _logger.Trace($"Underlying type for target {targetType.FullName} is {targetUnderlyingType.FullName}");

                    targetType = targetUnderlyingType;
                }

                field.SetValue(target, Convert.ChangeType(value, targetType));
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}
