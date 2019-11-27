using CustomAvatar.Tracking;
using DynamicOpenVR.IO;
using System;
using System.Reflection;
using RootMotion.FinalIK;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
    public class AvatarBehaviour : MonoBehaviour
    {
        public Vector3 position
        {
	        get => transform.position - _initialPosition;
	        set => transform.position = _initialPosition + value;
        }

        public float scale
        {
	        get => transform.localScale.y / _initialScale.y;
	        set
	        {
		        transform.localScale = _initialScale * value;
		        Plugin.logger.Info("Avatar resized with scale: " + value);
	        }
        }
		
        private Vector3 _initialPosition;
        private Vector3 _initialScale;

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

        private RootMotion.FinalIK.VRIK _vrik;
        private VRIKManager _vrikManager;
        private TrackedDeviceManager _trackedDevices;
        private VRPlatformHelper _vrPlatformHelper;
        private Animator _animator;
        private PoseManager _poseManager;

        private bool _isFingerTrackingSupported;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        private void Awake()
        {
			_initialPosition = transform.position;
			_initialScale = transform.localScale;

            _vrikManager = GetComponentInChildren<VRIKManager>();
            _vrik = GetComponentInChildren<VRIK>();
            _animator = GetComponentInChildren<Animator>();
            _poseManager = GetComponentInChildren<PoseManager>();

            if (_vrikManager && (!_vrik || _vrikManager.gameObject != _vrik.gameObject))
            {
                Destroy(_vrik);

                _vrik = _vrikManager.gameObject.AddComponent<VRIK>();
            }

            _isFingerTrackingSupported = _animator && _poseManager;

            foreach (TwistRelaxer twistRelaxer in GetComponentsInChildren<TwistRelaxer>())
            {
                twistRelaxer.ik = _vrik;
            }

            _trackedDevices = PersistentSingleton<TrackedDeviceManager>.instance;
            _vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

            _trackedDevices.DeviceAdded += OnTrackedDeviceAdded;
            _trackedDevices.DeviceRemoved += OnTrackedDeviceRemoved;

            _head = transform.Find("Head");
            _body = transform.Find("Body");
            _leftHand = transform.Find("LeftHand");
            _rightHand = transform.Find("RightHand");
            _leftLeg = transform.Find("LeftLeg");
            _rightLeg = transform.Find("RightLeg");
            _pelvis = transform.Find("Pelvis");

            SetVrikReferences();
		}

        private void LateUpdate()
        {
            if (_isFingerTrackingSupported)
            {
                ApplyFingerTracking();
            }

            try
            {
                TrackedDeviceState headPose = _trackedDevices.Head;
                TrackedDeviceState leftPose = _trackedDevices.LeftHand;
                TrackedDeviceState rightPose = _trackedDevices.RightHand;

                if (_head && headPose != null && headPose.NodeState.tracked)
                {
                    _head.position = headPose.Position;
                    _head.rotation = headPose.Rotation;
                }

                if (_leftHand && leftPose != null && leftPose.NodeState.tracked)
                {
                    _leftHand.position = leftPose.Position;
                    _leftHand.rotation = leftPose.Rotation;

                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_leftHand);
                }

                if (_rightHand && rightPose != null && rightPose.NodeState.tracked)
                {
                    _rightHand.position = rightPose.Position;
                    _rightHand.rotation = rightPose.Rotation;

                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_rightHand);
                }

                TrackedDeviceState leftLegTracker = _trackedDevices.LeftFoot;
                TrackedDeviceState rightLegTracker = _trackedDevices.RightFoot;
                TrackedDeviceState pelvisTracker = _trackedDevices.Waist;

                float playerEyeHeight = BeatSaberUtil.GetPlayerEyeHeight();
                float positionScale = (playerEyeHeight - position.y) / playerEyeHeight;

                if (_leftLeg && leftLegTracker != null && leftLegTracker.NodeState.tracked)
                {
                    var leftLegPose = _trackedDevices.LeftFoot;
                    var correction = SettingsManager.settings.fullBodyCalibration.leftLeg;

                    _prevLeftLegPos = Vector3.Lerp(_prevLeftLegPos, (leftLegPose.Position + correction.position) * positionScale + position, SettingsManager.settings.fullBodyMotionSmoothing.feet.position * Time.deltaTime);
                    _prevLeftLegRot = Quaternion.Slerp(_prevLeftLegRot, leftLegPose.Rotation * correction.rotation, SettingsManager.settings.fullBodyMotionSmoothing.feet.rotation * Time.deltaTime);
                    _leftLeg.position = _prevLeftLegPos;
                    _leftLeg.rotation = _prevLeftLegRot;
                }

                if (_rightLeg && rightLegTracker != null && rightLegTracker.NodeState.tracked)
                {
                    var rightLegPose = _trackedDevices.RightFoot;
                    var correction = SettingsManager.settings.fullBodyCalibration.rightLeg;

                    _prevRightLegPos = Vector3.Lerp(_prevRightLegPos, (rightLegPose.Position + correction.position) * positionScale + position, SettingsManager.settings.fullBodyMotionSmoothing.feet.position * Time.deltaTime);
                    _prevRightLegRot = Quaternion.Slerp(_prevRightLegRot, rightLegPose.Rotation * correction.rotation, SettingsManager.settings.fullBodyMotionSmoothing.feet.rotation * Time.deltaTime);
                    _rightLeg.position = _prevRightLegPos;
                    _rightLeg.rotation = _prevRightLegRot;
                }

                if (_pelvis && pelvisTracker != null && pelvisTracker.NodeState.tracked)
                {
                    var pelvisPose = _trackedDevices.Waist;
                    var correction = SettingsManager.settings.fullBodyCalibration.rightLeg;

                    _prevPelvisPos = Vector3.Lerp(_prevPelvisPos, (pelvisPose.Position + correction.position) * positionScale + position, SettingsManager.settings.fullBodyMotionSmoothing.waist.position * Time.deltaTime);
                    _prevPelvisRot = Quaternion.Slerp(_prevPelvisRot, pelvisPose.Rotation * correction.rotation, SettingsManager.settings.fullBodyMotionSmoothing.waist.rotation * Time.deltaTime);
                    _pelvis.position = _prevPelvisPos;
                    _pelvis.rotation = _prevPelvisRot;
                }

                if (!_body) return;
                _body.position = _head.position - (_head.transform.up * 0.1f);

                var vel = new Vector3(_body.transform.localPosition.x - _prevBodyPos.x, 0.0f,
                    _body.localPosition.z - _prevBodyPos.z);

                var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
                var tiltAxis = Vector3.Cross(transform.up, vel);
                _body.localRotation = Quaternion.Lerp(_body.localRotation,
                    Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                    Time.deltaTime * 10.0f);

                _prevBodyPos = _body.transform.localPosition;
            }
            catch (Exception e)
            {
                Plugin.logger.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            _trackedDevices.DeviceAdded -= OnTrackedDeviceAdded;
            _trackedDevices.DeviceRemoved -= OnTrackedDeviceRemoved;
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private void OnTrackedDeviceAdded(TrackedDeviceState device)
        {
            UpdateVrikReferences();
        }

        private void OnTrackedDeviceRemoved(TrackedDeviceState device)
        {
            UpdateVrikReferences();
        }

        private void SetVrikReferences()
        {
            if (!_vrik || !_vrikManager) return;

            foreach (FieldInfo sourceField in _vrikManager.GetType().GetFields())
            {
                string[] parts = sourceField.Name.Split('_');
                object target = _vrik;

                try
                {
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        target = target.GetType().GetField(parts[i])?.GetValue(target);

                        if (target == null)
                        {
                            Plugin.logger.Warn($"Target {parts[i]} is null");
                            break;
                        }
                    }

                    if (target == null) break;

                    FieldInfo targetField = target.GetType().GetField(parts[parts.Length - 1]);
                    object value = sourceField.GetValue(_vrikManager);

                    Plugin.logger.Debug($"Set {string.Join(".", parts)} = {value}");

                    if (targetField.FieldType.IsEnum)
                    {
                        Type sourceType = Enum.GetUnderlyingType(sourceField.FieldType);
                        Type targetType = Enum.GetUnderlyingType(targetField.FieldType);

                        if (sourceType != targetType)
                        {
                            Plugin.logger.Warn($"Underlying types for {sourceField.Name} ({sourceType}) and {targetField.Name} ({targetType}) are not the same");
                        }

                        Plugin.logger.Debug($"Converting enum value {sourceField.FieldType} ({sourceType}) -> {targetField.FieldType} ({targetType})");
                        targetField.SetValue(target, Convert.ChangeType(value, targetType));
                    }
                    else
                    {
                        if (sourceField.FieldType != targetField.FieldType)
                        {
                            Plugin.logger.Warn($"Types for {sourceField.Name} ({sourceField.FieldType}) and {targetField.Name} ({targetField.FieldType}) are not the same");
                        }

                        targetField.SetValue(target, value);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.logger.Error(ex);
                }
            }

            if (!_vrik.references.isFilled) _vrik.AutoDetectReferences();

            UpdateVrikReferences();
        }

        private void UpdateVrikReferences()
        {
            if (!_vrik || !_vrikManager) return;

            Plugin.logger.Info("Tracking device change detected, updating VRIK references");

            if (_trackedDevices.LeftFoot.Found)
            {
                _vrik.solver.leftLeg.target = _vrikManager.solver_leftLeg_target;
                _vrik.solver.leftLeg.positionWeight = _vrikManager.solver_leftLeg_positionWeight;
                _vrik.solver.leftLeg.rotationWeight = _vrikManager.solver_leftLeg_rotationWeight;
            }
            else
            {
                _vrik.solver.leftLeg.target = null;
                _vrik.solver.leftLeg.positionWeight = 0;
                _vrik.solver.leftLeg.rotationWeight = 0;
            }

            if (_trackedDevices.RightFoot.Found)
            {
                _vrik.solver.rightLeg.target = _vrikManager.solver_rightLeg_target;
                _vrik.solver.rightLeg.positionWeight = _vrikManager.solver_rightLeg_positionWeight;
                _vrik.solver.rightLeg.rotationWeight = _vrikManager.solver_rightLeg_rotationWeight;
            }
            else
            {
                _vrik.solver.rightLeg.target = null;
                _vrik.solver.rightLeg.positionWeight = 0;
                _vrik.solver.rightLeg.rotationWeight = 0;
            }

            if (_trackedDevices.Waist.Found)
            {
                _vrik.solver.spine.pelvisTarget = _vrikManager.solver_spine_pelvisTarget;
                _vrik.solver.spine.pelvisPositionWeight = _vrikManager.solver_spine_pelvisPositionWeight;
                _vrik.solver.spine.pelvisRotationWeight = _vrikManager.solver_spine_pelvisRotationWeight;
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
            if (Plugin.leftHandAnimAction != null)
            {
                try
                {
                    SkeletalSummaryData leftHandAnim = Plugin.leftHandAnimAction.GetSummaryData();

                    ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       _poseManager.openHand_LeftThumbProximal,       _poseManager.closedHand_LeftThumbProximal,       leftHandAnim.thumbCurl * 2);
                    ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   _poseManager.openHand_LeftThumbIntermediate,   _poseManager.closedHand_LeftThumbIntermediate,   leftHandAnim.thumbCurl * 2);
                    ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         _poseManager.openHand_LeftThumbDistal,         _poseManager.closedHand_LeftThumbDistal,         leftHandAnim.thumbCurl * 2);

                    ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       _poseManager.openHand_LeftIndexProximal,       _poseManager.closedHand_LeftIndexProximal,       leftHandAnim.indexCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   _poseManager.openHand_LeftIndexIntermediate,   _poseManager.closedHand_LeftIndexIntermediate,   leftHandAnim.indexCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         _poseManager.openHand_LeftIndexDistal,         _poseManager.closedHand_LeftIndexDistal,         leftHandAnim.indexCurl);

                    ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      _poseManager.openHand_LeftMiddleProximal,      _poseManager.closedHand_LeftMiddleProximal,      leftHandAnim.middleCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  _poseManager.openHand_LeftMiddleIntermediate,  _poseManager.closedHand_LeftMiddleIntermediate,  leftHandAnim.middleCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        _poseManager.openHand_LeftMiddleDistal,        _poseManager.closedHand_LeftMiddleDistal,        leftHandAnim.middleCurl);

                    ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        _poseManager.openHand_LeftRingProximal,        _poseManager.closedHand_LeftRingProximal,        leftHandAnim.ringCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    _poseManager.openHand_LeftRingIntermediate,    _poseManager.closedHand_LeftRingIntermediate,    leftHandAnim.ringCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          _poseManager.openHand_LeftRingDistal,          _poseManager.closedHand_LeftRingDistal,          leftHandAnim.ringCurl);

                    ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      _poseManager.openHand_LeftLittleProximal,      _poseManager.closedHand_LeftLittleProximal,      leftHandAnim.littleCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  _poseManager.openHand_LeftLittleIntermediate,  _poseManager.closedHand_LeftLittleIntermediate,  leftHandAnim.littleCurl);
                    ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        _poseManager.openHand_LeftLittleDistal,        _poseManager.closedHand_LeftLittleDistal,        leftHandAnim.littleCurl);
                }
                catch (Exception) { }
            }

            if (Plugin.rightHandAnimAction != null)
            {
                try
                {
                    SkeletalSummaryData rightHandAnim = Plugin.rightHandAnimAction.GetSummaryData();

                    ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,      _poseManager.openHand_RightThumbProximal,      _poseManager.closedHand_RightThumbProximal,      rightHandAnim.thumbCurl * 2);
                    ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,  _poseManager.openHand_RightThumbIntermediate,  _poseManager.closedHand_RightThumbIntermediate,  rightHandAnim.thumbCurl * 2);
                    ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,        _poseManager.openHand_RightThumbDistal,        _poseManager.closedHand_RightThumbDistal,        rightHandAnim.thumbCurl * 2);

                    ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,      _poseManager.openHand_RightIndexProximal,      _poseManager.closedHand_RightIndexProximal,      rightHandAnim.indexCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,  _poseManager.openHand_RightIndexIntermediate,  _poseManager.closedHand_RightIndexIntermediate,  rightHandAnim.indexCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,        _poseManager.openHand_RightIndexDistal,        _poseManager.closedHand_RightIndexDistal,        rightHandAnim.indexCurl);

                    ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,     _poseManager.openHand_RightMiddleProximal,     _poseManager.closedHand_RightMiddleProximal,     rightHandAnim.middleCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate, _poseManager.openHand_RightMiddleIntermediate, _poseManager.closedHand_RightMiddleIntermediate, rightHandAnim.middleCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,       _poseManager.openHand_RightMiddleDistal,       _poseManager.closedHand_RightMiddleDistal,       rightHandAnim.middleCurl);

                    ApplyBodyBonePose(HumanBodyBones.RightRingProximal,       _poseManager.openHand_RightRingProximal,       _poseManager.closedHand_RightRingProximal,       rightHandAnim.ringCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,   _poseManager.openHand_RightRingIntermediate,   _poseManager.closedHand_RightRingIntermediate,   rightHandAnim.ringCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightRingDistal,         _poseManager.openHand_RightRingDistal,         _poseManager.closedHand_RightRingDistal,         rightHandAnim.ringCurl);

                    ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,     _poseManager.openHand_RightLittleProximal,     _poseManager.closedHand_RightLittleProximal,     rightHandAnim.littleCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate, _poseManager.openHand_RightLittleIntermediate, _poseManager.closedHand_RightLittleIntermediate, rightHandAnim.littleCurl);
                    ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,       _poseManager.openHand_RightLittleDistal,       _poseManager.closedHand_RightLittleDistal,       rightHandAnim.littleCurl);
                }
                catch (Exception) { }
            }
        }

        private void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float position)
        {
            Transform boneTransform = _animator.GetBoneTransform(bodyBone);

            if (!boneTransform) return;

            boneTransform.localPosition = Vector3.Lerp(open.position, closed.position, position);
            boneTransform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, position);
        }
    }
}
