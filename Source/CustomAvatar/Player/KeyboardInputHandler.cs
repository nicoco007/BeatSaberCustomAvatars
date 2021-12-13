//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class KeyboardInputHandler : ITickable
    {
        private readonly Settings _settings;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly ILogger<KeyboardInputHandler> _logger;

        public KeyboardInputHandler(Settings settings, PlayerAvatarManager avatarManager, ILogger<KeyboardInputHandler> logger)
        {
            _settings = settings;
            _avatarManager = avatarManager;
            _logger = logger;
        }

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                _ = _avatarManager.SwitchToNextAvatarAsync();
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                _ = _avatarManager.SwitchToPreviousAvatarAsync();
            }
            else if (Input.GetKeyDown(KeyCode.Home))
            {
                _settings.isAvatarVisibleInFirstPerson.value = !_settings.isAvatarVisibleInFirstPerson;
                _logger.Info($"{(_settings.isAvatarVisibleInFirstPerson.value ? "Enabled" : "Disabled")} first person visibility");
            }
            else if (Input.GetKeyDown(KeyCode.End))
            {
                _settings.resizeMode.value = (AvatarResizeMode)(((int)_settings.resizeMode.value + 1) % 3);
                _logger.Info($"Set resize mode to {_settings.resizeMode}");
            }
            else if (Input.GetKeyDown(KeyCode.Insert))
            {
                _settings.floorHeightAdjust.value = (FloorHeightAdjustMode)(((int)_settings.floorHeightAdjust.value + 1) % Enum.GetValues(typeof(FloorHeightAdjustMode)).Length);
                _logger.Info($"Set floor height adjust to {_settings.floorHeightAdjust}");
            }
        }
    }
}
