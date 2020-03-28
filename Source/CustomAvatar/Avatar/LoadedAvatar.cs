using System;
using System.Collections.Generic;
using System.IO;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    public class LoadedAvatar
    {
        private const string kGameObjectName = "_CustomAvatar";

        public string fullPath { get; }
        public GameObject gameObject { get; }
        public AvatarDescriptor descriptor { get; }
        public float eyeHeight { get; }
        public float armSpan { get; }
        public bool supportsFingerTracking { get; }

        #pragma warning disable 618
        public bool isIKAvatar => (gameObject.GetComponentInChildren<VRIKManager>() ??
                                   gameObject.GetComponentInChildren<IKManager>() ??
                                   gameObject.GetComponentInChildren<IKManagerAdvanced>() as object) != null;
        #pragma warning restore 618

        private LoadedAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            gameObject = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
           
            supportsFingerTracking = avatarGameObject.GetComponentInChildren<Animator>() &&
                                     avatarGameObject.GetComponentInChildren<PoseManager>();

            eyeHeight = GetEyeHeight();
            armSpan = GetArmSpan();
        }

        public static IEnumerator<AsyncOperation> FromFileCoroutine(string fileName, Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            Plugin.logger.Info($"Loading avatar from '{fileName}'");

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Path.Combine(AvatarManager.kCustomAvatarsPath, fileName));
            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || !assetBundleCreateRequest.assetBundle)
            {
                var exception = new AvatarLoadException("Avatar game object not found");

                Plugin.logger.Error($"Failed to load avatar from '{fileName}'");
                Plugin.logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;
            assetBundleCreateRequest.assetBundle.Unload(false);

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                var exception = new AvatarLoadException("Could not load asset bundle");

                Plugin.logger.Error($"Failed to load avatar from '{fileName}'");
                Plugin.logger.Error(exception);

                error?.Invoke(exception);
                yield break;
            }
                
            try
            {
                var loadedAvatar = new LoadedAvatar(fileName, assetBundleRequest.asset as GameObject);

                Plugin.logger.Info($"Successfully loaded avatar '{loadedAvatar.descriptor.name}' from '{fileName}'");

                success?.Invoke(loadedAvatar);
            }
            catch (Exception ex)
            {
                Plugin.logger.Error($"Failed to load avatar from '{fileName}'");
                Plugin.logger.Error(ex);

                error?.Invoke(ex);
            }
        }

        private float GetEyeHeight()
        {
            if (!isIKAvatar)
            {
                return BeatSaberUtil.kDefaultPlayerEyeHeight;
            }

            return GetViewPoint().y;
        }

        /// <summary>
        /// Get the avatar's view point (camera position) in global coordinates.
        /// </summary>
        private Vector3 GetViewPoint()
        {
            Transform head = gameObject.transform.Find("Head") ?? throw new AvatarLoadException($"Avatar '{descriptor.name}' does not have a Head transform");
            Vector3 offset = GetHeadTargetOffset();

            // only warn if offset is larger than 1 mm
            if (offset.magnitude > 0.001f)
            {
                // manually putting each coordinate gives more resolution
                Plugin.logger.Warn($"Head bone and head target are not at the same position; offset: ({offset.x}, {offset.y}, {offset.z})");
            }

            return gameObject.transform.InverseTransformPoint(head.position - offset);
        }

        /// <summary>
        /// Gets the offset between the head target and the actual head bone. Avoids issues when using
        /// the Head transform for calculations.
        /// </summary>
        private Vector3 GetHeadTargetOffset()
        {
            Transform headReference = null;
            Transform headTarget = null;
                
            #pragma warning disable 618
            VRIK vrik = gameObject.GetComponentInChildren<VRIK>();
            IKManager ikManager = gameObject.GetComponentInChildren<IKManager>();
            IKManagerAdvanced ikManagerAdvanced = gameObject.GetComponentInChildren<IKManagerAdvanced>();
            #pragma warning restore 618

            VRIKManager vrikManager = gameObject.GetComponentInChildren<VRIKManager>();
                
            if (vrikManager)
            {
                if (!vrikManager.references_head) vrikManager.AutoDetectReferences();

                headReference = vrikManager.references_head;
                headTarget = vrikManager.solver_spine_headTarget;
            }
            else if (vrik)
            {
                vrik.AutoDetectReferences();
                headReference = vrik.references.head;

                if (ikManagerAdvanced)
                {
                    headTarget = ikManagerAdvanced.HeadTarget;
                }
                else if (ikManager)
                {
                    headTarget = ikManager.HeadTarget;
                }
            }

            if (!headReference)
            {
                Plugin.logger.Warn("Could not find head reference; height adjust may be broken");
                return Vector3.zero;
            }

            if (!headTarget)
            {
                // target will be added automatically, no need to adjust
                return Vector3.zero;
            }

            return headTarget.position - headReference.position;
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        private float GetArmSpan()
        {
            Animator animator = gameObject.GetComponentInChildren<Animator>();

            if (!animator) return AvatarTailor.kDefaultPlayerArmSpan;

            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform leftHand = gameObject.transform.Find("LeftHand");

            Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rightHand = gameObject.transform.Find("RightHand");

            if (!leftShoulder || !leftUpperArm || !leftLowerArm || !leftHand || !rightShoulder || !rightUpperArm || !rightLowerArm || !rightHand)
            {
                Plugin.logger.Warn("Could not calculate avatar arm span due to missing bones");
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
