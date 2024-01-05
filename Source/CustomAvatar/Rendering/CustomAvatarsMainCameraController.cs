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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class CustomAvatarsMainCameraController : MonoBehaviour
    {
        private ILogger<CustomAvatarsMainCameraController> _logger;
        private Settings _settings;
        private ActivePlayerSpaceManager _activePlayerSpaceManager;
        private ActiveCameraManager _activeCameraManager;

        private Transform _parent;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            AddToPlayerSpaceManager();
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(ILogger<CustomAvatarsMainCameraController> logger, Settings settings, ActivePlayerSpaceManager activePlayerSpaceManager, ActiveCameraManager activeCameraManager)
        {
            _logger = logger;
            _settings = settings;
            _activePlayerSpaceManager = activePlayerSpaceManager;
            _activeCameraManager = activeCameraManager;
        }

        private void Start()
        {
            // prevent errors if this is instantiated via Object.Instantiate
            if (_logger == null || _settings == null || _activePlayerSpaceManager == null)
            {
                Destroy(this);
                return;
            }

            _settings.cameraNearClipPlane.changed += OnCameraNearClipPlaneChanged;

            UpdateCameraMask();

            AddToPlayerSpaceManager();
        }

        private void OnDestroy()
        {
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
            }

            RemoveFromPlayerSpaceManager();
        }

        private void OnCameraNearClipPlaneChanged(float value)
        {
            UpdateCameraMask();
        }

        private void UpdateCameraMask()
        {
            _logger.LogInformation($"Setting avatar culling mask and near clip plane on '{_camera.name}'");

            int mask = _camera.cullingMask | AvatarLayers.kAlwaysVisibleMask;

            // FPFC basically ends up being a 3rd person camera
            if (Environment.GetCommandLineArgs().Contains("fpfc"))
            {
                mask |= AvatarLayers.kOnlyInThirdPersonMask;
            }
            else
            {
                mask &= ~AvatarLayers.kOnlyInThirdPersonMask;
            }

            _camera.cullingMask = mask;
            _camera.nearClipPlane = _settings.cameraNearClipPlane;
        }

        private void AddToPlayerSpaceManager()
        {
            _parent = transform.parent;

            // this is simply to avoid the model flying around with the FPFC
            if (_parent != null && Environment.GetCommandLineArgs().Contains("fpfc"))
            {
                _parent = _parent.parent;
            }

            _activePlayerSpaceManager?.Add(_parent);
            _activeCameraManager?.Add(_camera);
        }

        private void RemoveFromPlayerSpaceManager()
        {
            _activePlayerSpaceManager?.Remove(_parent);
            _activeCameraManager?.Remove(_camera);
        }
    }
}
