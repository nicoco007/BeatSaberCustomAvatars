using AvatarScriptPack;
using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarBehaviour : MonoBehaviour
	{
		public static PosRot? LeftLegCorrection { get; set; }
		public static PosRot? RightLegCorrection { get; set; }
		public static PosRot? PelvisCorrection { get; set; }

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
		private OpenVRInputManager _inputManager;

		public void Start()
		{
			_vrik = GetComponentInChildren<VRIK>();
			_ikManagerAdvanced = GetComponentInChildren<IKManagerAdvanced>();
			_trackedDevices = PersistentSingleton<TrackedDeviceManager>.instance;
			_vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

			_animator = GetComponentInChildren<Animator>();
			_poseManager = GetComponentInChildren<PoseManager>();
			_inputManager = PersistentSingleton<OpenVRInputManager>.instance;

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

		private void UpdateVrikReferences()
		{
			Plugin.Logger.Info("Tracking device change detected, updating VRIK references");

			if (_trackedDevices.LeftFoot.Found)
			{
				_vrik.solver.leftLeg.positionWeight = _ikManagerAdvanced.LeftLeg_positionWeight;
				_vrik.solver.leftLeg.rotationWeight = _ikManagerAdvanced.LeftLeg_rotationWeight;
			}
			else
			{
				_vrik.solver.leftLeg.positionWeight = 0;
				_vrik.solver.leftLeg.rotationWeight = 0;
			}

			if (_trackedDevices.RightFoot.Found)
			{
				_vrik.solver.rightLeg.positionWeight = _ikManagerAdvanced.RightLeg_positionWeight;
				_vrik.solver.rightLeg.rotationWeight = _ikManagerAdvanced.RightLeg_positionWeight;
			}
			else
			{
				_vrik.solver.rightLeg.positionWeight = 0;
				_vrik.solver.rightLeg.rotationWeight = 0;
			}

			if (_trackedDevices.Waist.Found)
			{
				_vrik.solver.spine.pelvisPositionWeight = _ikManagerAdvanced.Spine_pelvisPositionWeight;
				_vrik.solver.spine.pelvisRotationWeight = _ikManagerAdvanced.Spine_pelvisRotationWeight;
				_vrik.solver.plantFeet = false;
			}
			else
			{
				_vrik.solver.spine.pelvisPositionWeight = 0;
				_vrik.solver.spine.pelvisRotationWeight = 0;
				_vrik.solver.plantFeet = true;
			}
		}

		public void Update()
		{
			GetComponentInChildren<VRIK>().transform.localScale = Vector3.one * Plugin.PLAYER_SCALE;
		}

		public void ApplyFingerTracking()
		{
			if (_poseManager == null || _inputManager == null) return;

			ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       _poseManager.OpenHand_Left_ThumbProximal,       _poseManager.ClosedHand_Left_ThumbProximal,       _inputManager.LeftHandAnim.flFingerCurl0 * 2);
			ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   _poseManager.OpenHand_Left_ThumbIntermediate,   _poseManager.ClosedHand_Left_ThumbIntermediate,   _inputManager.LeftHandAnim.flFingerCurl0 * 2);
			ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         _poseManager.OpenHand_Left_ThumbDistal,         _poseManager.ClosedHand_Left_ThumbDistal,         _inputManager.LeftHandAnim.flFingerCurl0 * 2);

			ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       _poseManager.OpenHand_Left_IndexProximal,       _poseManager.ClosedHand_Left_IndexProximal,       _inputManager.LeftHandAnim.flFingerCurl1);
			ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   _poseManager.OpenHand_Left_IndexIntermediate,   _poseManager.ClosedHand_Left_IndexIntermediate,   _inputManager.LeftHandAnim.flFingerCurl1);
			ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         _poseManager.OpenHand_Left_IndexDistal,         _poseManager.ClosedHand_Left_IndexDistal,         _inputManager.LeftHandAnim.flFingerCurl1);

			ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      _poseManager.OpenHand_Left_MiddleProximal,      _poseManager.ClosedHand_Left_MiddleProximal,      _inputManager.LeftHandAnim.flFingerCurl2);
			ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  _poseManager.OpenHand_Left_MiddleIntermediate,  _poseManager.ClosedHand_Left_MiddleIntermediate,  _inputManager.LeftHandAnim.flFingerCurl2);
			ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        _poseManager.OpenHand_Left_MiddleDistal,        _poseManager.ClosedHand_Left_MiddleDistal,        _inputManager.LeftHandAnim.flFingerCurl2);

			ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        _poseManager.OpenHand_Left_RingProximal,        _poseManager.ClosedHand_Left_RingProximal,        _inputManager.LeftHandAnim.flFingerCurl3);
			ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    _poseManager.OpenHand_Left_RingIntermediate,    _poseManager.ClosedHand_Left_RingIntermediate,    _inputManager.LeftHandAnim.flFingerCurl3);
			ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          _poseManager.OpenHand_Left_RingDistal,          _poseManager.ClosedHand_Left_RingDistal,          _inputManager.LeftHandAnim.flFingerCurl3);

			ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      _poseManager.OpenHand_Left_LittleProximal,      _poseManager.ClosedHand_Left_LittleProximal,      _inputManager.LeftHandAnim.flFingerCurl4);
			ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  _poseManager.OpenHand_Left_LittleIntermediate,  _poseManager.ClosedHand_Left_LittleIntermediate,  _inputManager.LeftHandAnim.flFingerCurl4);
			ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        _poseManager.OpenHand_Left_LittleDistal,        _poseManager.ClosedHand_Left_LittleDistal,        _inputManager.LeftHandAnim.flFingerCurl4);

			ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,      _poseManager.OpenHand_Right_ThumbProximal,      _poseManager.ClosedHand_Right_ThumbProximal,      _inputManager.RightHandAnim.flFingerCurl0 * 2);
			ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,  _poseManager.OpenHand_Right_ThumbIntermediate,  _poseManager.ClosedHand_Right_ThumbIntermediate,  _inputManager.RightHandAnim.flFingerCurl0 * 2);
			ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,        _poseManager.OpenHand_Right_ThumbDistal,        _poseManager.ClosedHand_Right_ThumbDistal,        _inputManager.RightHandAnim.flFingerCurl0 * 2);

			ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,      _poseManager.OpenHand_Right_IndexProximal,      _poseManager.ClosedHand_Right_IndexProximal,      _inputManager.RightHandAnim.flFingerCurl1);
			ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,  _poseManager.OpenHand_Right_IndexIntermediate,  _poseManager.ClosedHand_Right_IndexIntermediate,  _inputManager.RightHandAnim.flFingerCurl1);
			ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,        _poseManager.OpenHand_Right_IndexDistal,        _poseManager.ClosedHand_Right_IndexDistal,        _inputManager.RightHandAnim.flFingerCurl1);

			ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,     _poseManager.OpenHand_Right_MiddleProximal,     _poseManager.ClosedHand_Right_MiddleProximal,     _inputManager.RightHandAnim.flFingerCurl2);
			ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate, _poseManager.OpenHand_Right_MiddleIntermediate, _poseManager.ClosedHand_Right_MiddleIntermediate, _inputManager.RightHandAnim.flFingerCurl2);
			ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,       _poseManager.OpenHand_Right_MiddleDistal,       _poseManager.ClosedHand_Right_MiddleDistal,       _inputManager.RightHandAnim.flFingerCurl2);

			ApplyBodyBonePose(HumanBodyBones.RightRingProximal,       _poseManager.OpenHand_Right_RingProximal,       _poseManager.ClosedHand_Right_RingProximal,       _inputManager.RightHandAnim.flFingerCurl3);
			ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,   _poseManager.OpenHand_Right_RingIntermediate,   _poseManager.ClosedHand_Right_RingIntermediate,   _inputManager.RightHandAnim.flFingerCurl3);
			ApplyBodyBonePose(HumanBodyBones.RightRingDistal,         _poseManager.OpenHand_Right_RingDistal,         _poseManager.ClosedHand_Right_RingDistal,         _inputManager.RightHandAnim.flFingerCurl3);

			ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,     _poseManager.OpenHand_Right_LittleProximal,     _poseManager.ClosedHand_Right_LittleProximal,     _inputManager.RightHandAnim.flFingerCurl4);
			ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate, _poseManager.OpenHand_Right_LittleIntermediate, _poseManager.ClosedHand_Right_LittleIntermediate, _inputManager.RightHandAnim.flFingerCurl4);
			ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,       _poseManager.OpenHand_Right_LittleDistal,       _poseManager.ClosedHand_Right_LittleDistal,       _inputManager.RightHandAnim.flFingerCurl4);
		}

		public void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float position)
		{
			if (_animator == null) return;

			Transform transform = _animator.GetBoneTransform(bodyBone);

			transform.localPosition = Vector3.Lerp(open.position, closed.position, position);
			transform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, position);
		}

		private void LateUpdate()
		{
			ApplyFingerTracking();

			try
			{
				TrackedDeviceState headPosRot = _trackedDevices.Head;
				TrackedDeviceState leftPosRot = _trackedDevices.LeftHand;
				TrackedDeviceState rightPosRot = _trackedDevices.RightHand;

				if (headPosRot.NodeState.tracked)
				{
					_head.position = headPosRot.Position;
					_head.rotation = headPosRot.Rotation;
				}

				if (leftPosRot.NodeState.tracked)
				{
					_leftHand.position = leftPosRot.Position;
					_leftHand.rotation = leftPosRot.Rotation;

					_vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_leftHand);
				}

				if (rightPosRot.NodeState.tracked)
				{
					_rightHand.position = rightPosRot.Position;
					_rightHand.rotation = rightPosRot.Rotation;

					_vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_rightHand);
				}

				if (_leftLeg != null && _trackedDevices.LeftFoot.NodeState.tracked)
				{
					var leftLegPosRot = _trackedDevices.LeftFoot;
					var correction = LeftLegCorrection ?? default;

					_prevLeftLegPos = Vector3.Lerp(_prevLeftLegPos, leftLegPosRot.Position + correction.Position, 15 * Time.deltaTime);
					_prevLeftLegRot = Quaternion.Slerp(_prevLeftLegRot, leftLegPosRot.Rotation * correction.Rotation, 10 * Time.deltaTime);
					_leftLeg.position = _prevLeftLegPos;
					_leftLeg.rotation = _prevLeftLegRot;
				}

				if (_rightLeg != null && _trackedDevices.RightFoot.NodeState.tracked)
				{
					var rightLegPosRot = _trackedDevices.RightFoot;
					var correction = RightLegCorrection ?? default;

					_prevRightLegPos = Vector3.Lerp(_prevRightLegPos, rightLegPosRot.Position + correction.Position, 15 * Time.deltaTime);
					_prevRightLegRot = Quaternion.Slerp(_prevRightLegRot, rightLegPosRot.Rotation * correction.Rotation, 10 * Time.deltaTime);
					_rightLeg.position = _prevRightLegPos;
					_rightLeg.rotation = _prevRightLegRot;
				}

				if (_pelvis != null && _trackedDevices.Waist.NodeState.tracked)
				{
					var pelvisPosRot = _trackedDevices.Waist;
					var correction = PelvisCorrection ?? default;

					_prevPelvisPos = Vector3.Lerp(_prevPelvisPos, pelvisPosRot.Position + correction.Position, 17 * Time.deltaTime);
					_prevPelvisRot = Quaternion.Slerp(_prevPelvisRot, pelvisPosRot.Rotation * correction.Rotation, 13 * Time.deltaTime);
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
			} catch(Exception e)
			{
				Plugin.Logger.Error($"{e.Message}\n{e.StackTrace}");
			}
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
