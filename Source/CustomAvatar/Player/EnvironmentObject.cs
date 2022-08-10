//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class EnvironmentObject : MonoBehaviour
    {
        // found this property through UnityExplorer, hopefully it doesn't disappear in future versions of Unity
        private static readonly Action<Renderer, Transform> kStaticBatchRootTransformSetter = ReflectionExtensions.CreatePropertySetter<Renderer, Transform>("staticBatchRootTransform");

        private static readonly Dictionary<Plane, MirrorRendererSO> kReplacedMirrorRenderers = new Dictionary<Plane, MirrorRendererSO>();

        private ILogger<EnvironmentObject> _logger;
        private float _originalY;

        protected PlayerAvatarManager playerAvatarManager { get; private set; }

        protected Settings settings { get; private set; }

        protected BeatSaberUtilities beatSaberUtilities { get; private set; }

        internal void Awake()
        {
            _originalY = transform.position.y;

            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>().Where(mr => mr.isPartOfStaticBatch))
            {
                kStaticBatchRootTransformSetter(renderer, transform);
            }
        }

        [Inject]
        internal void Construct(ILogger<EnvironmentObject> logger, PlayerAvatarManager playerAvatarManager, Settings settings, BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            this.playerAvatarManager = playerAvatarManager;
            this.settings = settings;
            this.beatSaberUtilities = beatSaberUtilities;
        }

        internal virtual void Start()
        {
            playerAvatarManager.avatarChanged += OnAvatarChanged;
            playerAvatarManager.avatarScaleChanged += OnAvatarScaleChanged;
            settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;

            UpdateOffset();
            CreateMirrors();
        }

        internal virtual void OnDestroy()
        {
            playerAvatarManager.avatarChanged -= OnAvatarChanged;
            playerAvatarManager.avatarScaleChanged -= OnAvatarScaleChanged;
            settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
        }

        protected virtual void UpdateOffset()
        {
            float floorOffset = playerAvatarManager.GetFloorOffset();

            if (settings.moveFloorWithRoomAdjust)
            {
                floorOffset += beatSaberUtilities.roomCenter.y;
            }

            transform.position = new Vector3(transform.position.x, _originalY + floorOffset, transform.position.z);
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

                MirrorRendererSO original = mirror.GetField<MirrorRendererSO, Mirror>("_mirrorRenderer");
                Transform t = mirror.transform;

                var plane = new Plane(t.up, t.position);
                plane.distance = Mathf.Round(plane.distance / 1000) * 1000;

                if (!kReplacedMirrorRenderers.TryGetValue(plane, out MirrorRendererSO renderer) || renderer == null)
                {
                    kReplacedMirrorRenderers.Remove(plane);

                    _logger.LogTrace($"Creating new {nameof(MirrorRendererSO)} for '{mirror.name}'");

                    // TODO: these should be destroyed when they are no longer used; they shouldn't
                    // cause performance issues by sticking around but should be cleaned up eventually
                    renderer = Instantiate(original);

                    kReplacedMirrorRenderers.Add(plane, renderer);
                }

                renderer.name = original.name + " (Moved Floor Instance)";
                mirror.SetField("_mirrorRenderer", renderer);
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
