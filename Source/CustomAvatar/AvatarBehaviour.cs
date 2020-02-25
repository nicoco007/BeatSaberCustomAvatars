extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using CustomAvatar.Tracking;
using DynamicOpenVR.IO;
using System;
using System.Collections.Generic;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;
using VRIK = BeatSaberFinalIK::RootMotion.FinalIK.VRIK;
using TwistRelaxer = BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer;

namespace CustomAvatar
{
    internal class AvatarBehaviour : MonoBehaviour
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

        public AvatarInput input;
        public CustomAvatar customAvatar;
		
        private Vector3 _initialPosition;
        private Vector3 _initialScale;

        private Transform _head;
        private Transform _body;
        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _pelvis;

        private Pose _initialPelvisPose;
        private Pose _initialLeftFootPose;
        private Pose _initialRightFootPose;

        private Vector3 _prevBodyLocalPosition = Vector3.zero;

        private Pose _prevPelvisPose = Pose.identity;
        private Pose _prevLeftLegPose = Pose.identity;
        private Pose _prevRightLegPose = Pose.identity;

        private VRIK _vrik;
        private VRIKManager _vrikManager;
        private VRPlatformHelper _vrPlatformHelper;
        private Animator _animator;
        private PoseManager _poseManager;

        private SkeletalInput _leftHandAnimAction;
        private SkeletalInput _rightHandAnimAction;

        private BeatSaberDynamicBone::DynamicBone[] _dynamicBones;

        private bool _isFingerTrackingSupported;
        private bool _fixTransforms;
        private bool _fingerTrackingExceptionOccurred;

        private Action<BeatSaberDynamicBone::DynamicBone> _preUpdateDelegate;
        private Action<BeatSaberDynamicBone::DynamicBone, float> _updateDynamicBonesDelegate;

        private List<TwistRelaxer> _twistRelaxers = new List<TwistRelaxer>();

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        private void Awake()
        {
            // create delegates for dynamic bones private methods (more efficient than continuously calling Invoke())
            _preUpdateDelegate = typeof(BeatSaberDynamicBone::DynamicBone).CreatePrivateMethodDelegate<Action<BeatSaberDynamicBone::DynamicBone>>("PreUpdate");
            _updateDynamicBonesDelegate = typeof(BeatSaberDynamicBone::DynamicBone).CreatePrivateMethodDelegate<Action<BeatSaberDynamicBone::DynamicBone, float>>("UpdateDynamicBones");

            _initialPosition = transform.position;
            _initialScale = transform.localScale;

            _leftHandAnimAction  = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");

            _dynamicBones = GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>();

            foreach (TwistRelaxer twistRelaxer in GetComponentsInChildren<TwistRelaxer>())
            {
                if (twistRelaxer.enabled)
                {
                    twistRelaxer.enabled = false;
                    _twistRelaxers.Add(twistRelaxer);
                }
            }
		}

        private void Start()
        {
            if (input == null)
            {
                Destroy(this);
                throw new ArgumentNullException(nameof(input));
            }

            if (customAvatar == null)
            {
                Destroy(this);
                throw new ArgumentNullException(nameof(customAvatar));
            }

            _vrikManager = GetComponentInChildren<VRIKManager>();
            _animator = GetComponentInChildren<Animator>();
            _poseManager = GetComponentInChildren<PoseManager>();

            _isFingerTrackingSupported = _animator && _poseManager;

            if (_vrikManager)
            {
                _vrik = _vrikManager.vrik;
                _vrik.fixTransforms = false;

                _fixTransforms = _vrikManager.fixTransforms;

                _vrikManager.referencesUpdated += UpdateConditionalReferences;
                
                UpdateConditionalReferences();
                
                foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
                {
                    twistRelaxer.ik = _vrik;
                    twistRelaxer.enabled = true;
                }
            }
            
            _vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;
            
            input.inputChanged += OnInputChanged;

            _head = transform.Find("Head");
            _body = transform.Find("Body");
            _leftHand = transform.Find("LeftHand");
            _rightHand = transform.Find("RightHand");
            _leftLeg = transform.Find("LeftLeg");
            _rightLeg = transform.Find("RightLeg");
            _pelvis = transform.Find("Pelvis");

            if (_pelvis) _initialPelvisPose = new Pose(_pelvis.position, _pelvis.rotation);
            if (_leftLeg) _initialLeftFootPose = new Pose(_leftLeg.position, _leftLeg.rotation);
            if (_rightLeg) _initialRightFootPose = new Pose(_rightLeg.position, _rightLeg.rotation);

            foreach (FirstPersonExclusion firstPersonExclusion in GetComponentsInChildren<FirstPersonExclusion>())
            {
                foreach (GameObject gameObj in firstPersonExclusion.exclude)
                {
                    gameObj.layer = AvatarLayers.OnlyInThirdPerson;
                }
            }
        }

