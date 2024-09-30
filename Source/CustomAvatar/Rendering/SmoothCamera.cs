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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    [DisallowMultipleComponent]
    internal class SmoothCamera : MonoBehaviour
    {
        private const float kCameraDefaultNearClipMask = 0.1f;

        private ILogger<SmoothCamera> _logger;
        private Settings _settings;

        private global::SmoothCamera _smoothCamera;
        private Camera _camera;
        private float _originalNearClipPlane;

        [Inject]
        public void Construct(ILogger<SmoothCamera> logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;

            _smoothCamera = GetComponent<global::SmoothCamera>();
            _camera = GetComponent<Camera>();
            _originalNearClipPlane = _camera.nearClipPlane;
        }

        protected void Start()
        {
            // prevent errors if this is instantiated via Object.Instantiate
            if (_logger == null || _settings == null)
            {
                Destroy(this);
                return;
            }

            _settings.forceCloseNearClipPlane.changed += OnCameraNearClipPlaneChanged;
            _settings.showAvatarInSmoothCamera.changed += OnShowAvatarInSmoothCameraChanged;

            UpdateSmoothCamera();
        }

        protected void OnDestroy()
        {
            if (_settings != null)
            {
                _settings.forceCloseNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
                _settings.showAvatarInSmoothCamera.changed -= OnShowAvatarInSmoothCameraChanged;
            }
        }

        private void OnCameraNearClipPlaneChanged(bool value)
        {
            UpdateSmoothCamera();
        }

        private void OnShowAvatarInSmoothCameraChanged(bool value)
        {
            UpdateSmoothCamera();
        }

        private void UpdateSmoothCamera()
        {
            _logger.LogInformation($"Setting avatar culling mask and near clip plane on '{_camera.name}'");

            if (!_settings.showAvatarInSmoothCamera)
            {
                _camera.cullingMask &= ~AvatarLayers.kAllLayersMask;
                _camera.nearClipPlane = kCameraDefaultNearClipMask;
            }
            else if (_smoothCamera._thirdPersonEnabled)
            {
                _camera.cullingMask = _camera.cullingMask | AvatarLayers.kOnlyInThirdPersonMask | AvatarLayers.kAlwaysVisibleMask;
                _camera.nearClipPlane = kCameraDefaultNearClipMask;
            }
            else
            {
                _camera.cullingMask = (_camera.cullingMask & ~AvatarLayers.kOnlyInThirdPersonMask) | AvatarLayers.kAlwaysVisibleMask;
                _camera.nearClipPlane = _settings.forceCloseNearClipPlane ? MainCamera.kCloseNearClipPlane : _originalNearClipPlane;
            }
        }
    }
}
