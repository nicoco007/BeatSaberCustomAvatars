//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Utilities
{
    internal class BeatSaberUtilities : IInitializable, IDisposable
    {
        public static readonly float kDefaultPlayerHeight = MainSettingsModelSO.kDefaultPlayerHeight;
        public static readonly float kHeadPosToPlayerHeightOffset = MainSettingsModelSO.kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerEyeHeight = kDefaultPlayerHeight - kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerArmSpan = kDefaultPlayerHeight;

        private readonly MainSettingsModelSO _mainSettingsModel;

        internal BeatSaberUtilities(MainSettingsModelSO mainSettingsModel)
        {
            _mainSettingsModel = mainSettingsModel;
        }

        public Vector3 roomCenter => _mainSettingsModel.roomCenter;

        public Quaternion roomRotation => Quaternion.Euler(0, _mainSettingsModel.roomRotation, 0);

        public event Action<Vector3, Quaternion> roomAdjustChanged;

        public void Initialize()
        {
            _mainSettingsModel.roomCenter.didChangeEvent += OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent += OnRoomRotationChanged;
        }

        public void Dispose()
        {
            _mainSettingsModel.roomCenter.didChangeEvent -= OnRoomCenterChanged;
            _mainSettingsModel.roomRotation.didChangeEvent -= OnRoomRotationChanged;
        }

        private void OnRoomCenterChanged()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }

        private void OnRoomRotationChanged()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }
    }
}