        private void Update()
        {
            if (_vrik && _fixTransforms)
            {
                _vrik.solver.FixTransforms();
            }

            // DynamicBones PreUpdate
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                if (!dynamicBone.enabled) continue;

                // setting m_Weight prevents the integrated calls to PreUpdate and UpdateDynamicBones from taking effect
                dynamicBone.SetPrivateField("m_Weight", 1);
                _preUpdateDelegate(dynamicBone);
                dynamicBone.SetPrivateField("m_Weight", 0);
            }
        }

        private void LateUpdate()
        {
            if (_isFingerTrackingSupported)
            {
                ApplyFingerTracking();
            }

            try
            {
                if (_head && input.TryGetHeadPose(out Pose headPose))
                {
                    _head.position = headPose.position;
                    _head.rotation = headPose.rotation;
                }
                
                Vector3 controllerPositionOffset = BeatSaberUtil.GetControllerPositionOffset();
                Vector3 controllerRotationOffset = BeatSaberUtil.GetControllerRotationOffset();

                if (_rightHand && input.TryGetRightHandPose(out Pose rightHandPose))
                {
                    _rightHand.position = rightHandPose.position;
                    _rightHand.rotation = rightHandPose.rotation;
                    
                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(XRNode.RightHand, _rightHand, controllerPositionOffset, controllerRotationOffset);
                }
                
                controllerPositionOffset = new Vector3(-controllerPositionOffset.x, controllerPositionOffset.y, controllerPositionOffset.z);
                controllerRotationOffset = new Vector3(controllerRotationOffset.x, -controllerRotationOffset.y, controllerRotationOffset.z);

                if (_leftHand && input.TryGetLeftHandPose(out Pose leftHandPose))
                {
                    _leftHand.position = leftHandPose.position;
                    _leftHand.rotation = leftHandPose.rotation;

                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(XRNode.LeftHand, _leftHand, controllerPositionOffset, controllerRotationOffset);
                }

                if (_leftLeg && input.TryGetLeftFootPose(out Pose leftFootPose))
                {
                    Pose correction = SettingsManager.settings.fullBodyCalibration.leftLeg;

                    _prevLeftLegPose.position = Vector3.Lerp(_prevLeftLegPose.position, AdjustTransformPosition(leftFootPose.position, correction.position, _initialLeftFootPose.position), SettingsManager.settings.fullBodyMotionSmoothing.feet.position * Time.deltaTime);
                    _prevLeftLegPose.rotation = Quaternion.Slerp(_prevLeftLegPose.rotation, leftFootPose.rotation * correction.rotation, SettingsManager.settings.fullBodyMotionSmoothing.feet.rotation * Time.deltaTime);
                    
                    _leftLeg.position = _prevLeftLegPose.position;
                    _leftLeg.rotation = _prevLeftLegPose.rotation;
                }

                if (_rightLeg && input.TryGetRightFootPose(out Pose rightFootPose))
                {
                    Pose correction = SettingsManager.settings.fullBodyCalibration.rightLeg;

                    _prevRightLegPose.position = Vector3.Lerp(_prevRightLegPose.position, AdjustTransformPosition(rightFootPose.position, correction.position, _initialRightFootPose.position), SettingsManager.settings.fullBodyMotionSmoothing.feet.position * Time.deltaTime);
                    _prevRightLegPose.rotation = Quaternion.Slerp(_prevRightLegPose.rotation, rightFootPose.rotation * correction.rotation, SettingsManager.settings.fullBodyMotionSmoothing.feet.rotation * Time.deltaTime);
                    
                    _rightLeg.position = _prevRightLegPose.position;
                    _rightLeg.rotation = _prevRightLegPose.rotation;
                }

                if (_pelvis && input.TryGetWaistPose(out Pose pelvisPose))
                {
                    Pose correction = SettingsManager.settings.fullBodyCalibration.pelvis;

                    _prevPelvisPose.position = Vector3.Lerp(_prevPelvisPose.position, AdjustTransformPosition(pelvisPose.position, correction.position, _initialPelvisPose.position), SettingsManager.settings.fullBodyMotionSmoothing.waist.position * Time.deltaTime);
                    _prevPelvisPose.rotation = Quaternion.Slerp(_prevPelvisPose.rotation, pelvisPose.rotation * correction.rotation, SettingsManager.settings.fullBodyMotionSmoothing.waist.rotation * Time.deltaTime);
                    
                    _pelvis.position = _prevPelvisPose.position;
                    _pelvis.rotation = _prevPelvisPose.rotation;
                }

                if (_body)
                {
                    _body.position = _head.position - (_head.transform.up * 0.1f);

                    var vel = new Vector3(_body.transform.localPosition.x - _prevBodyLocalPosition.x, 0.0f,
                        _body.localPosition.z - _prevBodyLocalPosition.z);

                    var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
                    var tiltAxis = Vector3.Cross(transform.up, vel);

                    _body.localRotation = Quaternion.Lerp(_body.localRotation,
                        Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                        Time.deltaTime * 10.0f);

                    _prevBodyLocalPosition = _body.transform.localPosition;
                }
            }
            catch (Exception e)
            {
                Plugin.logger.Error($"{e.Message}\n{e.StackTrace}");
            }

            // VRIK must run before dynamic bones
            if (_vrik)
            {
                _vrik.UpdateSolverExternal();
            }

            // apply dynamic bones
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                if (!dynamicBone.enabled) continue;

                // setting m_Weight prevents the integrated calls to PreUpdate and UpdateDynamicBones from taking effect
                dynamicBone.SetPrivateField("m_Weight", 1);
                _updateDynamicBonesDelegate(dynamicBone, Time.deltaTime);
                dynamicBone.SetPrivateField("m_Weight", 0);
            }
        }

        private void OnDestroy()
        {
            input.inputChanged -= OnInputChanged;

            _leftHandAnimAction?.Dispose();
            _rightHandAnimAction?.Dispose();
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private void OnInputChanged()
        {
            Plugin.logger.Info("Tracking device change detected, updating VRIK references");
            UpdateConditionalReferences();
        }

        private Vector3 AdjustTransformPosition(Vector3 original, Vector3 correction, Vector3 originalPosition)
        {
            Vector3 corrected = original + correction;
            float y = position.y;

            if (SettingsManager.settings.moveFloorWithRoomAdjust)
            {
                y -= BeatSaberUtil.GetRoomCenter().y;
            }

            return new Vector3(corrected.x, corrected.y + (1 - originalPosition.y / customAvatar.eyeHeight) * y, corrected.z);
        }

        private void UpdateConditionalReferences()
        {
            if (!_vrik || !_vrikManager) return;
            
            Plugin.logger.Info("Updating conditional references");

            if (input.TryGetLeftFootPose(out _))
            {
                Plugin.logger.Debug("Left foot enabled");
                _vrik.solver.leftLeg.target = _vrikManager.solver_leftLeg_target;
                _vrik.solver.leftLeg.positionWeight = _vrikManager.solver_leftLeg_positionWeight;
                _vrik.solver.leftLeg.rotationWeight = _vrikManager.solver_leftLeg_rotationWeight;
            }
            else
            {
                Plugin.logger.Debug("Left foot disabled");
                _vrik.solver.leftLeg.target = null;
                _vrik.solver.leftLeg.positionWeight = 0;
                _vrik.solver.leftLeg.rotationWeight = 0;
            }

            if (input.TryGetRightFootPose(out _))
            {
                Plugin.logger.Debug("Right foot enabled");
                _vrik.solver.rightLeg.target = _vrikManager.solver_rightLeg_target;
                _vrik.solver.rightLeg.positionWeight = _vrikManager.solver_rightLeg_positionWeight;
                _vrik.solver.rightLeg.rotationWeight = _vrikManager.solver_rightLeg_rotationWeight;
            }
            else
            {
                Plugin.logger.Debug("Right foot disabled");
                _vrik.solver.rightLeg.target = null;
                _vrik.solver.rightLeg.positionWeight = 0;
                _vrik.solver.rightLeg.rotationWeight = 0;
            }

            if (input.TryGetWaistPose(out _))
            {
                Plugin.logger.Debug("Pelvis enabled");
                _vrik.solver.spine.pelvisTarget = _vrikManager.solver_spine_pelvisTarget;
                _vrik.solver.spine.pelvisPositionWeight = _vrikManager.solver_spine_pelvisPositionWeight;
                _vrik.solver.spine.pelvisRotationWeight = _vrikManager.solver_spine_pelvisRotationWeight;
                _vrik.solver.plantFeet = false;
            }
            else
            {
                Plugin.logger.Debug("Pelvis disabled");
                _vrik.solver.spine.pelvisTarget = null;
                _vrik.solver.spine.pelvisPositionWeight = 0;
                _vrik.solver.spine.pelvisRotationWeight = 0;
                _vrik.solver.plantFeet = _vrikManager.solver_plantFeet;
            }
        }

        public void ApplyFingerTracking()
        {
            if (_leftHandAnimAction.isActive && !_fingerTrackingExceptionOccurred)
            {
                try
                {
                    SkeletalSummaryData leftHandAnim = _leftHandAnimAction.summaryData;
                    ApplyLeftHandFingerPoses(leftHandAnim.thumbCurl, leftHandAnim.indexCurl, leftHandAnim.middleCurl, leftHandAnim.ringCurl, leftHandAnim.littleCurl);
                }
                catch (Exception ex)
                {
                    Plugin.logger.Error("Left hand finger tracking failed");
                    Plugin.logger.Error(ex.ToString());

                    _fingerTrackingExceptionOccurred = true;
                }
            }
            else
            {
                ApplyLeftHandFingerPoses(1, 1, 1, 1, 1);
            }

            if (_rightHandAnimAction.isActive && !_fingerTrackingExceptionOccurred)
            {
                try
                {
                    SkeletalSummaryData rightHandAnim = _rightHandAnimAction.summaryData;
                    ApplyRightHandFingerPoses(rightHandAnim.thumbCurl, rightHandAnim.indexCurl, rightHandAnim.middleCurl, rightHandAnim.ringCurl, rightHandAnim.littleCurl);
                }
                catch (Exception ex)
                {
                    Plugin.logger.Error("Right hand finger tracking failed");
                    Plugin.logger.Error(ex.ToString());

                    _fingerTrackingExceptionOccurred = true;
                }
            }
            else
            {
                ApplyRightHandFingerPoses(1, 1, 1, 1, 1);
            }
        }

        private void ApplyLeftHandFingerPoses(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float littleCurl)
        {
            ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       _poseManager.openHand_LeftThumbProximal,       _poseManager.closedHand_LeftThumbProximal,       thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   _poseManager.openHand_LeftThumbIntermediate,   _poseManager.closedHand_LeftThumbIntermediate,   thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         _poseManager.openHand_LeftThumbDistal,         _poseManager.closedHand_LeftThumbDistal,         thumbCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       _poseManager.openHand_LeftIndexProximal,       _poseManager.closedHand_LeftIndexProximal,       indexCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   _poseManager.openHand_LeftIndexIntermediate,   _poseManager.closedHand_LeftIndexIntermediate,   indexCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         _poseManager.openHand_LeftIndexDistal,         _poseManager.closedHand_LeftIndexDistal,         indexCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      _poseManager.openHand_LeftMiddleProximal,      _poseManager.closedHand_LeftMiddleProximal,      middleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  _poseManager.openHand_LeftMiddleIntermediate,  _poseManager.closedHand_LeftMiddleIntermediate,  middleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        _poseManager.openHand_LeftMiddleDistal,        _poseManager.closedHand_LeftMiddleDistal,        middleCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        _poseManager.openHand_LeftRingProximal,        _poseManager.closedHand_LeftRingProximal,        ringCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    _poseManager.openHand_LeftRingIntermediate,    _poseManager.closedHand_LeftRingIntermediate,    ringCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          _poseManager.openHand_LeftRingDistal,          _poseManager.closedHand_LeftRingDistal,          ringCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      _poseManager.openHand_LeftLittleProximal,      _poseManager.closedHand_LeftLittleProximal,      littleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  _poseManager.openHand_LeftLittleIntermediate,  _poseManager.closedHand_LeftLittleIntermediate,  littleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        _poseManager.openHand_LeftLittleDistal,        _poseManager.closedHand_LeftLittleDistal,        littleCurl);
        }

        private void ApplyRightHandFingerPoses(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float littleCurl)
        {
            ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,       _poseManager.openHand_RightThumbProximal,       _poseManager.closedHand_RightThumbProximal,       thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,   _poseManager.openHand_RightThumbIntermediate,   _poseManager.closedHand_RightThumbIntermediate,   thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,         _poseManager.openHand_RightThumbDistal,         _poseManager.closedHand_RightThumbDistal,         thumbCurl);

            ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,       _poseManager.openHand_RightIndexProximal,       _poseManager.closedHand_RightIndexProximal,       indexCurl);
            ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,   _poseManager.openHand_RightIndexIntermediate,   _poseManager.closedHand_RightIndexIntermediate,   indexCurl);
            ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,         _poseManager.openHand_RightIndexDistal,         _poseManager.closedHand_RightIndexDistal,         indexCurl);

            ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,      _poseManager.openHand_RightMiddleProximal,      _poseManager.closedHand_RightMiddleProximal,      middleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate,  _poseManager.openHand_RightMiddleIntermediate,  _poseManager.closedHand_RightMiddleIntermediate,  middleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,        _poseManager.openHand_RightMiddleDistal,        _poseManager.closedHand_RightMiddleDistal,        middleCurl);

            ApplyBodyBonePose(HumanBodyBones.RightRingProximal,        _poseManager.openHand_RightRingProximal,        _poseManager.closedHand_RightRingProximal,        ringCurl);
            ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,    _poseManager.openHand_RightRingIntermediate,    _poseManager.closedHand_RightRingIntermediate,    ringCurl);
            ApplyBodyBonePose(HumanBodyBones.RightRingDistal,          _poseManager.openHand_RightRingDistal,          _poseManager.closedHand_RightRingDistal,          ringCurl);

            ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,      _poseManager.openHand_RightLittleProximal,      _poseManager.closedHand_RightLittleProximal,      littleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate,  _poseManager.openHand_RightLittleIntermediate,  _poseManager.closedHand_RightLittleIntermediate,  littleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,        _poseManager.openHand_RightLittleDistal,        _poseManager.closedHand_RightLittleDistal,        littleCurl);
        }

        private void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float fade)
        {
            Transform boneTransform = _animator.GetBoneTransform(bodyBone);

            if (!boneTransform) return;

            boneTransform.localPosition = Vector3.Lerp(open.position, closed.position, fade);
            boneTransform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, fade);
        }
    }
}
