using AvatarScriptPack;
using CustomAvatar.Tracking;
using DynamicOpenVR.IO;
using System;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
    public class AvatarBehaviour : MonoBehaviour
    {
        public static Pose? LeftLegCorrection { get; set; }
        public static Pose? RightLegCorrection { get; set; }
        public static Pose? PelvisCorrection { get; set; }
        public Vector3 Position
        {
	        get => transform.position - initialPosition;
	        set => transform.position = initialPosition + value;
        }

        public float Scale
        {
	        get => transform.localScale.y / initialScale.y;
	        set
	        {
		        transform.localScale = initialScale * value;
		        Plugin.Logger.Info("Avatar resized with scale: " + value);
	        }
        }
		
        private Vector3 initialPosition;
        private Vector3 initialScale;

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

		private void Awake()
		{
			initialPosition = transform.position;
			initialScale = transform.localScale;
		}

        private void Start()
        {
			Console.WriteLine(initialPosition);
			Console.WriteLine(initialScale);

            vrik = GetComponentInChildren<VRIK>();
            ikManagerAdvanced = GetComponentInChildren<IKManagerAdvanced>();
            animator = GetComponentInChildren<Animator>();
            poseManager = GetComponentInChildren<PoseManager>();

            trackedDevices = PersistentSingleton<TrackedDeviceManager>.instance;
            vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

            trackedDevices.DeviceAdded += (device) => UpdateVrikReferences();
            trackedDevices.DeviceRemoved += (device) => UpdateVrikReferences();

            head = transform.Find("Head");
            body = transform.Find("Body");
            leftHand = transform.Find("LeftHand");
            rightHand = transform.Find("RightHand");
            leftLeg = transform.Find("LeftLeg");
            rightLeg = transform.Find("RightLeg");
            pelvis = transform.Find("Pelvis");

            UpdateVrikReferences();
        }

        private void LateUpdate()
        {
            ApplyFingerTracking();

            try
            {
                TrackedDeviceState headPose = trackedDevices.Head;
                TrackedDeviceState leftPose = trackedDevices.LeftHand;
                TrackedDeviceState rightPose = trackedDevices.RightHand;

                if (head && headPose != null && headPose.NodeState.tracked)
                {
                    head.position = headPose.Position;
                    head.rotation = headPose.Rotation;
                }

                if (leftHand && leftPose != null && leftPose.NodeState.tracked)
                {
                    leftHand.position = leftPose.Position;
                    leftHand.rotation = leftPose.Rotation;

                    vrPlatformHelper.AdjustPlatformSpecificControllerTransform(leftHand);
                }

                if (rightHand && rightPose != null && rightPose.NodeState.tracked)
                {
                    rightHand.position = rightPose.Position;
                    rightHand.rotation = rightPose.Rotation;

                    vrPlatformHelper.AdjustPlatformSpecificControllerTransform(rightHand);
                }

                TrackedDeviceState leftLegTracker = trackedDevices.LeftFoot;
                TrackedDeviceState rightLegTracker = trackedDevices.RightFoot;
                TrackedDeviceState pelvisTracker = trackedDevices.Waist;

                float playerEyeHeight = BeatSaberUtil.GetPlayerEyeHeight();
                float positionScale = (playerEyeHeight - Position.y) / playerEyeHeight;

                if (leftLeg && leftLegTracker != null && leftLegTracker.NodeState.tracked)
                {
                    var leftLegPose = trackedDevices.LeftFoot;
                    var correction = LeftLegCorrection ?? default;

                    prevLeftLegPos = Vector3.Lerp(prevLeftLegPos, (leftLegPose.Position + correction.position) * positionScale + Position, 15 * Time.deltaTime);
                    prevLeftLegRot = Quaternion.Slerp(prevLeftLegRot, leftLegPose.Rotation * correction.rotation, 10 * Time.deltaTime);
                    leftLeg.position = prevLeftLegPos;
                    leftLeg.rotation = prevLeftLegRot;
                }

                if (rightLeg && rightLegTracker != null && rightLegTracker.NodeState.tracked)
                {
                    var rightLegPose = trackedDevices.RightFoot;
                    var correction = RightLegCorrection ?? default;

                    prevRightLegPos = Vector3.Lerp(prevRightLegPos, (rightLegPose.Position + correction.position) * positionScale + Position, 15 * Time.deltaTime);
                    prevRightLegRot = Quaternion.Slerp(prevRightLegRot, rightLegPose.Rotation * correction.rotation, 10 * Time.deltaTime);
                    rightLeg.position = prevRightLegPos;
                    rightLeg.rotation = prevRightLegRot;
                }

                if (pelvis && pelvisTracker != null && pelvisTracker.NodeState.tracked)
                {
                    var pelvisPose = trackedDevices.Waist;
                    var correction = PelvisCorrection ?? default;

                    prevPelvisPos = Vector3.Lerp(prevPelvisPos, (pelvisPose.Position + correction.position) * positionScale + Position, 17 * Time.deltaTime);
                    prevPelvisRot = Quaternion.Slerp(prevPelvisRot, pelvisPose.Rotation * correction.rotation, 13 * Time.deltaTime);
                    pelvis.position = prevPelvisPos;
                    pelvis.rotation = prevPelvisRot;
                }

                if (body == null) return;
                body.position = head.position - (head.transform.up * 0.1f);

                var vel = new Vector3(body.transform.localPosition.x - prevBodyPos.x, 0.0f,
                    body.localPosition.z - prevBodyPos.z);

                var rot = Quaternion.Euler(0.0f, head.localEulerAngles.y, 0.0f);
                var tiltAxis = Vector3.Cross(transform.up, vel);
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
