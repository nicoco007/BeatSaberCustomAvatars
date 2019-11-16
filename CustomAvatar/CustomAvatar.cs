using System;
using System.Collections.Generic;
using CustomAvatar.Exceptions;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		private const float MinIkAvatarHeight = 1.4f;
		private const float MaxIkAvatarHeight = 2.5f;
		private const string GameObjectName = "_CustomAvatar";
		private float? eyeHeight;

		public string FullPath { get; }
		public GameObject GameObject { get; }
		public AvatarDescriptor Descriptor { get; }
		public Transform ViewPoint { get; }

		public float EyeHeight
		{
			get
			{
				if (GameObject == null) return BeatSaberUtil.GetPlayerEyeHeight();
				if (eyeHeight == null)
				{
					var localPosition = GameObject.transform.InverseTransformPoint(ViewPoint.position);
					eyeHeight = localPosition.y;
			
					//This is to handle cases where the head might be at 0,0,0, like in a non-IK avatar.
					if (eyeHeight < MinIkAvatarHeight || eyeHeight > MaxIkAvatarHeight)
					{
						eyeHeight = MainSettingsModel.kDefaultPlayerHeight;
					}
				}

				return eyeHeight.Value;
			}
		}

		public CustomAvatar(string fullPath, GameObject avatarGameObject)
		{
			FullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
			GameObject = avatarGameObject ?? throw new ArgumentNullException(nameof(avatarGameObject));
			Descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
			ViewPoint = avatarGameObject.transform.Find("Head") ?? throw new AvatarLoadException($"Avatar '{Descriptor.Name}' does not have a Head transform");

			GameObject.transform.localScale = Vector3.one * 0.56666666666f;
		}

		public static IEnumerator<AsyncOperation> FromFileCoroutine(string filePath, Action<CustomAvatar> success, Action<Exception> error)
		{
			Plugin.Logger.Info("Loading avatar from " + filePath);

			AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);
			yield return assetBundleCreateRequest;

			if (!assetBundleCreateRequest.isDone || assetBundleCreateRequest.assetBundle == null)
			{
				error(new AvatarLoadException("Avatar game object not found"));
				yield break;
			}

			AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(GameObjectName);
			yield return assetBundleRequest;
			assetBundleCreateRequest.assetBundle.Unload(false);

			if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
			{
				error(new AvatarLoadException("Could not load asset bundle"));
				yield break;
			}
				
			try
			{
				success(new CustomAvatar(filePath, assetBundleRequest.asset as GameObject));
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
			Animator animator = GameObject.GetComponentInChildren<Animator>();

			Vector3 leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
			Vector3 leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
			Vector3 leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
			Vector3 leftHand = GameObject.transform.Find("LeftHand").position;

			Vector3 rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
			Vector3 rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
			Vector3 rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
			Vector3 rightHand = GameObject.transform.Find("RightHand").position;

			float leftArmLength = Vector3.Distance(leftShoulder, leftUpperArm) + Vector3.Distance(leftUpperArm, leftLowerArm) + Vector3.Distance(leftLowerArm, leftHand);
			float rightArmLength = Vector3.Distance(rightShoulder, rightUpperArm) + Vector3.Distance(rightUpperArm, rightLowerArm) + Vector3.Distance(rightLowerArm, rightHand);
			float shoulderToShoulderDistance = Vector3.Distance(leftShoulder, rightShoulder);
			
			float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;
			
			Plugin.Logger.Debug("Avatar arm span: " + totalLength);

			return totalLength;
		}
	}
}
