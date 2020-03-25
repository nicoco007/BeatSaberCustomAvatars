extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using CustomAvatar.Tracking;
using System;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar.Avatar
{
    internal class AvatarTracking : BodyAwareBehaviour
    {
        public float verticalPosition
        {
	        get => transform.position.y;
	        set => transform.position = new Vector3(0, value, 0);
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
        public LoadedAvatar customAvatar;
		
        private Vector3 _initialScale;

        private Pose _initialPelvisPose;
        private Pose _initialLeftFootPose;
        private Pose _initialRightFootPose;

        private Vector3 _prevBodyLocalPosition = Vector3.zero;

        private Pose _prevPelvisPose = Pose.identity;
        private Pose _prevLeftLegPose = Pose.identity;
        private Pose _prevRightLegPose = Pose.identity;

        private VRPlatformHelper _vrPlatformHelper;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        private void Awake()
        {
            _initialScale = transform.localScale;
		}

        protected override void Start()
        {
            base.Start();

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
            
            _vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

            if (_pelvis) _initialPelvisPose = new Pose(_pelvis.position, _pelvis.rotation);
            if (_leftLeg) _initialLeftFootPose = new Pose(_leftLeg.position, _leftLeg.rotation);
            if (_rightLeg) _initialRightFootPose = new Pose(_rightLeg.position, _rightLeg.rotation);
        }

        private void LateUpdate()
        {
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
                    _body.position = _head.position - (_head.up * 0.1f);

                    var vel = new Vector3(_body.localPosition.x - _prevBodyLocalPosition.x, 0.0f,
                        _body.localPosition.z - _prevBodyLocalPosition.z);

                    var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
                    var tiltAxis = Vector3.Cross(transform.up, vel);

                    _body.localRotation = Quaternion.Lerp(_body.localRotation,
                        Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                        Time.deltaTime * 10.0f);

                    _prevBodyLocalPosition = _body.localPosition;
                }
            }
            catch (Exception e)
            {
                Plugin.logger.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private Vector3 AdjustTransformPosition(Vector3 original, Vector3 correction, Vector3 originalPosition)
        {
            Vector3 corrected = original + correction;
            float y = verticalPosition;

            if (SettingsManager.settings.moveFloorWithRoomAdjust)
            {
                y -= BeatSaberUtil.GetRoomCenter().y;
            }

            return new Vector3(corrected.x, corrected.y + (1 - originalPosition.y / customAvatar.eyeHeight) * y, corrected.z);
        }
    }
}
