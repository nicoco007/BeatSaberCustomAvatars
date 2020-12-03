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
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Player
{
    public class FloorController : IInitializable, IDisposable
    {
        public float floorOffset { get; private set; }
        public float floorPosition { get; private set; }

        public event Action<float> floorPositionChanged;

        private readonly ILogger<FloorController> _logger;
        private readonly Settings _settings;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly GameScenesManager _gameScenesManager;

        private readonly string[] _playersPlaceObjectNames = { "MenuEnvironment", "Environment/PlayersPlace", "Environment/PlayersPlaceShadow" };
        private readonly string[] _environmentObjectNames = { "MenuEnvironment", "Environment" };

        private readonly Dictionary<Transform, Vector3> _originalPositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<MirrorRendererSO, MirrorRendererReplacer> _mirrorRenderers = new Dictionary<MirrorRendererSO, MirrorRendererReplacer>();

        internal FloorController(ILoggerProvider loggerProvider, Settings settings, BeatSaberUtilities beatSaberUtilities, GameScenesManager gameScenesManager)
        {
            _logger = loggerProvider.CreateLogger<FloorController>();
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
            _gameScenesManager = gameScenesManager;
        }

        public void Initialize()
        {
            _beatSaberUtilities.roomCenterChanged += OnRoomCenterChanged;
            _gameScenesManager.transitionDidFinishEvent += OnSceneTransitionDidFinish;
        }

        public void Dispose()
        {
            _beatSaberUtilities.roomCenterChanged -= OnRoomCenterChanged;
            _gameScenesManager.transitionDidFinishEvent -= OnSceneTransitionDidFinish;
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
            RemoveDestroyedMirrorRenderers();
            RemoveDestroyedTransforms();

            string[] floorObjectNames;

            switch (_settings.floorHeightAdjust)
            {
                case FloorHeightAdjust.PlayersPlaceOnly:
                    floorObjectNames = _playersPlaceObjectNames;
                    break;

                case FloorHeightAdjust.EntireEnvironment:
                    floorObjectNames = _environmentObjectNames;
                    break;

                default:
                    ResetFloorObjects();
                    return;
            }

            foreach (var floorObjectName in floorObjectNames)
            {
                GameObject floorObject = GameObject.Find(floorObjectName);

                if (!floorObject) continue;

                _logger.Info($"Moving '{floorObjectName}' to {floorPosition:0.000} m");

                if (!_originalPositions.ContainsKey(floorObject.transform))
                {
                    _originalPositions.Add(floorObject.transform, floorObject.transform.position);
                }

                floorObject.transform.position = _originalPositions[floorObject.transform] + new Vector3(0, floorPosition, 0);

                foreach (Mirror mirror in floorObject.GetComponentsInChildren<Mirror>())
                {
                    MirrorRendererSO mirrorRenderer = mirror.GetPrivateField<MirrorRendererSO>("_mirrorRenderer");

                    if (!_mirrorRenderers.ContainsKey(mirrorRenderer))
                    {
                        _mirrorRenderers.Add(mirrorRenderer, new MirrorRendererReplacer(mirrorRenderer));
                    }

                    _mirrorRenderers[mirrorRenderer].AddMirror(mirror);
                }
            }
        }

        private void ResetFloorObjects()
        {
            foreach (KeyValuePair<Transform, Vector3> kvp in _originalPositions)
            {
                if (!kvp.Key) continue;

                kvp.Key.position = kvp.Value;
            }

            _originalPositions.Clear();
        }

        private void RemoveDestroyedMirrorRenderers()
        {
            var renderersToDestroy = new List<KeyValuePair<MirrorRendererSO, MirrorRendererReplacer>>();

            foreach (KeyValuePair<MirrorRendererSO, MirrorRendererReplacer> kvp in _mirrorRenderers)
            {
                if (kvp.Value.AreAllMirrorsDestroyed())
                {
                    renderersToDestroy.Add(kvp);
                }
            }

            foreach (KeyValuePair<MirrorRendererSO, MirrorRendererReplacer> kvp in renderersToDestroy)
            {
                kvp.Value.Dispose();
                _mirrorRenderers.Remove(kvp.Key);
            }
        }

        private void RemoveDestroyedTransforms()
        {
            var transformsToRemove = new List<Transform>();

            foreach (KeyValuePair<Transform, Vector3> kvp in _originalPositions)
            {
                if (!kvp.Key)
                {
                    transformsToRemove.Add(kvp.Key);
                }
            }

            foreach (Transform transform in transformsToRemove)
            {
                _originalPositions.Remove(transform);
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

        private readonly struct MirrorRendererReplacer : IDisposable
        {
            public readonly MirrorRendererSO renderer;
            private readonly List<Mirror> mirrors;

            public MirrorRendererReplacer(MirrorRendererSO original)
            {
                renderer = Object.Instantiate(original);
                renderer.name = original.name + " (Floor Instance)";

                mirrors = new List<Mirror>();
            }

            public void AddMirror(Mirror mirror)
            {
                mirrors.Add(mirror);
                mirror.SetPrivateField("_mirrorRenderer", renderer);
            }

            public bool AreAllMirrorsDestroyed()
            {
                return mirrors.TrueForAll(m => !m);
            }

            public void Dispose()
            {
                Object.Destroy(renderer);
                mirrors.Clear();
            }
        }
    }
}
