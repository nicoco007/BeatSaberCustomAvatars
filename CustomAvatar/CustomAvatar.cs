using System;
using System.Collections.Generic;
using System.IO;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
    public class CustomAvatar
    {
        private const float kMinIkAvatarHeight = 1.4f;
        private const float kMaxIkAvatarHeight = 2.5f;
        private const string kGameObjectName = "_CustomAvatar";

        private float? _eyeHeight;

        public string fullPath { get; }
        public GameObject gameObject { get; }
        public AvatarDescriptor descriptor { get; }
        public Transform viewPoint { get; }

        public float eyeHeight
        {
            get
            {
                if (gameObject == null) return BeatSaberUtil.GetPlayerEyeHeight();
                if (this._eyeHeight == null)
                {
                    var localPosition = gameObject.transform.InverseTransformPoint(viewPoint.position);
                    this._eyeHeight = localPosition.y;
            
                    //This is to handle cases where the head might be at 0,0,0, like in a non-IK avatar.
                    if (this._eyeHeight < kMinIkAvatarHeight || this._eyeHeight > kMaxIkAvatarHeight)
                    {
                        this._eyeHeight = MainSettingsModel.kDefaultPlayerHeight;
                    }
                }

                return this._eyeHeight.Value;
            }
        }

        public bool isIKAvatar => (gameObject.GetComponentInChildren<VRIKManager>()?.gameObject ??
                                   gameObject.GetComponentInChildren<IKManager>()?.gameObject ??
                                   gameObject.GetComponentInChildren<IKManagerAdvanced>()?.gameObject) != null;

        public CustomAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            gameObject = avatarGameObject ?? throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
            viewPoint = avatarGameObject.transform.Find("Head") ?? throw new AvatarLoadException($"Avatar '{descriptor.name}' does not have a Head transform");
        }

        public static IEnumerator<AsyncOperation> FromFileCoroutine(string fileName, Action<CustomAvatar> success, Action<Exception> error)
        {
            Plugin.logger.Info("Loading avatar " + fileName);

            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Path.Combine(AvatarManager.kCustomAvatarsPath, fileName));
            yield return assetBundleCreateRequest;

            if (!assetBundleCreateRequest.isDone || assetBundleCreateRequest.assetBundle == null)
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
                success(new CustomAvatar(fileName, assetBundleRequest.asset as GameObject));
            }
            catch (Exception ex)
            {
                error(ex);
            }
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        public float GetArmSpan()
        {
            Animator animator = gameObject.GetComponentInChildren<Animator>();

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
