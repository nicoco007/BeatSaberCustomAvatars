//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Tracking;
using UnityEngine;
using UnityEngine.Animations;
using Zenject;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
    internal class AvatarTransformTracking : MonoBehaviour
    {
        private static readonly List<ConstraintSource> kEmptyConstraintSources = new(0);

        private ParentConstraint _head;
        private ParentConstraint _leftHand;
        private ParentConstraint _rightHand;
        private ParentConstraint _pelvis;
        private ParentConstraint _leftFoot;
        private ParentConstraint _rightFoot;

        private SpawnedAvatar _spawnedAvatar;
        private IAvatarInput _avatarInput;

        private Vector3 _prevBodyLocalPosition;

        private void OnEnable()
        {
            if (_avatarInput != null)
            {
                _avatarInput.inputChanged += OnInputChanged;
            }

            if (_head != null) _head.constraintActive = true;
            if (_leftHand != null) _leftHand.constraintActive = true;
            if (_rightHand != null) _rightHand.constraintActive = true;
            if (_pelvis != null) _pelvis.constraintActive = true;
            if (_leftFoot != null) _leftFoot.constraintActive = true;
            if (_rightFoot != null) _rightFoot.constraintActive = true;

            UpdateConstraints();
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(SpawnedAvatar spawnedAvatar, IAvatarInput avatarInput)
        {
            _spawnedAvatar = spawnedAvatar;
            _avatarInput = avatarInput;
        }

        private void Start()
        {
            if (_avatarInput != null)
            {
                _avatarInput.inputChanged += OnInputChanged;
            }

            _head = CreateConstraint(_spawnedAvatar.head);
            _leftHand = CreateConstraint(_spawnedAvatar.leftHand);
            _rightHand = CreateConstraint(_spawnedAvatar.rightHand);
            _pelvis = CreateConstraint(_spawnedAvatar.pelvis);
            _leftFoot = CreateConstraint(_spawnedAvatar.leftLeg);
            _rightFoot = CreateConstraint(_spawnedAvatar.rightLeg);

            UpdateConstraints();
        }

        private void Update()
        {
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

        private void OnDisable()
        {
            if (_head != null) _head.constraintActive = false;
            if (_leftHand != null) _leftHand.constraintActive = false;
            if (_rightHand != null) _rightHand.constraintActive = false;
            if (_pelvis != null) _pelvis.constraintActive = false;
            if (_leftFoot != null) _leftFoot.constraintActive = false;
            if (_rightFoot != null) _rightFoot.constraintActive = false;

            if (_avatarInput != null)
            {
                _avatarInput.inputChanged -= OnInputChanged;
            }
        }

        private void OnInputChanged()
        {
            UpdateConstraints();
        }

        private ParentConstraint CreateConstraint(Transform transform)
        {
            if (transform == null)
            {
                return null;
            }

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
