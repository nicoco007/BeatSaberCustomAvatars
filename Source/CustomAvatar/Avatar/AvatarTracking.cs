//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using CustomAvatar.Tracking;
using System;
using CustomAvatar.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarTracking : MonoBehaviour
    {
        private Pose _initialPelvisPose;
        private Pose _initialLeftFootPose;
        private Pose _initialRightFootPose;

        private Vector3 _prevBodyLocalPosition = Vector3.zero;

        public bool isCalibrationModeEnabled = false;

        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private MainSettingsModelSO _mainSettingsModel;
        private VRPlatformHelper _vrPlatformHelper;
        private ILogger<AvatarTracking> _logger = new UnityDebugLogger<AvatarTracking>();
        private AvatarTailor _tailor;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        private void Inject(MainSettingsModelSO mainSettingsModel, ILoggerProvider loggerProvider, IAvatarInput input, SpawnedAvatar avatar, VRPlatformHelper vrPlatformHelper, AvatarTailor tailor)
        {
            _mainSettingsModel = mainSettingsModel;
            _logger = loggerProvider.CreateLogger<AvatarTracking>(avatar.avatar.descriptor.name);
            _input = input;
            _avatar = avatar;
            _vrPlatformHelper = vrPlatformHelper;
            _tailor = tailor;

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
                else
                {
                    if (_avatar.leftLeg && _input.TryGetLeftFootPose(out Pose leftFootPose))
                    {
                        leftFootPose.position = _tailor.ApplyTrackedPointFloorOffset(_avatar, leftFootPose.position);

                        _avatar.leftLeg.position = leftFootPose.position;
                        _avatar.leftLeg.rotation = leftFootPose.rotation;
                    }

                    if (_avatar.rightLeg && _input.TryGetRightFootPose(out Pose rightFootPose))
                    {
                        rightFootPose.position = _tailor.ApplyTrackedPointFloorOffset(_avatar, rightFootPose.position);

                        _avatar.rightLeg.position = rightFootPose.position;
                        _avatar.rightLeg.rotation = rightFootPose.rotation;
                    }

                    if (_avatar.pelvis && _input.TryGetWaistPose(out Pose pelvisPose))
                    {
                        pelvisPose.position = _tailor.ApplyTrackedPointFloorOffset(_avatar, pelvisPose.position);

                        _avatar.pelvis.position = pelvisPose.position;
                        _avatar.pelvis.rotation = pelvisPose.rotation;
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

        #pragma warning restore IDE0051
        #endregion
    }
}
