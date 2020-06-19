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
    public class AvatarTracking : MonoBehaviour
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
        private AvatarTailor _tailor;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(Settings settings, Settings.AvatarSpecificSettings avatarSpecificSettings, MainSettingsModelSO mainSettingsModel, ILoggerProvider loggerProvider, AvatarInput input, SpawnedAvatar avatar, VRPlatformHelper vrPlatformHelper, AvatarTailor tailor)
        {
            _settings = settings;
            _avatarSpecificSettings = avatarSpecificSettings;
            _mainSettingsModel = mainSettingsModel;
            _logger = loggerProvider.CreateLogger<AvatarTracking>(avatar.avatar.descriptor.name);
            _input = input;
            _avatar = avatar;
            _vrPlatformHelper = vrPlatformHelper;
            _tailor = tailor;
        }

        private void Start()
        {
            if (_avatar.pelvis) _initialPelvisPose = new Pose(_avatar.pelvis.position, _avatar.pelvis.rotation);
            if (_avatar.leftLeg) _initialLeftFootPose = new Pose(_avatar.leftLeg.position, _avatar.leftLeg.rotation);
            if (_avatar.rightLeg) _initialRightFootPose = new Pose(_avatar.rightLeg.position, _avatar.rightLeg.rotation);
        }

        private void LateUpdate()
        {
            try
            {
                if (_avatar.head && _input.TryGetHeadPose(out Pose headPose))
                {
                    _avatar.head.position = headPose.position;
                    _avatar.head.rotation = headPose.rotation;
                }
                
                Vector3 controllerPositionOffset = _mainSettingsModel.controllerPosition;
                Vector3 controllerRotationOffset = _mainSettingsModel.controllerRotation;

                if (_avatar.rightHand && _input.TryGetRightHandPose(out Pose rightHandPose))
                {
                    _avatar.rightHand.position = rightHandPose.position;
                    _avatar.rightHand.rotation = rightHandPose.rotation;
                    
                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(XRNode.RightHand, _avatar.rightHand, controllerPositionOffset, controllerRotationOffset);
                }

                // mirror across YZ plane for left hand
                controllerPositionOffset = new Vector3(-controllerPositionOffset.x, controllerPositionOffset.y, controllerPositionOffset.z);
                controllerRotationOffset = new Vector3(controllerRotationOffset.x, -controllerRotationOffset.y, -controllerRotationOffset.z);

                if (_avatar.leftHand && _input.TryGetLeftHandPose(out Pose leftHandPose))
                {
                    _avatar.leftHand.position = leftHandPose.position;
                    _avatar.leftHand.rotation = leftHandPose.rotation;

                    _vrPlatformHelper.AdjustPlatformSpecificControllerTransform(XRNode.LeftHand, _avatar.leftHand, controllerPositionOffset, controllerRotationOffset);
                }

                if (isCalibrationModeEnabled)
                {
                    if (_avatar.pelvis)
                    {
                        _avatar.pelvis.position = _initialPelvisPose.position * _avatar.scale + new Vector3(0, _avatar.verticalPosition, 0);
                        _avatar.pelvis.rotation = _initialPelvisPose.rotation;
                    }

                    if (_avatar.leftLeg)
                    {
                        _avatar.leftLeg.position = _initialLeftFootPose.position * _avatar.scale + new Vector3(0, _avatar.verticalPosition, 0);
                        _avatar.leftLeg.rotation = _initialLeftFootPose.rotation;
                    }

                    if (_avatar.rightLeg)
                    {
                        _avatar.rightLeg.position = _initialRightFootPose.position * _avatar.scale + new Vector3(0, _avatar.verticalPosition, 0);
                        _avatar.rightLeg.rotation = _initialRightFootPose.rotation;
                    }
                }
                else if (_avatar.shouldTrackFullBody)
                {
                    if (_avatar.leftLeg && _input.TryGetLeftFootPose(out Pose leftFootPose))
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

                        _avatar.leftLeg.position = _prevLeftLegPose.position;
                        _avatar.leftLeg.rotation = _prevLeftLegPose.rotation;
                    }

                    if (_avatar.rightLeg && _input.TryGetRightFootPose(out Pose rightFootPose))
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

                        _avatar.rightLeg.position = _prevRightLegPose.position;
                        _avatar.rightLeg.rotation = _prevRightLegPose.rotation;
                    }

                    if (_avatar.pelvis && _input.TryGetWaistPose(out Pose pelvisPose))
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

                        _avatar.pelvis.position = _prevPelvisPose.position;
                        _avatar.pelvis.rotation = _prevPelvisPose.rotation;
                    }
                }

                if (_avatar.body)
                {
                    _avatar.body.position = _avatar.head.position - (_avatar.head.up * 0.1f);

                    var vel = new Vector3(_avatar.body.localPosition.x - _prevBodyLocalPosition.x, 0.0f,
                        _avatar.body.localPosition.z - _prevBodyLocalPosition.z);

                    var rot = Quaternion.Euler(0.0f, _avatar.head.localEulerAngles.y, 0.0f);
                    var tiltAxis = Vector3.Cross(transform.up, vel);

                    _avatar.body.localRotation = Quaternion.Lerp(_avatar.body.localRotation,
                        Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                        Time.deltaTime * 10.0f);

                    _prevBodyLocalPosition = _avatar.body.localPosition;
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

            correctedPosition = _tailor.ApplyTrackedPointFloorOffset(_avatar, correctedPosition);

            return new Pose(Vector3.Lerp(previousPose.position, correctedPosition, smoothing.position), Quaternion.Slerp(previousPose.rotation, correctedRotation, smoothing.rotation));
        }
    }
}
