//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Logging;
using CustomAvatar.Rendering;
using UnityEngine;
using UnityEngine.SpatialTracking;
using Zenject;

namespace CustomAvatar.Replays
{
    internal class BeatLeaderReplayHandler : IInitializable
    {
        private readonly DiContainer _container;
        private readonly ILogger<BeatLeaderReplayHandler> _logger;
        private readonly PlayerTransforms _playerTransforms;

        protected BeatLeaderReplayHandler(DiContainer container, ILogger<BeatLeaderReplayHandler> logger, PlayerTransforms playerTransforms)
        {
            _container = container;
            _logger = logger;
            _playerTransforms = playerTransforms;
        }

        public void Initialize()
        {
            Transform replayerCore = _playerTransforms.transform.Find("ReplayerCore");

            if (replayerCore == null)
            {
                return;
            }

            Camera camera = replayerCore.GetComponentInChildren<Camera>(true);

            if (camera == null)
            {
                _logger.LogError("Failed to find camera");
                return;
            }

            if (!camera.TryGetComponent(out TrackedPoseDriver _))
            {
                return;
            }

            GameObject gameObject = camera.gameObject;
            Transform playerSpace = camera.transform.parent;

            while (playerSpace != null && playerSpace.name != "CenterAdjust")
            {
                playerSpace = playerSpace.parent;
            }

            if (playerSpace == null)
            {
                _logger.LogError("Failed to find CenterAdjust");
                return;
            }

            _container.InstantiateComponent<SpectatorCameraTracker>(gameObject).Init(playerSpace, playerSpace.parent);
        }
    }
}
