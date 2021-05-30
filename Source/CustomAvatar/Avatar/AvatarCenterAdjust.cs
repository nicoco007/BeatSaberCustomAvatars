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
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    internal class AvatarCenterAdjust : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;

        [Inject]
        public void Construct(PlayerAvatarManager playerAvatarManager)
        {
            _playerAvatarManager = playerAvatarManager;
        }

        public void OnEnable()
        {
            _playerAvatarManager?.SetParent(transform);
        }

        public void Start()
        {
            OnEnable();
        }

        public void OnDestroy()
        {
            _playerAvatarManager?.SetParent(null);
        }
    }
}
