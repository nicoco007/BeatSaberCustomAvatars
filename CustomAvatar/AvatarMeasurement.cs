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
	}
}