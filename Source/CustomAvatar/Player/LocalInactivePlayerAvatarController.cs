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

using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    /// <summary>
    /// Moves the local player's avatar around when in spectator mode in multiplayer. This will eventually
    /// be removed in favor of something similar to the in-game avatar's way of spawning player avatars.
    /// </summary>
    internal class LocalInactivePlayerAvatarController : IInitializable, IDisposable
    {
        private readonly PlayerAvatarManager _playerAvatarManager;
        private readonly MultiplayerSpectatorController _spectatorController;

        [Inject]
        public LocalInactivePlayerAvatarController(PlayerAvatarManager playerAvatarManager, MultiplayerSpectatorController spectatorController)
        {
            _playerAvatarManager = playerAvatarManager;
            _spectatorController = spectatorController;
        }

        public void Initialize()
        {
            _spectatorController.spectatingSpotDidChangeEvent += OnSpectatingSpotDidChange;

            if (_spectatorController.currentSpot != null) OnSpectatingSpotDidChange(_spectatorController.currentSpot);
        }

        public void Dispose()
        {
            _spectatorController.spectatingSpotDidChangeEvent -= OnSpectatingSpotDidChange;

            _playerAvatarManager.Move(Vector3.zero, Quaternion.identity);
        }

        private void OnSpectatingSpotDidChange(IMultiplayerSpectatingSpot spectatingSpot)
        {
            _playerAvatarManager.Move(spectatingSpot.transform.position, spectatingSpot.transform.rotation);
        }
    }
}
