using UnityEngine;

namespace CustomAvatar
{
	public class PoseManager : MonoBehaviour
	{
		public Pose OpenHand_Left_ThumbProximal;
		public Pose OpenHand_Left_ThumbIntermediate;
		public Pose OpenHand_Left_ThumbDistal;

		public Pose OpenHand_Left_IndexProximal;
		public Pose OpenHand_Left_IndexIntermediate;
		public Pose OpenHand_Left_IndexDistal;

		public Pose OpenHand_Left_MiddleProximal;
		public Pose OpenHand_Left_MiddleIntermediate;
		public Pose OpenHand_Left_MiddleDistal;

		public Pose OpenHand_Left_RingProximal;
		public Pose OpenHand_Left_RingIntermediate;
		public Pose OpenHand_Left_RingDistal;

		public Pose OpenHand_Left_LittleProximal;
		public Pose OpenHand_Left_LittleIntermediate;
		public Pose OpenHand_Left_LittleDistal;

		public Pose OpenHand_Right_ThumbProximal;
		public Pose OpenHand_Right_ThumbIntermediate;
		public Pose OpenHand_Right_ThumbDistal;

		public Pose OpenHand_Right_IndexProximal;
		public Pose OpenHand_Right_IndexIntermediate;
		public Pose OpenHand_Right_IndexDistal;

		public Pose OpenHand_Right_MiddleProximal;
		public Pose OpenHand_Right_MiddleIntermediate;
		public Pose OpenHand_Right_MiddleDistal;

		public Pose OpenHand_Right_RingProximal;
		public Pose OpenHand_Right_RingIntermediate;
		public Pose OpenHand_Right_RingDistal;

		public Pose OpenHand_Right_LittleProximal;
		public Pose OpenHand_Right_LittleIntermediate;
		public Pose OpenHand_Right_LittleDistal;

		public Pose ClosedHand_Left_ThumbProximal;
		public Pose ClosedHand_Left_ThumbIntermediate;
		public Pose ClosedHand_Left_ThumbDistal;

		public Pose ClosedHand_Left_IndexProximal;
		public Pose ClosedHand_Left_IndexIntermediate;
		public Pose ClosedHand_Left_IndexDistal;

		public Pose ClosedHand_Left_MiddleProximal;
		public Pose ClosedHand_Left_MiddleIntermediate;
		public Pose ClosedHand_Left_MiddleDistal;

		public Pose ClosedHand_Left_RingProximal;
		public Pose ClosedHand_Left_RingIntermediate;
		public Pose ClosedHand_Left_RingDistal;

		public Pose ClosedHand_Left_LittleProximal;
		public Pose ClosedHand_Left_LittleIntermediate;
		public Pose ClosedHand_Left_LittleDistal;

		public Pose ClosedHand_Right_ThumbProximal;
		public Pose ClosedHand_Right_ThumbIntermediate;
		public Pose ClosedHand_Right_ThumbDistal;

		public Pose ClosedHand_Right_IndexProximal;
		public Pose ClosedHand_Right_IndexIntermediate;
		public Pose ClosedHand_Right_IndexDistal;

		public Pose ClosedHand_Right_MiddleProximal;
		public Pose ClosedHand_Right_MiddleIntermediate;
		public Pose ClosedHand_Right_MiddleDistal;

		public Pose ClosedHand_Right_RingProximal;
		public Pose ClosedHand_Right_RingIntermediate;
		public Pose ClosedHand_Right_RingDistal;

		public Pose ClosedHand_Right_LittleProximal;
		public Pose ClosedHand_Right_LittleIntermediate;
		public Pose ClosedHand_Right_LittleDistal;

		public void SetOpenHand(Animator animator)
		{
			OpenHand_Left_ThumbProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));
			OpenHand_Left_ThumbIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));
			OpenHand_Left_ThumbDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal));

			OpenHand_Left_IndexProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal));
			OpenHand_Left_IndexIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate));
			OpenHand_Left_IndexDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal));

			OpenHand_Left_MiddleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal));
			OpenHand_Left_MiddleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate));
			OpenHand_Left_MiddleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal));

			OpenHand_Left_RingProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftRingProximal));
			OpenHand_Left_RingIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate));
			OpenHand_Left_RingDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftRingDistal));

			OpenHand_Left_LittleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal));
			OpenHand_Left_LittleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate));
			OpenHand_Left_LittleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal));

			OpenHand_Right_ThumbProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));
			OpenHand_Right_ThumbIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate));
			OpenHand_Right_ThumbDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightThumbDistal));

			OpenHand_Right_IndexProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightIndexProximal));
			OpenHand_Right_IndexIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate));
			OpenHand_Right_IndexDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightIndexDistal));

			OpenHand_Right_MiddleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal));
			OpenHand_Right_MiddleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate));
			OpenHand_Right_MiddleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal));

			OpenHand_Right_RingProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightRingProximal));
			OpenHand_Right_RingIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate));
			OpenHand_Right_RingDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightRingDistal));

			OpenHand_Right_LittleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightLittleProximal));
			OpenHand_Right_LittleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate));
			OpenHand_Right_LittleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightLittleDistal));
		}

		public void SetClosedHand(Animator animator)
		{
			ClosedHand_Left_ThumbProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));
			ClosedHand_Left_ThumbIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));
			ClosedHand_Left_ThumbDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal));

			ClosedHand_Left_IndexProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal));
			ClosedHand_Left_IndexIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate));
			ClosedHand_Left_IndexDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal));

			ClosedHand_Left_MiddleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal));
			ClosedHand_Left_MiddleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate));
			ClosedHand_Left_MiddleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal));

			ClosedHand_Left_RingProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftRingProximal));
			ClosedHand_Left_RingIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate));
			ClosedHand_Left_RingDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftRingDistal));

			ClosedHand_Left_LittleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal));
			ClosedHand_Left_LittleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate));
			ClosedHand_Left_LittleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal));

			ClosedHand_Right_ThumbProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));
			ClosedHand_Right_ThumbIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate));
			ClosedHand_Right_ThumbDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightThumbDistal));

			ClosedHand_Right_IndexProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightIndexProximal));
			ClosedHand_Right_IndexIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate));
			ClosedHand_Right_IndexDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightIndexDistal));

			ClosedHand_Right_MiddleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal));
			ClosedHand_Right_MiddleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate));
			ClosedHand_Right_MiddleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal));

			ClosedHand_Right_RingProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightRingProximal));
			ClosedHand_Right_RingIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate));
			ClosedHand_Right_RingDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightRingDistal));

			ClosedHand_Right_LittleProximal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightLittleProximal));
			ClosedHand_Right_LittleIntermediate = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate));
			ClosedHand_Right_LittleDistal = TransformToPose(animator.GetBoneTransform(HumanBodyBones.RightLittleDistal));
		}

		public Pose TransformToPose(Transform transform)
		{
			return new Pose(transform.localPosition, transform.localRotation);
		}
	}
}
