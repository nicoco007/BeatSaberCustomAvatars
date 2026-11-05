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

using CustomAvatar.Avatar;
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
            _trackingRig.isCalibratingChanged += OnIsCalibratingChanged;
            _playerAvatarManager.avatarChanged += OnAvatarChanged;

            UpdateState();
        }

        protected void LateUpdate()
        {
            Transform parent = transform.parent;
            Vector3 up = parent != null ? parent.up : Vector3.up;
            _trackingRig.head.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.forward, up), up);
            transform.SetPositionAndRotation(position + rotation * Vector3.Scale(transform.lossyScale, _playerAvatarManager.currentlySpawnedAvatar.prefab.headToRoot.position), rotation);
        }

        protected void OnDestroy()
        {
            _trackingRig.isCalibratingChanged -= OnIsCalibratingChanged;
            _playerAvatarManager.avatarChanged -= OnAvatarChanged;
        }

        private void OnIsCalibratingChanged(bool isCalibrating)
        {
            UpdateState();
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            enabled = _trackingRig.isCalibrating && _playerAvatarManager.currentlySpawnedAvatar != null;
        }
    }
}
