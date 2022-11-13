//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections;
using System.Linq;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarTracking : MonoBehaviour
    {
        private const float kMinBlinkInterval = 60f / 25;
        private const float kMaxBlinkInterval = 60f / 10;
        private const float kBlinkDuration = 0.15f;

        [System.Obsolete]
        public bool isCalibrationModeEnabled { get; set; }

        private IAvatarInput _input;
        private SpawnedAvatar _spawnedAvatar;
        private ILogger<AvatarTracking> _logger = new UnityDebugLogger<AvatarTracking>();
        private TrackingHelper _trackingHelper;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private int _blendShapeIndex = -1;

        private readonly AnimationCurve _blinkAnimationCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.5f, 100),
            new Keyframe(1, 0));

        private Vector3 _prevBodyLocalPosition = Vector3.zero;


        [Inject]
        [UsedImplicitly]
        private void Construct(ILogger<AvatarTracking> logger, IAvatarInput input, SpawnedAvatar spawnedAvatar, TrackingHelper trackingHelper)
        {
            _logger = logger;
            _input = input;
            _spawnedAvatar = spawnedAvatar;
            _trackingHelper = trackingHelper;

            _logger.name = spawnedAvatar.prefab.descriptor.name;
        }

        private void Start()
        {
            _skinnedMeshRenderer = _spawnedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(skm => skm.sharedMesh.GetBlendShapeIndex("Blink") != -1);
            if (_skinnedMeshRenderer != null)
            {
                _logger.LogNotice($"Found blink blendshape on {_skinnedMeshRenderer.name}");
                _blendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Blink");
            }
            else
            {
                _logger.LogNotice("No blink blend shape!");
            }
        }

        private void OnEnable()
        {
            if (_skinnedMeshRenderer != null && _blendShapeIndex >= 0)
            {
                StartCoroutine(DoBlink());
            }
        }

        private void LateUpdate()
        {
            SetPose(DeviceUse.Head, _spawnedAvatar.head);
            SetPose(DeviceUse.LeftHand, _spawnedAvatar.leftHand);
            SetPose(DeviceUse.RightHand, _spawnedAvatar.rightHand);

#pragma warning disable CS0612
            if (isCalibrationModeEnabled)
            {
                if (_spawnedAvatar.pelvis)
                {
                    _trackingHelper.SetLocalPose(_spawnedAvatar.prefab.pelvis.position * _spawnedAvatar.scale, _spawnedAvatar.prefab.pelvis.rotation, _spawnedAvatar.pelvis, transform.parent);
                }

                if (_spawnedAvatar.leftLeg)
                {
                    _trackingHelper.SetLocalPose(_spawnedAvatar.prefab.leftLeg.position * _spawnedAvatar.scale, _spawnedAvatar.prefab.leftLeg.rotation, _spawnedAvatar.leftLeg, transform.parent);
                }

                if (_spawnedAvatar.rightLeg)
                {
                    _trackingHelper.SetLocalPose(_spawnedAvatar.prefab.rightLeg.position * _spawnedAvatar.scale, _spawnedAvatar.prefab.rightLeg.rotation, _spawnedAvatar.rightLeg, transform.parent);
                }
            }
#pragma warning restore CS0612
            else
            {
                SetPose(DeviceUse.Waist, _spawnedAvatar.pelvis);
                SetPose(DeviceUse.LeftFoot, _spawnedAvatar.leftLeg);
                SetPose(DeviceUse.RightFoot, _spawnedAvatar.rightLeg);
            }

            if (_spawnedAvatar.body)
            {
                _spawnedAvatar.body.position = _spawnedAvatar.head.position - _spawnedAvatar.head.up * 0.1f;

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


        private void SetPose(DeviceUse use, Transform target)
        {
            if (!target || !_input.TryGetPose(use, out Pose pose)) return;

            _trackingHelper.SetLocalPose(pose.position, pose.rotation, target, transform.parent);
        }

        private IEnumerator DoBlink()
        {
            while (enabled)
            {
                yield return new WaitForSecondsRealtime(Random.Range(kMinBlinkInterval, kMaxBlinkInterval));

                float startTime = Time.time;
                float time;

                do
                {
                    time = Time.time - startTime;
                    _skinnedMeshRenderer.SetBlendShapeWeight(_blendShapeIndex, _blinkAnimationCurve.Evaluate(time / kBlinkDuration));
                    yield return null;
                }
                while (time <= kBlinkDuration);

                _skinnedMeshRenderer.SetBlendShapeWeight(_blendShapeIndex, 0);
            }
        }
    }
}
