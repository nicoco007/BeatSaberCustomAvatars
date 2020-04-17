using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    public class LoadedAvatar
    {
        private const string kGameObjectName = "_CustomAvatar";

        public string fullPath { get; }
        public GameObject prefab { get; }
        public AvatarDescriptor descriptor { get; }
        public float eyeHeight { get; }
        public float armSpan { get; }
        public bool supportsFingerTracking { get; }
        public bool isIKAvatar { get; }

        private LoadedAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            prefab = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
           
            supportsFingerTracking = avatarGameObject.GetComponentInChildren<Animator>() &&
                                     avatarGameObject.GetComponentInChildren<PoseManager>();

            VRIKManager vrikManager = avatarGameObject.GetComponentInChildren<VRIKManager>();
            IKManager ikManager = avatarGameObject.GetComponentInChildren<IKManager>();
            VRIK vrik = avatarGameObject.GetComponentInChildren<VRIK>();

            isIKAvatar = ikManager || vrikManager;

            //_logger = loggerFactory.CreateLogger<LoadedAvatar>(descriptor.name);

            if (vrik && !vrik.references.isFilled) vrik.AutoDetectReferences();
            if (vrikManager && !vrikManager.areReferencesFilled) vrikManager.AutoDetectReferences();

            if (isIKAvatar) FixTrackingReferences();

            eyeHeight = GetEyeHeight();
            armSpan = GetArmSpan();
        }

        public static IEnumerator<AsyncOperation> FromFileCoroutine(string fileName, Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            //_logger.Info($"Loading avatar from '{fileName}'");

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Path.Combine(AvatarManager.kCustomAvatarsPath, fileName));
            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || !assetBundleCreateRequest.assetBundle)
            {
                var exception = new AvatarLoadException("Avatar game object not found");

                //_logger.Error($"Failed to load avatar from '{fileName}'");
                //_logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;
            assetBundleCreateRequest.assetBundle.Unload(false);

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                var exception = new AvatarLoadException("Could not load asset bundle");

                //_logger.Error($"Failed to load avatar from '{fileName}'");
                //_logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }
                
            try
            {
                var loadedAvatar = new LoadedAvatar(fileName, assetBundleRequest.asset as GameObject);

                //_logger.Info($"Successfully loaded avatar '{loadedAvatar.descriptor.name}' from '{fileName}'");

                success?.Invoke(loadedAvatar);
            }
            catch (Exception ex)
            {
                //_logger.Error($"Failed to load avatar from '{fileName}'");
                //_logger.Error(ex);

                error?.Invoke(ex);
            }
        }

        private float GetEyeHeight()
        {
            Transform head = prefab.transform.Find("Head");

            if (!head) throw new AvatarLoadException("Avatar does not have a head tracking reference");

            // many avatars rely on this being global because their root position isn't at (0, 0, 0)
            return head.position.y;
        }

        private void FixTrackingReferences()
        {
            Vector3 headOffset = GetTargetOffset(nameof(VRIK.References.head), nameof(VRIKManager.solver_spine_headTarget), nameof(IKManager.HeadTarget));
            Vector3 leftHandOffset = GetTargetOffset(nameof(VRIK.References.leftHand), nameof(VRIKManager.solver_leftArm_target), nameof(IKManager.LeftHandTarget));
            Vector3 rightHandOffset = GetTargetOffset(nameof(VRIK.References.rightHand), nameof(VRIKManager.solver_rightArm_target), nameof(IKManager.RightHandTarget));
            
            // only warn if offset is larger than 1 mm
            if (headOffset.magnitude > 0.001f)
            {
                // manually putting each coordinate gives more resolution
                //_logger.Warn($"Head bone and target are not at the same position; offset: ({headOffset.x}, {headOffset.y}, {headOffset.z})");
                prefab.transform.Find("Head").position -= headOffset;
            }

            if (leftHandOffset.magnitude > 0.001f)
            {
                //_logger.Warn($"Left hand bone and target are not at the same position; offset: ({leftHandOffset.x}, {leftHandOffset.y}, {leftHandOffset.z})");
                prefab.transform.Find("LeftHand").position -= headOffset;
            }

            if (rightHandOffset.magnitude > 0.001f)
            {
                //_logger.Warn($"Right hand bone and target are not at the same position; offset: ({rightHandOffset.x}, {rightHandOffset.y}, {rightHandOffset.z})");
                prefab.transform.Find("RightHand").position -= headOffset;
            }
        }

        /// <summary>
        /// Gets the offset between the target and the actual bone. Avoids issues when using just the tracking reference transform for calculations.
        /// </summary>
        private Vector3 GetTargetOffset(string referenceName, string vrikManagerTargetName, string ikManagerTargetName)
        {
            Transform reference = null;
            Transform target = null;
                
            #pragma warning disable 618
            VRIK vrik = prefab.GetComponentInChildren<VRIK>();
            IKManager ikManager = prefab.GetComponentInChildren<IKManager>();
            #pragma warning restore 618

            VRIKManager vrikManager = prefab.GetComponentInChildren<VRIKManager>();
                
            if (vrikManager)
            {
                if (!vrikManager.references_head) vrikManager.AutoDetectReferences();

                reference = GetFieldValue<Transform>(vrikManager, "references_" + referenceName);
                target = GetFieldValue<Transform>(vrikManager, vrikManagerTargetName);
            }
            else if (vrik)
            {
                vrik.AutoDetectReferences();
                reference = GetFieldValue<Transform>(vrik.references, referenceName);

                if (ikManager)
                {
                    target = GetFieldValue<Transform>(ikManager, ikManagerTargetName);
                }
            }

            if (!reference)
            {
                //_logger.Warn($"Could not find '{referenceName}' reference");
                return Vector3.zero;
            }

            if (!target)
            {
                // target will be added automatically, no need to adjust
                return Vector3.zero;
            }

            return target.position - reference.position;
        }

        private T GetFieldValue<T>(object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (field == null)
            {
                throw new InvalidOperationException($"Public instance field '{fieldName}' does not exist");
            }

            return (T) field.GetValue(obj);
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        private float GetArmSpan()
        {
            // TODO using animator here probably isn't a good idea, use VRIKManager references instead
            Animator animator = prefab.GetComponentInChildren<Animator>();

            if (!animator) return AvatarTailor.kDefaultPlayerArmSpan;

            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform leftHand = prefab.transform.Find("LeftHand");

            Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rightHand = prefab.transform.Find("RightHand");

            if (!leftShoulder || !leftUpperArm || !leftLowerArm || !rightShoulder || !rightUpperArm || !rightLowerArm)
            {
                //_logger.Warn("Could not calculate avatar arm span due to missing bones");
                return AvatarTailor.kDefaultPlayerArmSpan;
            }

            if (!leftHand || !rightHand)
            {
                //_logger.Warn("Could not calculate avatar arm span due to missing tracking references");
                return AvatarTailor.kDefaultPlayerArmSpan;
            }

            float leftArmLength = Vector3.Distance(leftShoulder.position, leftUpperArm.position) + Vector3.Distance(leftUpperArm.position, leftLowerArm.position) + Vector3.Distance(leftLowerArm.position, leftHand.position);
            float rightArmLength = Vector3.Distance(rightShoulder.position, rightUpperArm.position) + Vector3.Distance(rightUpperArm.position, rightLowerArm.position) + Vector3.Distance(rightLowerArm.position, rightHand.position);
            float shoulderToShoulderDistance = Vector3.Distance(leftShoulder.position, rightShoulder.position);

            float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;

            return totalLength;
        }
    }
}
