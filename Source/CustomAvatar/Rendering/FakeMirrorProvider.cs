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
using CustomAvatar.Player;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class FakeMirrorProvider : MonoBehaviour
    {
        [SerializeField] private FakeMirror _fakeMirror;

        private PlayerAvatarManager _playerAvatarManager;

        [Inject]
        protected void Construct(PlayerAvatarManager playerAvatarManager)
        {
            _playerAvatarManager = playerAvatarManager;
        }

        protected void OnEnable()
        {
            _playerAvatarManager.avatarLoading += OnAvatarLoading;
            _playerAvatarManager.avatarChanged += OnAvatarChanged;

            DestroyAvatar();
            CreateAvatar(_playerAvatarManager.currentlySpawnedAvatar);
        }

        protected void OnDisable()
        {
            _playerAvatarManager.avatarLoading -= OnAvatarLoading;
            _playerAvatarManager.avatarChanged -= OnAvatarChanged;
        }

        private void OnAvatarLoading(string fullPath, string name)
        {
            DestroyAvatar();
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            CreateAvatar(avatar);
        }

        private void CreateAvatar(SpawnedAvatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            _fakeMirror.Set(avatar.transform);
        }

        private void DestroyAvatar()
        {
            _fakeMirror.Clear();
        }
    }
}
