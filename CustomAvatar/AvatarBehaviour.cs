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

		private Transform _head;
		private Transform _body;
		private Transform _leftHand;
		private Transform _rightHand;
		private Transform _leftLeg;
		private Transform _rightLeg;
		private Transform _pelvis;

		private Vector3 _prevBodyPos;

		private Vector3 _prevLeftLegPos = default(Vector3);
		private Vector3 _prevRightLegPos = default(Vector3);
		private Quaternion _prevLeftLegRot = default(Quaternion);
		private Quaternion _prevRightLegRot = default(Quaternion);

		private Vector3 _prevPelvisPos = default(Vector3);
		private Quaternion _prevPelvisRot = default(Quaternion);

		private VRIK _vrik;
		private IKManagerAdvanced _ikManagerAdvanced;
		private TrackedDeviceManager _trackedDevices;
		private VRPlatformHelper _vrPlatformHelper;
		private Animator _animator;
		private PoseManager _poseManager;

		#region Behaviour Lifecycle
		#pragma warning disable IDE0051

		private void Start()
		{
			_vrik = GetComponentInChildren<VRIK>();
			_ikManagerAdvanced = GetComponentInChildren<IKManagerAdvanced>();
			_animator = GetComponentInChildren<Animator>();
			_poseManager = GetComponentInChildren<PoseManager>();

			_trackedDevices = PersistentSingleton<TrackedDeviceManager>.instance;
			_vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

			_trackedDevices.DeviceAdded += (device) => UpdateVrikReferences();
			_trackedDevices.DeviceRemoved += (device) => UpdateVrikReferences();

			_head = GetHeadTransform();
			_body = gameObject.transform.Find("Body");
			_leftHand = gameObject.transform.Find("LeftHand");
			_rightHand = gameObject.transform.Find("RightHand");
			_leftLeg = gameObject.transform.Find("LeftLeg");
			_rightLeg = gameObject.transform.Find("RightLeg");
			_pelvis = gameObject.transform.Find("Pelvis");

			UpdateVrikReferences();
		}

		private void LateUpdate()
		{
			ApplyFingerTracking();

			try
			{
				TrackedDeviceState headPosRot = _trackedDevices.Head;
				TrackedDeviceState leftPosRot = _trackedDevices.LeftHand;
				TrackedDeviceState rightPosRot = _trackedDevices.RightHand;

				if (_head && headPosRot != null && headPosRot.NodeState.tracked)
				{
					_head.position = headPosRot.Position;
					_head.rotation = headPosRot.Rotation;
				}

				if (_leftHand && leftPosRot != null && leftPosRot.NodeState.tracked)
				{
					_leftHand.position = leftPosRot.Position;
					_leftHand.rotation = leftPosRot.Rotation;

					_vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_leftHand);
				}

				if (_rightHand && rightPosRot != null && rightPosRot.NodeState.tracked)
				{
					_rightHand.position = rightPosRot.Position;
					_rightHand.rotation = rightPosRot.Rotation;

					_vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_rightHand);
				}

				TrackedDeviceState leftFoot = _trackedDevices.LeftFoot;
				TrackedDeviceState rightFoot = _trackedDevices.RightFoot;
				TrackedDeviceState pelvis = _trackedDevices.Waist;

				if (_leftLeg && leftFoot != null && leftFoot.NodeState.tracked)
				{
					var leftLegPosRot = _trackedDevices.LeftFoot;
					var correction = LeftLegCorrection ?? default;

					_prevLeftLegPos = Vector3.Lerp(_prevLeftLegPos, leftLegPosRot.Position + correction.position, 15 * Time.deltaTime);
					_prevLeftLegRot = Quaternion.Slerp(_prevLeftLegRot, leftLegPosRot.Rotation * correction.rotation, 10 * Time.deltaTime);
					_leftLeg.position = _prevLeftLegPos;
					_leftLeg.rotation = _prevLeftLegRot;
				}

				if (_rightLeg && rightFoot != null && rightFoot.NodeState.tracked)
				{
					var rightLegPosRot = _trackedDevices.RightFoot;
					var correction = RightLegCorrection ?? default;

					_prevRightLegPos = Vector3.Lerp(_prevRightLegPos, rightLegPosRot.Position + correction.position, 15 * Time.deltaTime);
					_prevRightLegRot = Quaternion.Slerp(_prevRightLegRot, rightLegPosRot.Rotation * correction.rotation, 10 * Time.deltaTime);
					_rightLeg.position = _prevRightLegPos;
					_rightLeg.rotation = _prevRightLegRot;
				}

				if (_pelvis && pelvis != null && pelvis.NodeState.tracked)
				{
					var pelvisPosRot = _trackedDevices.Waist;
					var correction = PelvisCorrection ?? default;

					_prevPelvisPos = Vector3.Lerp(_prevPelvisPos, pelvisPosRot.Position + correction.position, 17 * Time.deltaTime);
					_prevPelvisRot = Quaternion.Slerp(_prevPelvisRot, pelvisPosRot.Rotation * correction.rotation, 13 * Time.deltaTime);
					_pelvis.position = _prevPelvisPos;
					_pelvis.rotation = _prevPelvisRot;
				}

				if (_body == null) return;
				_body.position = _head.position - (_head.transform.up * 0.1f);

				var vel = new Vector3(_body.transform.localPosition.x - _prevBodyPos.x, 0.0f,
					_body.localPosition.z - _prevBodyPos.z);

				var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
				var tiltAxis = Vector3.Cross(gameObject.transform.up, vel);
				_body.localRotation = Quaternion.Lerp(_body.localRotation,
					Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
					Time.deltaTime * 10.0f);

				_prevBodyPos = _body.transform.localPosition;
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
			if (!_ikManagerAdvanced) return;

			Plugin.Logger.Info("Tracking device change detected, updating VRIK references");

			if (_trackedDevices.LeftFoot.Found)
			{
				_vrik.solver.leftLeg.target = _ikManagerAdvanced.LeftLeg_target;
				_vrik.solver.leftLeg.positionWeight = _ikManagerAdvanced.LeftLeg_positionWeight;
				_vrik.solver.leftLeg.rotationWeight = _ikManagerAdvanced.LeftLeg_rotationWeight;
			}
			else
			{
				_vrik.solver.leftLeg.target = null;
				_vrik.solver.leftLeg.positionWeight = 0;
				_vrik.solver.leftLeg.rotationWeight = 0;
			}

			if (_trackedDevices.RightFoot.Found)
			{
				_vrik.solver.rightLeg.target = _ikManagerAdvanced.RightLeg_target;
				_vrik.solver.rightLeg.positionWeight = _ikManagerAdvanced.RightLeg_positionWeight;
				_vrik.solver.rightLeg.rotationWeight = _ikManagerAdvanced.RightLeg_rotationWeight;
			}
			else
			{
				_vrik.solver.rightLeg.target = null;
				_vrik.solver.rightLeg.positionWeight = 0;
				_vrik.solver.rightLeg.rotationWeight = 0;
			}

			if (_trackedDevices.Waist.Found)
			{
				_vrik.solver.spine.pelvisTarget = _ikManagerAdvanced.Spine_pelvisTarget;
				_vrik.solver.spine.pelvisPositionWeight = _ikManagerAdvanced.Spine_pelvisPositionWeight;
				_vrik.solver.spine.pelvisRotationWeight = _ikManagerAdvanced.Spine_pelvisRotationWeight;
				_vrik.solver.plantFeet = false;
			}
			else
			{
				_vrik.solver.spine.pelvisTarget = null;
				_vrik.solver.spine.pelvisPositionWeight = 0;
				_vrik.solver.spine.pelvisRotationWeight = 0;
				_vrik.solver.plantFeet = true;
			}
		}

		public void ApplyFingerTracking()
		{
			if (_poseManager == null) return;

			if (Plugin.LeftHandAnimAction != null)
			{
				try
				{
					SkeletalSummaryData leftHandAnim = Plugin.LeftHandAnimAction.GetSummaryData();

					ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       _poseManager.openHand_LeftThumbProximal,       _poseManager.closedHand_LeftThumbProximal,       leftHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   _poseManager.openHand_LeftThumbIntermediate,   _poseManager.closedHand_LeftThumbIntermediate,   leftHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         _poseManager.openHand_LeftThumbDistal,         _poseManager.closedHand_LeftThumbDistal,         leftHandAnim.ThumbCurl * 2);

					ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       _poseManager.openHand_LeftIndexProximal,       _poseManager.closedHand_LeftIndexProximal,       leftHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   _poseManager.openHand_LeftIndexIntermediate,   _poseManager.closedHand_LeftIndexIntermediate,   leftHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         _poseManager.openHand_LeftIndexDistal,         _poseManager.closedHand_LeftIndexDistal,         leftHandAnim.IndexCurl);

					ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      _poseManager.openHand_LeftMiddleProximal,      _poseManager.closedHand_LeftMiddleProximal,      leftHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  _poseManager.openHand_LeftMiddleIntermediate,  _poseManager.closedHand_LeftMiddleIntermediate,  leftHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        _poseManager.openHand_LeftMiddleDistal,        _poseManager.closedHand_LeftMiddleDistal,        leftHandAnim.MiddleCurl);

					ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        _poseManager.openHand_LeftRingProximal,        _poseManager.closedHand_LeftRingProximal,        leftHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    _poseManager.openHand_LeftRingIntermediate,    _poseManager.closedHand_LeftRingIntermediate,    leftHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          _poseManager.openHand_LeftRingDistal,          _poseManager.closedHand_LeftRingDistal,          leftHandAnim.RingCurl);

					ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      _poseManager.openHand_LeftLittleProximal,      _poseManager.closedHand_LeftLittleProximal,      leftHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  _poseManager.openHand_LeftLittleIntermediate,  _poseManager.closedHand_LeftLittleIntermediate,  leftHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        _poseManager.openHand_LeftLittleDistal,        _poseManager.closedHand_LeftLittleDistal,        leftHandAnim.LittleCurl);
				}
				catch (Exception) { }
			}

			if (Plugin.RightHandAnimAction != null)
			{
				try
				{
					SkeletalSummaryData rightHandAnim = Plugin.RightHandAnimAction.GetSummaryData();

					ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,      _poseManager.openHand_RightThumbProximal,      _poseManager.closedHand_RightThumbProximal,      rightHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,  _poseManager.openHand_RightThumbIntermediate,  _poseManager.closedHand_RightThumbIntermediate,  rightHandAnim.ThumbCurl * 2);
					ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,        _poseManager.openHand_RightThumbDistal,        _poseManager.closedHand_RightThumbDistal,        rightHandAnim.ThumbCurl * 2);

					ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,      _poseManager.openHand_RightIndexProximal,      _poseManager.closedHand_RightIndexProximal,      rightHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,  _poseManager.openHand_RightIndexIntermediate,  _poseManager.closedHand_RightIndexIntermediate,  rightHandAnim.IndexCurl);
					ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,        _poseManager.openHand_RightIndexDistal,        _poseManager.closedHand_RightIndexDistal,        rightHandAnim.IndexCurl);

					ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,     _poseManager.openHand_RightMiddleProximal,     _poseManager.closedHand_RightMiddleProximal,     rightHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate, _poseManager.openHand_RightMiddleIntermediate, _poseManager.closedHand_RightMiddleIntermediate, rightHandAnim.MiddleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,       _poseManager.openHand_RightMiddleDistal,       _poseManager.closedHand_RightMiddleDistal,       rightHandAnim.MiddleCurl);

					ApplyBodyBonePose(HumanBodyBones.RightRingProximal,       _poseManager.openHand_RightRingProximal,       _poseManager.closedHand_RightRingProximal,       rightHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,   _poseManager.openHand_RightRingIntermediate,   _poseManager.closedHand_RightRingIntermediate,   rightHandAnim.RingCurl);
					ApplyBodyBonePose(HumanBodyBones.RightRingDistal,         _poseManager.openHand_RightRingDistal,         _poseManager.closedHand_RightRingDistal,         rightHandAnim.RingCurl);

					ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,     _poseManager.openHand_RightLittleProximal,     _poseManager.closedHand_RightLittleProximal,     rightHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate, _poseManager.openHand_RightLittleIntermediate, _poseManager.closedHand_RightLittleIntermediate, rightHandAnim.LittleCurl);
					ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,       _poseManager.openHand_RightLittleDistal,       _poseManager.closedHand_RightLittleDistal,       rightHandAnim.LittleCurl);
				}
				catch (Exception) { }
			}
		}

		private void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float position)
		{
			if (_animator == null) return;

			Transform boneTransform = _animator.GetBoneTransform(bodyBone);

			if (!boneTransform) return;

			boneTransform.localPosition = Vector3.Lerp(open.position, closed.position, position);
			boneTransform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, position);
		}

		private Transform GetHeadTransform()
		{
			var descriptor = GetComponent<AvatarDescriptor>();
			if (descriptor != null)
			{
				//if (descriptor.ViewPoint != null) return descriptor.ViewPoint;
			}

			return gameObject.transform.Find("Head");
		}
	}
}
