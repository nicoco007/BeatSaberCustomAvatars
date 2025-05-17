﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    [DisallowMultipleComponent]
    internal class EnvironmentObject : MonoBehaviour
    {
        private static readonly Dictionary<MirrorRendererSO, MirrorRendererSO> kReplacedMirrorRenderers = new();

        private SaberBurnMarkSparkles[] _saberBurnMarkSparkles;
        private SaberBurnMarkArea[] _saberBurnMarkAreas;

        private ILogger<EnvironmentObject> _logger;
        private float _originalY;

        protected PlayerAvatarManager playerAvatarManager { get; private set; }

        protected Settings settings { get; private set; }

        protected BeatSaberUtilities beatSaberUtilities { get; private set; }

        protected void Awake()
        {
            _originalY = transform.position.y;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>().Where(mr => mr.isPartOfStaticBatch))
            {
                renderer.staticBatchRootTransform = transform;
            }

            _saberBurnMarkSparkles = GetComponentsInChildren<SaberBurnMarkSparkles>();
            _saberBurnMarkAreas = GetComponentsInChildren<SaberBurnMarkArea>();
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(ILogger<EnvironmentObject> logger, PlayerAvatarManager playerAvatarManager, Settings settings, BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            this.playerAvatarManager = playerAvatarManager;
            this.settings = settings;
            this.beatSaberUtilities = beatSaberUtilities;
        }

        protected virtual void Start()
        {
            playerAvatarManager.avatarChanged += OnAvatarChanged;
            playerAvatarManager.avatarScaleChanged += OnAvatarScaleChanged;
            settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;

            UpdateOffset();
            CreateMirrors();
        }

        protected virtual void OnDestroy()
        {
            playerAvatarManager.avatarChanged -= OnAvatarChanged;
            playerAvatarManager.avatarScaleChanged -= OnAvatarScaleChanged;
            settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
        }

        protected virtual void UpdateOffset()
        {
            float floorOffset = playerAvatarManager.GetFloorOffset();

            transform.position = new Vector3(transform.position.x, _originalY + floorOffset, transform.position.z);

            foreach (SaberBurnMarkSparkles saberBurnMarkSparkles in _saberBurnMarkSparkles)
            {
                saberBurnMarkSparkles._plane = new Plane(saberBurnMarkSparkles.transform.up, saberBurnMarkSparkles.transform.position);
            }

            foreach (SaberBurnMarkArea saberBurnMarkArea in _saberBurnMarkAreas)
            {
                saberBurnMarkArea._plane = new Plane(saberBurnMarkArea.transform.up, saberBurnMarkArea.transform.position);
            }
        }

        // TODO: this should be re-run when offset is changed; there are
        // no mirrors in the menu so this isn't currently a real problem
        private void CreateMirrors()
        {
            if (playerAvatarManager.GetFloorOffset() == 0)
            {
                return;
            }

            foreach (Mirror mirror in GetComponentsInChildren<Mirror>())
            {
                _logger.LogTrace($"Replacing {nameof(MirrorRendererSO)} on '{mirror.name}'");

                MirrorRendererSO original = mirror._mirrorRenderer;

                // Since every EnvironmentObject will move by the same amount, we can assume any mirror under
                // any EnvironmentObject using a given MirrorRendererSO will be on the same plane.
                if (!kReplacedMirrorRenderers.TryGetValue(original, out MirrorRendererSO renderer) || renderer == null)
                {
                    kReplacedMirrorRenderers.Remove(original);

                    _logger.LogTrace($"Creating new {nameof(MirrorRendererSO)} for '{mirror.name}'");

                    renderer = Instantiate(original);
                    renderer.name = original.name + " (Moved Floor Instance)";

                    // Since these MirrorRendererSOs are reused and never unloaded, might as well keep them in the dictionary as long as the game is running.
                    kReplacedMirrorRenderers.Add(original, renderer);
                }

                mirror._mirrorRenderer = renderer;
            }
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            UpdateOffset();
        }

        private void OnAvatarScaleChanged(float scale)
        {
            UpdateOffset();
        }

        private void OnFloorHeightAdjustChanged(FloorHeightAdjustMode value)
        {
            UpdateOffset();
        }
    }
}
