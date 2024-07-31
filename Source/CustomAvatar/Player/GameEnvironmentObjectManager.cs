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
        internal static class BeatLeaderReflection
        {
            internal static readonly Type kCameraControllerType = Type.GetType("BeatLeader.Replayer.ReplayerCameraController, BeatLeader");
            internal static readonly Type kOriginComponentType = Type.GetType("BeatLeader.Replayer.ReplayerExtraObjectsProvider, BeatLeader");
            internal static readonly Func<object, Camera> kCameraField = kCameraControllerType?.CreateFieldGetter<Camera>("_camera");
            internal static readonly Func<object, Transform> kReplayerCoreGetter = kOriginComponentType?.CreatePropertyGetter<Transform>("ReplayerCore");
            internal static readonly Func<object, Transform> kReplayerCenterAdjustGetter = kOriginComponentType?.CreatePropertyGetter<Transform>("ReplayerCenterAdjust");

            internal static bool kAvailable = kCameraControllerType != null && kOriginComponentType != null && kCameraField != null && kReplayerCoreGetter != null && kReplayerCenterAdjustGetter != null;
        }

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

            HandleScoreSaberSpectatorCamera();
            HandleBeatLeaderSpectatorCamera();
        }

        private void HandleScoreSaberSpectatorCamera()
        {
            var spectatorParent = GameObject.Find(kSpectatorObjectPath);

            if (spectatorParent == null)
            {
                return;
            }

            Camera spectatorCamera = spectatorParent.GetComponentInChildren<Camera>();

            if (spectatorCamera == null)
            {
                return;
            }

            Transform origin = new GameObject("Origin").transform;
            Transform playerSpace = spectatorParent.transform;

            // assuming roomCenter and roomRotation won't change while spectating
            var inverseRotation = Quaternion.Inverse(_beatSaberUtilities.roomRotation);
            origin.SetLocalPositionAndRotation(inverseRotation * -_beatSaberUtilities.roomCenter, inverseRotation);
            origin.SetParent(playerSpace, false);

            SpectatorCamera spectatorCameraController = _container.InstantiateComponent<SpectatorCamera>(spectatorCamera.gameObject);
            spectatorCameraController.origin = origin;
            spectatorCameraController.playerSpace = playerSpace;
        }

        private void HandleBeatLeaderSpectatorCamera()
        {
            if (!BeatLeaderReflection.kAvailable)
            {
                return;
            }

            var controller = (Component)_container.TryResolve(BeatLeaderReflection.kCameraControllerType);
            var originComponent = (Component)_container.TryResolve(BeatLeaderReflection.kOriginComponentType);
            Camera camera = BeatLeaderReflection.kCameraField(controller);

            if (controller == null || originComponent == null || camera == null)
            {
                return;
            }

            SpectatorCamera spectatorCameraController = _container.InstantiateComponent<SpectatorCamera>(camera.gameObject);
            spectatorCameraController.origin = BeatLeaderReflection.kReplayerCoreGetter(originComponent);
            spectatorCameraController.playerSpace = BeatLeaderReflection.kReplayerCenterAdjustGetter(originComponent);
        }
    }
}
