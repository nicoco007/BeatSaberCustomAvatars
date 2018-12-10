using System;
using UnityEngine;

namespace CustomAvatar
{
	public static class AvatarMeasurement
	{
		public const float DefaultPlayerHeight = 1.8f;
		private const float EyeToTopOfHeadDistance = 0.06f;
		private const float MinHeight = 1.4f;
		private const float MaxHeight = 2f;
		
		public static float MeasureHeight(GameObject avatarGameObject, Transform viewPoint)
		{
			var localPosition = avatarGameObject.transform.InverseTransformPoint(viewPoint.position);
			var height = localPosition.y + EyeToTopOfHeadDistance;
			
			//This is to handle cases where the head might be at 0,0,0, like in a non-IK avatar.
			if (height < MinHeight || height > MaxHeight)
			{
				height = DefaultPlayerHeight;
			}
			
			return Mathf.Clamp(height, MinHeight, MaxHeight);
		}

		public static float? MeasureArmLength(Animator animator)
		{
			var indexFinger1 = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).position;
			var leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
			var leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
			var rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
			var leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
			var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;

			var shoulderLength = Vector3.Distance(leftUpperArm, leftShoulder) * 2.0f + Vector3.Distance(leftShoulder, rightShoulder);
			var armLength = (Vector3.Distance(indexFinger1, leftHand) * 0.5f + Vector3.Distance(leftHand, leftElbow) + Vector3.Distance(leftElbow, leftUpperArm)) * 2.0f;

			return shoulderLength + armLength;
		}
	}
}
