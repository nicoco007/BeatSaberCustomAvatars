using CustomAvatar.Player;
using CustomAvatar.Tracking;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class ArmSpanMeasurer : MonoBehaviour
    {
        private const float kMinArmSpan = 0.5f;
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
            _lastMeasuredArmSpan = kMinArmSpan;
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

                _lastMeasuredArmSpan = Mathf.Max(kMinArmSpan, (_lastMeasuredArmSpan + armSpan) / 2);

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
