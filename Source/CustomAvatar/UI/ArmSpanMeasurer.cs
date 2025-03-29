//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Player;
using CustomAvatar.Tracking;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    [DisallowMultipleComponent]
    internal class ArmSpanMeasurer : MonoBehaviour
    {
        private const float kStableMeasurementTimeout = 3f;
        private const float kMinDifferenceToReset = 0.02f;

        public event Action<float> updated;
        public event Action<float> completed;

        private IAvatarInput _playerInput;

        private float _lastUpdateTime;
        private float _lastMeasuredArmSpan;

        public bool isMeasuring { get; private set; }

        [Inject]
        internal void Construct(VRPlayerInput playerInput)
        {
            _playerInput = playerInput;
        }

        public void MeasureArmSpan()
        {
            if (isMeasuring) return;
            if (!_playerInput.TryGetTransform(DeviceUse.LeftHand, out Transform _) || !_playerInput.TryGetTransform(DeviceUse.RightHand, out Transform _)) return;

            isMeasuring = true;
            _lastMeasuredArmSpan = 0;
            _lastUpdateTime = Time.timeSinceLevelLoad;

            InvokeRepeating(nameof(ScanArmSpan), 0.0f, 0.1f);
        }

        public void Cancel()
        {
            if (!isMeasuring) return;

            CancelInvoke(nameof(ScanArmSpan));

            isMeasuring = false;
        }

        private void ScanArmSpan()
        {
            if (Time.timeSinceLevelLoad - _lastUpdateTime < kStableMeasurementTimeout && _playerInput.TryGetTransform(DeviceUse.LeftHand, out Transform leftHand) && _playerInput.TryGetTransform(DeviceUse.RightHand, out Transform rightHand))
            {
                float armSpan = Vector3.Distance(leftHand.position, rightHand.position);

                if (Mathf.Abs(armSpan - _lastMeasuredArmSpan) >= kMinDifferenceToReset)
                {
                    _lastUpdateTime = Time.timeSinceLevelLoad;
                }

                _lastMeasuredArmSpan = (_lastMeasuredArmSpan + armSpan) / 2;

                updated?.Invoke(_lastMeasuredArmSpan);
            }
            else
            {
                CancelInvoke(nameof(ScanArmSpan));

                isMeasuring = false;
                completed?.Invoke(_lastMeasuredArmSpan);
            }
        }
    }
}
