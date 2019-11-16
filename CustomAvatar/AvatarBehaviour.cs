using AvatarScriptPack;
using CustomAvatar.Tracking;
using DynamicOpenVR.IO;
using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarBehaviour : MonoBehaviour
	{
		public static Pose? LeftLegCorrection { get; set; }
		public static Pose? RightLegCorrection { get; set; }
		public static Pose? PelvisCorrection { get; set; }

		private Transform head;
		private Transform body;
		private Transform leftHand;
		private Transform rightHand;
		private Transform leftLeg;
		private Transform rightLeg;
		private Transform pelvis;

		private Vector3 prevBodyPos;

		private Vector3 prevLeftLegPos = default(Vector3);
		private Vector3 prevRightLegPos = default(Vector3);
		private Quaternion prevLeftLegRot = default(Quaternion);
		private Quaternion prevRightLegRot = default(Quaternion);

		private Vector3 prevPelvisPos = default(Vector3);
		private Quaternion prevPelvisRot = default(Quaternion);

		private VRIK vrik;
		private IKManagerAdvanced ikManagerAdvanced;
		private TrackedDeviceManager trackedDevices;
		private VRPlatformHelper vrPlatformHelper;
		private Animator animator;
		private PoseManager poseManager;

		#region Behaviour Lifecycle
		#pragma warning disable IDE0051

		private void Start()
		{
			vrik = GetComponentInChildren<VRIK>();
			ikManagerAdvanced = GetComponentInChildren<IKManagerAdvanced>();
			animator = GetComponentInChildren<Animator>();
			poseManager = GetComponentInChildren<PoseManager>();

			trackedDevices = PersistentSingleton<TrackedDeviceManager>.instance;
			vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

			trackedDevices.DeviceAdded += (device) => UpdateVrikReferences();
			trackedDevices.DeviceRemoved += (device) => UpdateVrikReferences();

			head = gameObject.transform.Find("Head");
			body = gameObject.transform.Find("Body");
			leftHand = gameObject.transform.Find("LeftHand");
			rightHand = gameObject.transform.Find("RightHand");
			leftLeg = gameObject.transform.Find("LeftLeg");
			rightLeg = gameObject.transform.Find("RightLeg");
			pelvis = gameObject.transform.Find("Pelvis");

			UpdateVrikReferences();
		}

		private void LateUpdate()
		{
			ApplyFingerTracking();

			try
			{
				TrackedDeviceState headPosRot = trackedDevices.Head;
				TrackedDeviceState leftPosRot = trackedDevices.LeftHand;
				TrackedDeviceState rightPosRot = trackedDevices.RightHand;

				if (head && headPosRot != null && headPosRot.NodeState.tracked)
				{
					head.position = headPosRot.Position;
					head.rotation = headPosRot.Rotation;
				}

				if (leftHand && leftPosRot != null && leftPosRot.NodeState.tracked)
				{
					leftHand.position = leftPosRot.Position;
					leftHand.rotation = leftPosRot.Rotation;

					vrPlatformHelper.AdjustPlatformSpecificControllerTransform(leftHand);
				}

				if (rightHand && rightPosRot != null && rightPosRot.NodeState.tracked)
				{
					rightHand.position = rightPosRot.Position;
					rightHand.rotation = rightPosRot.Rotation;

					vrPlatformHelper.AdjustPlatformSpecificControllerTransform(rightHand);
				}

				TrackedDeviceState leftFoot = trackedDevices.LeftFoot;
				TrackedDeviceState rightFoot = trackedDevices.RightFoot;
				TrackedDeviceState waist = trackedDevices.Waist;

				if (leftLeg && leftFoot != null && leftFoot.NodeState.tracked)
				{
					var leftLegPosRot = trackedDevices.LeftFoot;
					var correction = LeftLegCorrection ?? default;

					prevLeftLegPos = Vector3.Lerp(prevLeftLegPos, leftLegPosRot.Position + correction.position, 15 * Time.deltaTime);
					prevLeftLegRot = Quaternion.Slerp(prevLeftLegRot, leftLegPosRot.Rotation * correction.rotation, 10 * Time.deltaTime);
					leftLeg.position = prevLeftLegPos;
					leftLeg.rotation = prevLeftLegRot;
				}

				if (rightLeg && rightFoot != null && rightFoot.NodeState.tracked)
				{
					var rightLegPosRot = trackedDevices.RightFoot;
					var correction = RightLegCorrection ?? default;

					prevRightLegPos = Vector3.Lerp(prevRightLegPos, rightLegPosRot.Position + correction.position, 15 * Time.deltaTime);
					prevRightLegRot = Quaternion.Slerp(prevRightLegRot, rightLegPosRot.Rotation * correction.rotation, 10 * Time.deltaTime);
					rightLeg.position = prevRightLegPos;
					rightLeg.rotation = prevRightLegRot;
				}

				if (pelvis && waist != null && waist.NodeState.tracked)
				{
					var pelvisPosRot = trackedDevices.Waist;
					var correction = PelvisCorrection ?? default;

					prevPelvisPos = Vector3.Lerp(prevPelvisPos, pelvisPosRot.Position + correction.position, 17 * Time.deltaTime);
					prevPelvisRot = Quaternion.Slerp(prevPelvisRot, pelvisPosRot.Rotation * correction.rotation, 13 * Time.deltaTime);
					pelvis.position = prevPelvisPos;
					pelvis.rotation = prevPelvisRot;
				}

				if (body == null) return;
				body.position = head.position - (head.transform.up * 0.1f);

				var vel = new Vector3(body.transform.localPosition.x - prevBodyPos.x, 0.0f,
					body.localPosition.z - prevBodyPos.z);

				var rot = Quaternion.Euler(0.0f, head.localEulerAngles.y, 0.0f);
				var tiltAxis = Vector3.Cross(gameObject.transform.up, vel);
				body.localRotation = Quaternion.Lerp(body.localRotation,
					Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
					Time.deltaTime * 10.0f);

				prevBodyPos = body.transform.localPosition;
			}
			catch (Exception e)
			{
				Plugin.Logger.Error($"{e.Message}\n{e.StackTrace}");
			}
		}

		#pragma warning restore IDE0051
		#endregion

		private void UpdateVrikReferences()
		{
			if (!ikManagerAdvanced) return;

			Plugin.Logger.Info("Tracking device change detected, updating VRIK references");

			if (trackedDevices.LeftFoot.Found)
			{
				vrik.solver.leftLeg.target = ikManagerAdvanced.LeftLeg_target;
				vrik.solver.leftLeg.positionWeight = ikManagerAdvanced.LeftLeg_positionWeight;
				vrik.solver.leftLeg.rotationWeight = ikManagerAdvanced.LeftLeg_rotationWeight;
			}
			else
			{
				vrik.solver.leftLeg.target = null;
				vrik.solver.leftLeg.positionWeight = 0;
				vrik.solver.leftLeg.rotationWeight = 0;
			}

			if (trackedDevices.RightFoot.Found)
			{
				vrik.solver.rightLeg.target = ikManagerAdvanced.RightLeg_target;
				vrik.solver.rightLeg.positionWeight = ikManagerAdvanced.RightLeg_positionWeight;
				vrik.solver.rightLeg.rotationWeight = ikManagerAdvanced.RightLeg_rotationWeight;
			}
			else
			{
				vrik.solver.rightLeg.target = null;
				vrik.solver.rightLeg.positionWeight = 0;
				vrik.solver.rightLeg.rotationWeight = 0;
			}

			if (trackedDevices.Waist.Found)
			{
				vrik.solver.spine.pelvisTarget = ikManagerAdvanced.Spine_pelvisTarget;
				vrik.solver.spine.pelvisPositionWeight = ikManagerAdvanced.Spine_pelvisPositionWeight;
				vrik.solver.spine.pelvisRotationWeight = ikManagerAdvanced.Spine_pelvisRotationWeight;
				vrik.solver.plantFeet = false;
			}
			else
			{
				vrik.solver.spine.pelvisTarget = null;
				vrik.solver.spine.pelvisPositionWeight = 0;
				vrik.solver.spine.pelvisRotationWeight = 0;
				vrik.solver.plantFeet = true;
			}
		}

		public void ApplyFingerTracking()
		{
			if (poseManager == null) return;

			if (Plugin.LeftHandAnimAction != null)
			{
				try
				{
					SkeletalSummaryData leftHandAnim = Plugin.LeftHandAnimAction.GetSummaryData();

					ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       poseManager.openHand_LeftThumbProximal,       poseManager.closedHand_LeftThumbProximal,       leftHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   poseManager.openHand_LeftThumbIntermediate,   poseManager.closedHand_LeftThumbIntermediate,   leftHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         poseManager.openHand_LeftThumbDistal,         poseManager.closedHand_LeftThumbDistal,         leftHandAnim.ThumbCurl * 2);

					ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       poseManager.openHand_LeftIndexProximal,       poseManager.closedHand_LeftIndexProximal,       leftHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   poseManager.openHand_LeftIndexIntermediate,   poseManager.closedHand_LeftIndexIntermediate,   leftHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         poseManager.openHand_LeftIndexDistal,         poseManager.closedHand_LeftIndexDistal,         leftHandAnim.IndexCurl);

					ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      poseManager.openHand_LeftMiddleProximal,      poseManager.closedHand_LeftMiddleProximal,      leftHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  poseManager.openHand_LeftMiddleIntermediate,  poseManager.closedHand_LeftMiddleIntermediate,  leftHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        poseManager.openHand_LeftMiddleDistal,        poseManager.closedHand_LeftMiddleDistal,        leftHandAnim.MiddleCurl);

					ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        poseManager.openHand_LeftRingProximal,        poseManager.closedHand_LeftRingProximal,        leftHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    poseManager.openHand_LeftRingIntermediate,    poseManager.closedHand_LeftRingIntermediate,    leftHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          poseManager.openHand_LeftRingDistal,          poseManager.closedHand_LeftRingDistal,          leftHandAnim.RingCurl);

					ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      poseManager.openHand_LeftLittleProximal,      poseManager.closedHand_LeftLittleProximal,      leftHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  poseManager.openHand_LeftLittleIntermediate,  poseManager.closedHand_LeftLittleIntermediate,  leftHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        poseManager.openHand_LeftLittleDistal,        poseManager.closedHand_LeftLittleDistal,        leftHandAnim.LittleCurl);
				}
				catch (Exception) { }
			}

			if (Plugin.RightHandAnimAction != null)
			{
				try
				{
					SkeletalSummaryData rightHandAnim = Plugin.RightHandAnimAction.GetSummaryData();

					ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,      poseManager.openHand_RightThumbProximal,      poseManager.closedHand_RightThumbProximal,      rightHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,  poseManager.openHand_RightThumbIntermediate,  poseManager.closedHand_RightThumbIntermediate,  rightHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,        poseManager.openHand_RightThumbDistal,        poseManager.closedHand_RightThumbDistal,        rightHandAnim.ThumbCurl * 2);

					ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,      poseManager.openHand_RightIndexProximal,      poseManager.closedHand_RightIndexProximal,      rightHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,  poseManager.openHand_RightIndexIntermediate,  poseManager.closedHand_RightIndexIntermediate,  rightHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,        poseManager.openHand_RightIndexDistal,        poseManager.closedHand_RightIndexDistal,        rightHandAnim.IndexCurl);

					ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,     poseManager.openHand_RightMiddleProximal,     poseManager.closedHand_RightMiddleProximal,     rightHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate, poseManager.openHand_RightMiddleIntermediate, poseManager.closedHand_RightMiddleIntermediate, rightHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,       poseManager.openHand_RightMiddleDistal,       poseManager.closedHand_RightMiddleDistal,       rightHandAnim.MiddleCurl);

					ApplyBodyBonePose(HumanBodyBones.RightRingProximal,       poseManager.openHand_RightRingProximal,       poseManager.closedHand_RightRingProximal,       rightHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,   poseManager.openHand_RightRingIntermediate,   poseManager.closedHand_RightRingIntermediate,   rightHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.RightRingDistal,         poseManager.openHand_RightRingDistal,         poseManager.closedHand_RightRingDistal,         rightHandAnim.RingCurl);

					ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,     poseManager.openHand_RightLittleProximal,     poseManager.closedHand_RightLittleProximal,     rightHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate, poseManager.openHand_RightLittleIntermediate, poseManager.closedHand_RightLittleIntermediate, rightHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,       poseManager.openHand_RightLittleDistal,       poseManager.closedHand_RightLittleDistal,       rightHandAnim.LittleCurl);
				}
				catch (Exception) { }
			}
		}

		private void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float position)
		{
			if (animator == null) return;

			Transform boneTransform = animator.GetBoneTransform(bodyBone);

			if (!boneTransform) return;

			boneTransform.localPosition = Vector3.Lerp(open.position, closed.position, position);
			boneTransform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, position);
		}
	}
}
