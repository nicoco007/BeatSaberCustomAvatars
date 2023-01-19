//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Rendering;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class GameEnvironmentObjectManager : IInitializable
    {
        private const string kEnvironmentObjectPath = "/Environment";
        private const string kSpectatorObjectPath = "/SpectatorParent";

        private readonly DiContainer _container;
        private readonly ILogger<GameEnvironmentObjectManager> _logger;
        private readonly Settings _settings;
        private readonly BeatSaberUtilities _beatSaberUtilities;

        internal GameEnvironmentObjectManager(DiContainer container, ILogger<GameEnvironmentObjectManager> logger, Settings settings, BeatSaberUtilities beatSaberUtilities)
        {
            _container = container;
            _logger = logger;
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
        }

        public void Initialize()
        {
            var environment = GameObject.Find(kEnvironmentObjectPath);

            if (environment != null)
            {
                switch (_settings.floorHeightAdjust.value)
                {
                    case FloorHeightAdjustMode.EntireEnvironment:
                        _container.InstantiateComponent<EnvironmentObject>(environment);
                        break;

                    case FloorHeightAdjustMode.PlayersPlaceOnly:
                        Transform environmentTransform = environment.transform;
                        Transform playersPlace = environmentTransform.Find("PlayersPlace");

                        if (playersPlace)
                        {
                            _container.InstantiateComponent<EnvironmentObject>(playersPlace.gameObject);
                        }
                        else
                        {
                            _logger.LogWarning($"PlayersPlace not found!");
                        }

                        break;
                }
            }
            else
            {
                _logger.LogWarning($"{kEnvironmentObjectPath} not found!");
            }

            // ScoreSaber replay spectator camera
            var spectatorParent = GameObject.Find(kSpectatorObjectPath);

            if (spectatorParent != null)
            {
                // "SpectatorParent" has position room adjust applied but not rotation
                var avatarParent = new GameObject("AvatarParent");
                Transform avatarParentTransform = avatarParent.transform;
                avatarParentTransform.localRotation = _beatSaberUtilities.roomRotation;
                avatarParentTransform.SetParent(spectatorParent.transform, false);

                Camera spectatorCamera = spectatorParent.GetComponentInChildren<Camera>();

                if (spectatorCamera != null)
                {
                    _container.InstantiateComponent<CustomAvatarsMainCameraController>(spectatorCamera.gameObject);
                }
                else
                {
                    _logger.LogWarning($"Spectator camera not found!");
                }
            }
        }
    }
}
