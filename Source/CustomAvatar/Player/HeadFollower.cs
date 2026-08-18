using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class HeadFollower : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;
        private TrackingRig _trackingRig;

        [Inject]
        protected void Construct(PlayerAvatarManager playerAvatarManager, TrackingRig trackingRig)
        {
            _playerAvatarManager = playerAvatarManager;
            _trackingRig = trackingRig;
        }

        protected void Start()
        {
            _trackingRig.activeCalibrationModeChanged += OnActiveCalibrationModeChanged;
            _playerAvatarManager.avatarChanged += OnAvatarChanged;

            UpdateState();
        }

        protected void LateUpdate()
        {
            Transform parent = transform.parent;
            Vector3 up = parent != null ? parent.up : Vector3.up;
            _trackingRig.head.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position + Vector3.Scale(transform.lossyScale, _playerAvatarManager.currentlySpawnedAvatar.prefab.headToRoot.position), Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.forward, up), up));
        }

        protected void OnDestroy()
        {
            _trackingRig.activeCalibrationModeChanged -= OnActiveCalibrationModeChanged;
            _playerAvatarManager.avatarChanged -= OnAvatarChanged;
        }

        private void OnActiveCalibrationModeChanged(CalibrationMode calibrationMode)
        {
            UpdateState();
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            enabled = _trackingRig.activeCalibrationMode is not CalibrationMode.None && _playerAvatarManager.currentlySpawnedAvatar != null;
        }
    }
}
