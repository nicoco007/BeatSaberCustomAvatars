using System;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    public class RoomAdjustedInput : IAvatarInput
    {
        private IAvatarInput _input;
        private MainSettingsModelSO _mainSettingsModel;

        public RoomAdjustedInput(IAvatarInput input, MainSettingsModelSO mainSettingsModel)
        {
            _input = input;
            _mainSettingsModel = mainSettingsModel;

            inputChanged += OnInputChanged;
        }

        public bool allowMaintainPelvisPosition => _input.allowMaintainPelvisPosition;

        public event Action inputChanged;

        public bool TryGetPose(DeviceUse use, out Pose pose)
        {
            if (!_input.TryGetPose(use, out pose))
            {
                pose = Pose.identity;
                return false;
            }

            Vector3 origin = _mainSettingsModel.roomCenter.value;
            Quaternion originRotation = Quaternion.Euler(0, _mainSettingsModel.roomRotation.value, 0);

            pose.position = origin + originRotation * pose.position;
            pose.rotation = originRotation * pose.rotation;

            return true;
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl)
        {
            return _input.TryGetFingerCurl(use, out curl);
        }

        public void Dispose()
        {
            _input.Dispose();
        }

        private void OnInputChanged()
        {
            inputChanged?.Invoke();
        }
    }
}
