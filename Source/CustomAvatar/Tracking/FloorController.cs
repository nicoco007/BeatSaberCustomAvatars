//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Tracking
{
    public class FloorController
    {
        public float floorOffset { get; private set; }
        public float floorPosition { get; private set; }

        public event Action<float> floorPositionChanged;

        private readonly ILogger<FloorController> _logger;
        private readonly Settings _settings;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly GameScenesManager _gameScenesManager;

        private readonly string[] _floorObjectNames = { "MenuEnvironment", "Environment/PlayersPlace" };

        internal FloorController(ILoggerProvider loggerProvider, Settings settings, BeatSaberUtilities beatSaberUtilities, GameScenesManager gameScenesManager)
        {
            _logger = loggerProvider.CreateLogger<FloorController>();
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
            _gameScenesManager = gameScenesManager;

            _beatSaberUtilities.roomCenterChanged += OnRoomCenterChanged;
            _gameScenesManager.transitionDidFinishEvent += OnSceneTransitionDidFinish;
        }

        internal void SetFloorOffset(float offset)
        {
            floorOffset = offset;

            if (_settings.moveFloorWithRoomAdjust)
            {
                floorPosition = offset + _beatSaberUtilities.roomCenter.y;
            }
            else
            {
                floorPosition = offset;
            }

            UpdateFloorObjects();

            floorPositionChanged?.Invoke(floorPosition);
        }

        private void UpdateFloorObjects()
        {
            foreach (var floorObjectName in _floorObjectNames)
            {
                GameObject floorObject = GameObject.Find(floorObjectName);

                if (!floorObject) continue;

                _logger.Info($"Moving '{floorObjectName}' to {floorPosition:0.000} m");

                floorObject.transform.position = new Vector3(0, floorPosition, 0);
            }
        }

        private void OnRoomCenterChanged(Vector3 center)
        {
            SetFloorOffset(floorOffset);
        }

        private void OnSceneTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            UpdateFloorObjects();
        }
    }
}
