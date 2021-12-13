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
using CustomAvatar.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    [Obsolete]
    public class FloorController : IInitializable, IDisposable
    {
        public float floorOffset => _playerAvatarManager.GetFloorOffset();
        public float floorPosition => floorOffset + (_settings.moveFloorWithRoomAdjust ? _beatSaberUtilities.roomCenter.y : 0);

        public event Action<float> floorPositionChanged;

        private readonly PlayerAvatarManager _playerAvatarManager;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly Settings _settings;

        internal FloorController(PlayerAvatarManager playerAvatarManager, BeatSaberUtilities beatSaberUtilities, Settings settings)
        {
            _playerAvatarManager = playerAvatarManager;
            _beatSaberUtilities = beatSaberUtilities;
            _settings = settings;
        }

        public void Initialize()
        {
            _playerAvatarManager.avatarChanged += OnAvatarChanged;
            _playerAvatarManager.avatarScaleChanged += OnAvatarScaleChanged;
            _beatSaberUtilities.roomAdjustChanged += OnRoomCenterChanged;
            _settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;
            _settings.moveFloorWithRoomAdjust.changed += OnMoveFloorWithRoomAdjustChanged;
        }

        public void Dispose()
        {
            _playerAvatarManager.avatarChanged -= OnAvatarChanged;
            _playerAvatarManager.avatarScaleChanged -= OnAvatarScaleChanged;
            _beatSaberUtilities.roomAdjustChanged -= OnRoomCenterChanged;
            _settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
            _settings.moveFloorWithRoomAdjust.changed -= OnMoveFloorWithRoomAdjustChanged;
        }

        private void OnRoomCenterChanged(Vector3 roomCenter, Quaternion roomRotation)
        {
            floorPositionChanged?.Invoke(floorPosition);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            floorPositionChanged?.Invoke(floorPosition);
        }

        private void OnAvatarScaleChanged(float scale)
        {
            floorPositionChanged?.Invoke(floorPosition);
        }

        private void OnFloorHeightAdjustChanged(FloorHeightAdjustMode floorHeightAdjust)
        {
            floorPositionChanged?.Invoke(floorPosition);
        }

        private void OnMoveFloorWithRoomAdjustChanged(bool enabled)
        {
            floorPositionChanged?.Invoke(floorPosition);
        }
    }
}
