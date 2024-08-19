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
        public static readonly float kDefaultPlayerHeight = PredefinedSettings.kDefaultPlayerHeight;
        public static readonly float kHeadPosToPlayerHeightOffset = PredefinedSettings.kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerEyeHeight = kDefaultPlayerHeight - kHeadPosToPlayerHeightOffset;
        public static readonly float kDefaultPlayerArmSpan = kDefaultPlayerHeight;

        private readonly SettingsManager _settingsManager;
        private readonly SettingsApplicatorSO _settingsApplicator;
        private readonly IVRPlatformHelper _vrPlatformHelper;

        internal BeatSaberUtilities(SettingsManager settingsManager, SettingsApplicatorSO settingsApplicator, IVRPlatformHelper vrPlatformHelper)
        {
            _settingsManager = settingsManager;
            _settingsApplicator = settingsApplicator;
            _vrPlatformHelper = vrPlatformHelper;
        }

        public Vector3 roomCenter => _settingsManager.settings.room.center;

        public Quaternion roomRotation => Quaternion.Euler(0, _settingsManager.settings.room.rotation, 0);

        public bool hasFocus => _vrPlatformHelper.hasInputFocus && _vrPlatformHelper.hasVrFocus;

        public event Action<Vector3, Quaternion> roomAdjustChanged;

        public event Action<bool> focusChanged;

        public event Action controllersChanged;

        public void Initialize()
        {
            _settingsApplicator.roomTransformOffsetDidUpdateEvent += OnRoomTransformOffsetUpdated;

            _vrPlatformHelper.inputFocusWasCapturedEvent += OnFocusWasChanged;
            _vrPlatformHelper.inputFocusWasReleasedEvent += OnFocusWasChanged;
            _vrPlatformHelper.vrFocusWasCapturedEvent += OnFocusWasChanged;
            _vrPlatformHelper.vrFocusWasReleasedEvent += OnFocusWasChanged;
            _vrPlatformHelper.controllersDidChangeReferenceEvent += OnControllersChanged;
            _vrPlatformHelper.controllersDidDisconnectEvent += OnControllersChanged;
        }

        public void Dispose()
        {
            _settingsApplicator.roomTransformOffsetDidUpdateEvent -= OnRoomTransformOffsetUpdated;

            _vrPlatformHelper.inputFocusWasCapturedEvent -= OnFocusWasChanged;
            _vrPlatformHelper.inputFocusWasReleasedEvent -= OnFocusWasChanged;
            _vrPlatformHelper.vrFocusWasCapturedEvent -= OnFocusWasChanged;
            _vrPlatformHelper.vrFocusWasReleasedEvent -= OnFocusWasChanged;
            _vrPlatformHelper.controllersDidChangeReferenceEvent -= OnControllersChanged;
            _vrPlatformHelper.controllersDidDisconnectEvent -= OnControllersChanged;
        }

        private void OnRoomTransformOffsetUpdated()
        {
            roomAdjustChanged?.Invoke(roomCenter, roomRotation);
        }

        private void OnFocusWasChanged()
        {
            focusChanged?.Invoke(hasFocus);
        }

        private void OnControllersChanged()
        {
            controllersChanged?.Invoke();
        }
    }
}
