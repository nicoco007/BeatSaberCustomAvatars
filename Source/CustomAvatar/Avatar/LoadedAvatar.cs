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
        public bool supportsFingerTracking { get; }

        #pragma warning disable 618
        public bool isIKAvatar => (gameObject.GetComponentInChildren<VRIKManager>() ??
                                   gameObject.GetComponentInChildren<IKManager>() ??
                                   gameObject.GetComponentInChildren<IKManagerAdvanced>() as object) != null;
        #pragma warning restore 618

        public LoadedAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            gameObject = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
           
            supportsFingerTracking = avatarGameObject.GetComponentInChildren<Animator>() &&
                                     avatarGameObject.GetComponentInChildren<PoseManager>();

            eyeHeight = GetEyeHeight();
        }

        public static IEnumerator<AsyncOperation> FromFileCoroutine(string fileName, Action<LoadedAvatar> success, Action<Exception> error)
        {
            Plugin.logger.Info("Loading avatar " + fileName);

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Path.Combine(AvatarManager.kCustomAvatarsPath, fileName));
            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || !assetBundleCreateRequest.assetBundle)
            {
                error(new AvatarLoadException("Avatar game object not found"));
                yield break;
            }

            AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
            yield return assetBundleRequest;
            assetBundleCreateRequest.assetBundle.Unload(false);

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                error(new AvatarLoadException("Could not load asset bundle"));
                yield break;
            }
                
            try
            {
                success(new LoadedAvatar(fileName, assetBundleRequest.asset as GameObject));
            }
            catch (Exception ex)
            {
                error(ex);
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
                Plugin.logger.Warn("Could not find head target; height adjust may be broken");
                return Vector3.zero;
            }

            return headTarget.position - headReference.position;
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        public float GetArmSpan()
        {
            Animator animator = gameObject.GetComponentInChildren<Animator>();

            if (!animator) return 0;

            Vector3 leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
            Vector3 leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            Vector3 leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
            Vector3 leftHand = gameObject.transform.Find("LeftHand").position;

            Vector3 rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
            Vector3 rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            Vector3 rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
            Vector3 rightHand = gameObject.transform.Find("RightHand").position;

            float leftArmLength = Vector3.Distance(leftShoulder, leftUpperArm) + Vector3.Distance(leftUpperArm, leftLowerArm) + Vector3.Distance(leftLowerArm, leftHand);
            float rightArmLength = Vector3.Distance(rightShoulder, rightUpperArm) + Vector3.Distance(rightUpperArm, rightLowerArm) + Vector3.Distance(rightLowerArm, rightHand);
            float shoulderToShoulderDistance = Vector3.Distance(leftShoulder, rightShoulder);

            float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;
            
            Plugin.logger.Debug("Avatar arm span: " + totalLength);

            return totalLength;
        }
    }
}
