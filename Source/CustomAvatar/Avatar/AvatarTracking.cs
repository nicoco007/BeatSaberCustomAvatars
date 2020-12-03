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

using CustomAvatar.Tracking;
using System;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using CustomAvatar.Utilities;

namespace CustomAvatar.Avatar
{
    internal class AvatarTracking : MonoBehaviour
    {
        internal bool isCalibrationModeEnabled = false;

        private IAvatarInput _input;
        private SpawnedAvatar _spawnedAvatar;
        private ILogger<AvatarTracking> _logger = new UnityDebugLogger<AvatarTracking>();
        private TrackingHelper _trackingHelper;

        private Vector3 _prevBodyLocalPosition = Vector3.zero;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, IAvatarInput input, SpawnedAvatar spawnedAvatar, TrackingHelper trackingHelper)
        {
            _logger = loggerProvider.CreateLogger<AvatarTracking>(spawnedAvatar.avatar.descriptor.name);
            _input = input;
            _spawnedAvatar = spawnedAvatar;
            _trackingHelper = trackingHelper;
        }

        private void LateUpdate()
        {
            try
            {
                SetPose(DeviceUse.Head,      _spawnedAvatar.head);
                SetPose(DeviceUse.LeftHand,  _spawnedAvatar.leftHand);
                SetPose(DeviceUse.RightHand, _spawnedAvatar.rightHand);

                if (isCalibrationModeEnabled)
                {
                    if (_spawnedAvatar.pelvis)
                    {
                        _trackingHelper.SetLocalPose(_spawnedAvatar.avatar.pelvis.position * _spawnedAvatar.scale, _spawnedAvatar.avatar.pelvis.rotation, _spawnedAvatar.pelvis, transform.parent);
                    }

                    if (_spawnedAvatar.leftLeg)
                    {
                        _trackingHelper.SetLocalPose(_spawnedAvatar.avatar.leftLeg.position * _spawnedAvatar.scale, _spawnedAvatar.avatar.leftLeg.rotation, _spawnedAvatar.leftLeg, transform.parent);
                    }

                    if (_spawnedAvatar.rightLeg)
                    {
                        _trackingHelper.SetLocalPose(_spawnedAvatar.avatar.rightLeg.position * _spawnedAvatar.scale, _spawnedAvatar.avatar.rightLeg.rotation, _spawnedAvatar.rightLeg, transform.parent);
                    }
                }
                else
                {
                    SetPose(DeviceUse.Waist,     _spawnedAvatar.pelvis);
                    SetPose(DeviceUse.LeftFoot,  _spawnedAvatar.leftLeg);
                    SetPose(DeviceUse.RightFoot, _spawnedAvatar.rightLeg);
                }

                if (_spawnedAvatar.body)
                {
                    _spawnedAvatar.body.position = _spawnedAvatar.head.position - (_spawnedAvatar.head.up * 0.1f);

                    var vel = new Vector3(_spawnedAvatar.body.localPosition.x - _prevBodyLocalPosition.x, 0.0f,
                        _spawnedAvatar.body.localPosition.z - _prevBodyLocalPosition.z);

                    var rot = Quaternion.Euler(0.0f, _spawnedAvatar.head.localEulerAngles.y, 0.0f);
                    var tiltAxis = Vector3.Cross(transform.up, vel);

                    _spawnedAvatar.body.localRotation = Quaternion.Lerp(_spawnedAvatar.body.localRotation,
                        Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                        Time.deltaTime * 10.0f);

                    _prevBodyLocalPosition = _spawnedAvatar.body.localPosition;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"{e.Message}\n{e.StackTrace}");
            }
        }

        #pragma warning restore IDE0051
        #endregion

        private void SetPose(DeviceUse use, Transform target)
        {
            if (!target || !_input.TryGetPose(use, out Pose pose)) return;

            _trackingHelper.SetLocalPose(pose.position, pose.rotation, target, transform.parent);
        }
    }
}
