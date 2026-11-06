//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections.Generic;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;
using Zenject;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
    internal class AvatarTransformTracking : MonoBehaviour
    {
        private static readonly List<ConstraintSource> kEmptyConstraintSources = [];

        private Pose _initialHead;
        private Pose _initialLeftHand;
        private Pose _initialRightHand;
        private Pose _initialPelvis;
        private Pose _initialLeftFoot;
        private Pose _initialRightFoot;

        private ParentConstraint _head;
        private ParentConstraint _leftHand;
        private ParentConstraint _rightHand;
        private ParentConstraint _pelvis;
        private ParentConstraint _leftFoot;
        private ParentConstraint _rightFoot;

        private SpawnedAvatar _spawnedAvatar;
        private IAvatarInput _avatarInput;

        private bool _hasBody;
        private Vector3 _prevBodyLocalPosition;

        [Inject]
        [UsedImplicitly]
        private void Construct(SpawnedAvatar spawnedAvatar, IAvatarInput avatarInput)
        {
            _spawnedAvatar = spawnedAvatar;
            _avatarInput = avatarInput;
        }

        internal void ResetTransforms()
        {
            if (!didAwake)
            {
                return;
            }

            if (_spawnedAvatar.head != null) _spawnedAvatar.head.SetLocalPose(_initialHead);
            if (_spawnedAvatar.leftHand != null) _spawnedAvatar.leftHand.SetLocalPose(_initialLeftHand);
            if (_spawnedAvatar.rightHand != null) _spawnedAvatar.rightHand.SetLocalPose(_initialRightHand);
            if (_spawnedAvatar.pelvis != null) _spawnedAvatar.pelvis.SetLocalPose(_initialPelvis);
            if (_spawnedAvatar.leftLeg != null) _spawnedAvatar.leftLeg.SetLocalPose(_initialLeftFoot);
            if (_spawnedAvatar.rightLeg != null) _spawnedAvatar.rightLeg.SetLocalPose(_initialRightFoot);
        }

        protected void Awake()
        {
            _head = CreateConstraint(_spawnedAvatar.head, ref _initialHead);
            _leftHand = CreateConstraint(_spawnedAvatar.leftHand, ref _initialLeftHand);
            _rightHand = CreateConstraint(_spawnedAvatar.rightHand, ref _initialRightHand);
            _pelvis = CreateConstraint(_spawnedAvatar.pelvis, ref _initialPelvis);
            _leftFoot = CreateConstraint(_spawnedAvatar.leftLeg, ref _initialLeftFoot);
            _rightFoot = CreateConstraint(_spawnedAvatar.rightLeg, ref _initialRightFoot);

            _hasBody = _spawnedAvatar.body != null;
        }

        protected void OnEnable()
        {
            _avatarInput.inputChanged += OnInputChanged;
            OnInputChanged();

            if (_head != null) _head.constraintActive = true;
            if (_leftHand != null) _leftHand.constraintActive = true;
            if (_rightHand != null) _rightHand.constraintActive = true;
            if (_pelvis != null) _pelvis.constraintActive = true;
            if (_leftFoot != null) _leftFoot.constraintActive = true;
            if (_rightFoot != null) _rightFoot.constraintActive = true;

            UpdateConstraints();
        }

        protected void Update()
        {
            if (_hasBody)
            {
                Transform head = _spawnedAvatar.head;
                Transform body = _spawnedAvatar.body;

                body.position = head.position - head.up * 0.1f;

                Vector3 vel = new(body.localPosition.x - _prevBodyLocalPosition.x, 0.0f,
                    body.localPosition.z - _prevBodyLocalPosition.z);

                Quaternion rot = Quaternion.Euler(0.0f, head.localEulerAngles.y, 0.0f);
                Vector3 tiltAxis = Vector3.Cross(transform.up, vel);

                body.localRotation = Quaternion.Lerp(body.localRotation,
                    Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
                    Time.deltaTime * 10.0f);

                _prevBodyLocalPosition = body.localPosition;
            }
        }

        protected void OnDisable()
        {
            if (_head != null) _head.constraintActive = false;
            if (_leftHand != null) _leftHand.constraintActive = false;
            if (_rightHand != null) _rightHand.constraintActive = false;
            if (_pelvis != null) _pelvis.constraintActive = false;
            if (_leftFoot != null) _leftFoot.constraintActive = false;
            if (_rightFoot != null) _rightFoot.constraintActive = false;

            _avatarInput.inputChanged -= OnInputChanged;
        }

        private void OnInputChanged()
        {
            UpdateConstraints();
        }

        private ParentConstraint CreateConstraint(Transform transform, ref Pose initialPose)
        {
            if (transform == null)
            {
                return null;
            }

            initialPose = transform.GetLocalPose();

            ParentConstraint parentConstraint = transform.gameObject.AddComponent<ParentConstraint>();
            parentConstraint.weight = 1;
            parentConstraint.constraintActive = true;

            return parentConstraint;
        }

        private void UpdateConstraints()
        {
            UpdateConstraint(DeviceUse.Head, _head);
            UpdateConstraint(DeviceUse.LeftHand, _leftHand);
            UpdateConstraint(DeviceUse.RightHand, _rightHand);
            UpdateConstraint(DeviceUse.Waist, _pelvis);
            UpdateConstraint(DeviceUse.LeftFoot, _leftFoot);
            UpdateConstraint(DeviceUse.RightFoot, _rightFoot);
        }

        private void UpdateConstraint(DeviceUse deviceUse, ParentConstraint parentConstraint)
        {
            if (parentConstraint == null)
            {
                return;
            }

            if (!_avatarInput.TryGetTransform(deviceUse, out Transform target))
            {
                parentConstraint.SetSources(kEmptyConstraintSources);
                return;
            }

            parentConstraint.SetSources([new ConstraintSource { sourceTransform = target, weight = 1 }]);
        }
    }
}
