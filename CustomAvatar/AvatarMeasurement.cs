using System;
using UnityEngine;

namespace CustomAvatar
{
	public static class AvatarMeasurement
	{
		private const float MinHeight = 1.4f;
		private const float MaxHeight = 4f;
		
		public static float MeasureEyeHeight(GameObject avatarGameObject, Transform viewPoint)
		{
			var localPosition = avatarGameObject.transform.InverseTransformPoint(viewPoint.position);
			var eyeHeight = localPosition.y;
			
			//This is to handle cases where the head might be at 0,0,0, like in a non-IK avatar.
			if (eyeHeight < MinHeight || eyeHeight > MaxHeight)
			{
				eyeHeight = MainSettingsModel.kDefaultPlayerHeight;
			}
			
			return Mathf.Clamp(eyeHeight, MinHeight, MaxHeight);
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
