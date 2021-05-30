//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Player;
using CustomAvatar.Tracking;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class ArmSpanMeasurer : MonoBehaviour
    {
        private const float kStableMeasurementTimeout = 3f;
        private const float kMinDifferenceToReset = 0.02f;

        public event Action<float> updated;
        public event Action<float> completed;

        private IAvatarInput _playerInput;

        private bool _isMeasuring;
        private float _lastUpdateTime;
        private float _lastMeasuredArmSpan;

        [Inject]
        internal void Construct(VRPlayerInput playerInput)
        {
            _playerInput = playerInput;
        }

        public void MeasureArmSpan()
        {
            if (_isMeasuring) return;
            if (!_playerInput.TryGetPose(DeviceUse.LeftHand, out Pose _) || !_playerInput.TryGetPose(DeviceUse.RightHand, out Pose _)) return;

            _isMeasuring = true;
            _lastMeasuredArmSpan = 0;
            _lastUpdateTime = Time.timeSinceLevelLoad;

            InvokeRepeating(nameof(ScanArmSpan), 0.0f, 0.1f);
        }

        private void ScanArmSpan()
        {
            if (Time.timeSinceLevelLoad - _lastUpdateTime < kStableMeasurementTimeout && _playerInput.TryGetPose(DeviceUse.LeftHand, out Pose leftHand) && _playerInput.TryGetPose(DeviceUse.RightHand, out Pose rightHand))
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

                completed?.Invoke(_lastMeasuredArmSpan);
                _isMeasuring = false;
            }
        }
    }
}
