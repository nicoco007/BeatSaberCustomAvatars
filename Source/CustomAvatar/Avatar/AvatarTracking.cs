extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using CustomAvatar.Tracking;
using System;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    public class AvatarTracking : BodyAwareBehaviour
    {
        private Settings.AvatarSpecificSettings _avatarSpecificSettings;

        private Pose _initialPelvisPose;
        private Pose _initialLeftFootPose;
        private Pose _initialRightFootPose;

        private Vector3 _prevBodyLocalPosition = Vector3.zero;

        private Pose _prevPelvisPose = Pose.identity;
        private Pose _prevLeftLegPose = Pose.identity;
        private Pose _prevRightLegPose = Pose.identity;

        public bool isCalibrationModeEnabled = false;

        private AvatarInput _input;
        private SpawnedAvatar _avatar;
        private Settings _settings;
        private MainSettingsModelSO _mainSettingsModel;
        private VRPlatformHelper _vrPlatformHelper;
        private ILogger _logger = new UnityDebugLogger<AvatarTracking>();

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(Settings settings, MainSettingsModelSO mainSettingsModel, ILoggerProvider loggerProvider, AvatarInput input, SpawnedAvatar avatar, VRPlatformHelper vrPlatformHelper)
        {
            _settings = settings;
            _mainSettingsModel = mainSettingsModel;
            _logger = loggerProvider.CreateLogger<AvatarTracking>(avatar.avatar.descriptor.name);
            _input = input;
            _avatar = avatar;
            _vrPlatformHelper = vrPlatformHelper;
        }

        protected override void Start()
        {
            base.Start();

            _avatarSpecificSettings = _settings.GetAvatarSettings(_avatar.avatar.fullPath);

            if (pelvis) _initialPelvisPose = new Pose(pelvis.position, pelvis.rotation);
            if (leftLeg) _initialLeftFootPose = new Pose(leftLeg.position, leftLeg.rotation);
            if (rightLeg) _initialRightFootPose = new Pose(rightLeg.position, rightLeg.rotation);
        }

        private void LateUpdate()
        {
            try
            {
                if (head && _input.TryGetHeadPose(out Pose headPose))
                {
                    head.position = headPose.position;
                    head.rotation = headPose.rotation;
                }
                
                Vector3 controllerPositionOffset = _mainSettingsModel.controllerPosition;
                Vector3 controllerRotationOffset = _mainSettingsModel.controllerRotation;

                if (rightHand && _input.TryGetRightHandPose(out Pose rightHandPose))
                {
                    rightHand.position = rightHandPose.position;
                    rightHand.rotation = rightHandPose.rotation;
                    
                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(XRNode.RightHand, rightHand, controllerPositionOffset, controllerRotationOffset);
                }

                // mirror across YZ plane for left hand
                controllerPositionOffset = new Vector3(-controllerPositionOffset.x, controllerPositionOffset.y, controllerPositionOffset.z);
                controllerRotationOffset = new Vector3(controllerRotationOffset.x, -controllerRotationOffset.y, -controllerRotationOffset.z);

                if (leftHand && _input.TryGetLeftHandPose(out Pose leftHandPose))
                {
                    leftHand.position = leftHandPose.position;
                    leftHand.rotation = leftHandPose.rotation;

                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(XRNode.LeftHand, leftHand, controllerPositionOffset, controllerRotationOffset);
                }

                if (isCalibrationModeEnabled)
                {
                    if (pelvis)
                    {
                        pelvis.position = ApplyTrackedPointFloorOffset(_initialPelvisPose.position * _avatar.scale);
                        pelvis.rotation = _initialPelvisPose.rotation;
                    }

                    if (leftLeg)
                    {
                        leftLeg.position = ApplyTrackedPointFloorOffset(_initialLeftFootPose.position * _avatar.scale);
                        leftLeg.rotation = _initialLeftFootPose.rotation;
                    }

                    if (rightLeg)
                    {
                        rightLeg.position = ApplyTrackedPointFloorOffset(_initialRightFootPose.position * _avatar.scale);
                        rightLeg.rotation = _initialRightFootPose.rotation;
                    }
                }
                else
                {
                    if (leftLeg && _input.TryGetLeftFootPose(out Pose leftFootPose))
                    {
                        Pose correction;

                        if (_avatarSpecificSettings.useAutomaticCalibration)
                        {
                            correction = _settings.automaticCalibration.leftLeg;
                            correction.position -= Vector3.up * _settings.automaticCalibration.leftLegOffset;
                        }
                        else
                        {
                            correction = _avatarSpecificSettings.fullBodyCalibration.leftLeg;
                        }

                        _prevLeftLegPose = AdjustTrackedPointPose(_prevLeftLegPose, leftFootPose, correction, _settings.fullBodyMotionSmoothing.feet);

                        leftLeg.position = _prevLeftLegPose.position;
                        leftLeg.rotation = _prevLeftLegPose.rotation;
                    }

                    if (rightLeg && _input.TryGetRightFootPose(out Pose rightFootPose))
                    {
                        Pose correction;

                        if (_avatarSpecificSettings.useAutomaticCalibration)
                        {
                            correction = _settings.automaticCalibration.rightLeg;
                            correction.position -= Vector3.up * _settings.automaticCalibration.rightLegOffset;
                        }
                        else
                        {
                            correction = _avatarSpecificSettings.fullBodyCalibration.rightLeg;
                        }

                        _prevRightLegPose = AdjustTrackedPointPose(_prevRightLegPose, rightFootPose, correction, _settings.fullBodyMotionSmoothing.feet);

                        rightLeg.position = _prevRightLegPose.position;
                        rightLeg.rotation = _prevRightLegPose.rotation;
                    }

                    if (pelvis && _input.TryGetWaistPose(out Pose pelvisPose))
                    {
                        Pose correction;

                        if (_avatarSpecificSettings.useAutomaticCalibration)
                        {
                            correction = _settings.automaticCalibration.pelvis;

                            Quaternion rotationOffset = Quaternion.Euler(0, (int) _settings.automaticCalibration.waistTrackerPosition * -90, 0);

                            correction.position -= Quaternion.Inverse(rotationOffset) * (Vector3.forward * _settings.automaticCalibration.pelvisOffset);
                            correction.rotation *= rotationOffset;
                        }
                        else
                        {
                            correction = _avatarSpecificSettings.fullBodyCalibration.pelvis;
                        }

                        _prevPelvisPose = AdjustTrackedPointPose(_prevPelvisPose, pelvisPose, correction, _settings.fullBodyMotionSmoothing.waist);

                        pelvis.position = _prevPelvisPose.position;
                        pelvis.rotation = _prevPelvisPose.rotation;
                    }
                }

                if (body)
                {
                    body.position = head.position - (head.up * 0.1f);

                    var vel = new Vector3(body.localPosition.x - _prevBodyLocalPosition.x, 0.0f,
                        body.localPosition.z - _prevBodyLocalPosition.z);

                    var rot = Quaternion.Euler(0.0f, head.localEulerAngles.y, 0.0f);
                    var tiltAxis = Vector3.Cross(transform.up, vel);

                    body.localRotation = Quaternion.Lerp(body.localRotation,
                        Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                        Time.deltaTime * 10.0f);

                    _prevBodyLocalPosition = body.localPosition;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private Pose AdjustTrackedPointPose(Pose previousPose, Pose currentPose, Pose correction, Settings.TrackedPointSmoothing smoothing)
        {
            Quaternion correctedRotation = currentPose.rotation * correction.rotation;
            Vector3 correctedPosition = currentPose.position + correctedRotation * correction.position; // correction is forward-facing by definition

            correctedPosition = ApplyTrackedPointFloorOffset(correctedPosition);

            return new Pose(Vector3.Lerp(previousPose.position, correctedPosition, smoothing.position), Quaternion.Slerp(previousPose.rotation, correctedRotation, smoothing.rotation));
        }

        private Vector3 ApplyTrackedPointFloorOffset(Vector3 position)
        {
            if (!_settings.enableFloorAdjust) return position;

            float scaledEyeHeight = (_avatar.eyeHeight * _avatar.scale);

            float yOffset = _avatar.verticalPosition;
            float yOffsetScale = scaledEyeHeight / (scaledEyeHeight + yOffset);

            return new Vector3(position.x, position.y * yOffsetScale + yOffset, position.z);
        }
    }
}
