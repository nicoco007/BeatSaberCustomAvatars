using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// ReSharper disable InconsistentNaming
namespace CustomAvatar
{
	public class PoseManager : MonoBehaviour
	{
		[HideInInspector] public Pose openHand_LeftThumbProximal;
		[HideInInspector] public Pose openHand_LeftThumbIntermediate;
		[HideInInspector] public Pose openHand_LeftThumbDistal;

		[HideInInspector] public Pose openHand_LeftIndexProximal;
		[HideInInspector] public Pose openHand_LeftIndexIntermediate;
		[HideInInspector] public Pose openHand_LeftIndexDistal;

		[HideInInspector] public Pose openHand_LeftMiddleProximal;
		[HideInInspector] public Pose openHand_LeftMiddleIntermediate;
		[HideInInspector] public Pose openHand_LeftMiddleDistal;

		[HideInInspector] public Pose openHand_LeftRingProximal;
		[HideInInspector] public Pose openHand_LeftRingIntermediate;
		[HideInInspector] public Pose openHand_LeftRingDistal;

		[HideInInspector] public Pose openHand_LeftLittleProximal;
		[HideInInspector] public Pose openHand_LeftLittleIntermediate;
		[HideInInspector] public Pose openHand_LeftLittleDistal;

		[HideInInspector] public Pose openHand_RightThumbProximal;
		[HideInInspector] public Pose openHand_RightThumbIntermediate;
		[HideInInspector] public Pose openHand_RightThumbDistal;

		[HideInInspector] public Pose openHand_RightIndexProximal;
		[HideInInspector] public Pose openHand_RightIndexIntermediate;
		[HideInInspector] public Pose openHand_RightIndexDistal;

		[HideInInspector] public Pose openHand_RightMiddleProximal;
		[HideInInspector] public Pose openHand_RightMiddleIntermediate;
		[HideInInspector] public Pose openHand_RightMiddleDistal;

		[HideInInspector] public Pose openHand_RightRingProximal;
		[HideInInspector] public Pose openHand_RightRingIntermediate;
		[HideInInspector] public Pose openHand_RightRingDistal;

		[HideInInspector] public Pose openHand_RightLittleProximal;
		[HideInInspector] public Pose openHand_RightLittleIntermediate;
		[HideInInspector] public Pose openHand_RightLittleDistal;

		[HideInInspector] public Pose closedHand_LeftThumbProximal;
		[HideInInspector] public Pose closedHand_LeftThumbIntermediate;
		[HideInInspector] public Pose closedHand_LeftThumbDistal;

		[HideInInspector] public Pose closedHand_LeftIndexProximal;
		[HideInInspector] public Pose closedHand_LeftIndexIntermediate;
		[HideInInspector] public Pose closedHand_LeftIndexDistal;

		[HideInInspector] public Pose closedHand_LeftMiddleProximal;
		[HideInInspector] public Pose closedHand_LeftMiddleIntermediate;
		[HideInInspector] public Pose closedHand_LeftMiddleDistal;

		[HideInInspector] public Pose closedHand_LeftRingProximal;
		[HideInInspector] public Pose closedHand_LeftRingIntermediate;
		[HideInInspector] public Pose closedHand_LeftRingDistal;

		[HideInInspector] public Pose closedHand_LeftLittleProximal;
		[HideInInspector] public Pose closedHand_LeftLittleIntermediate;
		[HideInInspector] public Pose closedHand_LeftLittleDistal;

		[HideInInspector] public Pose closedHand_RightThumbProximal;
		[HideInInspector] public Pose closedHand_RightThumbIntermediate;
		[HideInInspector] public Pose closedHand_RightThumbDistal;

		[HideInInspector] public Pose closedHand_RightIndexProximal;
		[HideInInspector] public Pose closedHand_RightIndexIntermediate;
		[HideInInspector] public Pose closedHand_RightIndexDistal;

		[HideInInspector] public Pose closedHand_RightMiddleProximal;
		[HideInInspector] public Pose closedHand_RightMiddleIntermediate;
		[HideInInspector] public Pose closedHand_RightMiddleDistal;

		[HideInInspector] public Pose closedHand_RightRingProximal;
		[HideInInspector] public Pose closedHand_RightRingIntermediate;
		[HideInInspector] public Pose closedHand_RightRingDistal;

		[HideInInspector] public Pose closedHand_RightLittleProximal;
		[HideInInspector] public Pose closedHand_RightLittleIntermediate;
		[HideInInspector] public Pose closedHand_RightLittleDistal;

		public void SaveOpenHand(Animator animator)
		{
			SaveValues("openHand", animator);
		}

		public void SaveClosedHand(Animator animator)
		{
			SaveValues("closedHand", animator);
		}

		public void ApplyOpenHand(Animator animator)
		{
			ApplyValues("openHand", animator);
		}

		public void ApplyClosedHand(Animator animator)
		{
			ApplyValues("closedHand", animator);
		}

		private void SaveValues(string prefix, Animator animator)
		{
			if (!animator.isHuman) return;

			foreach (FieldInfo field in GetType().GetFields().Where(f => f.Name.StartsWith(prefix)))
			{
				string boneName = field.Name.Split('_')[1];

				if (Enum.TryParse(boneName, out HumanBodyBones bone))
				{
					field.SetValue(this, TransformToLocalPose(animator.GetBoneTransform(bone)));
				}
				else
				{
					Debug.LogError($"Could not find HumanBodyBones.{boneName}");
				}
			}
		}

		private void ApplyValues(string prefix, Animator animator)
		{
			if (!animator.isHuman) return;

			foreach (FieldInfo field in GetType().GetFields().Where(f => f.Name.StartsWith(prefix)))
			{
				string boneName = field.Name.Split('_')[1];

				if (Enum.TryParse(boneName, out HumanBodyBones bone))
				{
					Pose bonePose = (Pose)field.GetValue(this);

					if (bonePose.Equals(default)) continue;

					Transform boneTransform = animator.GetBoneTransform(bone);
					boneTransform.localPosition = bonePose.position;
					boneTransform.localRotation = bonePose.rotation;
				}
				else
				{
					Debug.LogError($"Could not find HumanBodyBones.{boneName}");
				}
			}
		}

		private Pose TransformToLocalPose(Transform transform)
		{
			if (transform == null) return default;

			return new Pose(transform.localPosition, transform.localRotation);
		}
	}
}
