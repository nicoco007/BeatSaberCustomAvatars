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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    [RequireComponent(typeof(SmoothCamera))]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    internal class SmoothCamera : MonoBehaviour
    {
        private const float kCameraDefaultNearClipMask = 0.1f;

        private ILogger<SmoothCamera> _logger;
        private Settings _settings;
        private IFPFCSettings _fpfcSettings;
        private BeatSaberUtilities _beatSaberUtilities;

        private global::SmoothCamera _smoothCamera;
        private Camera _camera;

        [Inject]
        public void Construct(ILogger<SmoothCamera> logger, Settings settings, IFPFCSettings fpfcSettings, BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            _settings = settings;
            _fpfcSettings = fpfcSettings;
            _beatSaberUtilities = beatSaberUtilities;

            _smoothCamera = GetComponent<global::SmoothCamera>();
            _camera = GetComponent<Camera>();
        }

        protected void Start()
        {
            // prevent errors if this is instantiated via Object.Instantiate
            if (_logger == null || _settings == null)
            {
                Destroy(this);
                return;
            }

            _settings.cameraNearClipPlane.changed += OnCameraNearClipPlaneChanged;
            _settings.showAvatarInSmoothCamera.changed += OnShowAvatarInSmoothCameraChanged;

            _fpfcSettings.Changed += OnFpfcSettingsChanged;

            _beatSaberUtilities.focusChanged += OnFocusChanged;

            UpdateSmoothCamera();
        }

        protected void OnDestroy()
        {
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
                _settings.showAvatarInSmoothCamera.changed -= OnShowAvatarInSmoothCameraChanged;
            }

            if (_fpfcSettings != null)
            {
                _fpfcSettings.Changed -= OnFpfcSettingsChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged -= OnFocusChanged;
            }
        }

        private void OnCameraNearClipPlaneChanged(float value)
        {
            UpdateSmoothCamera();
        }

        private void OnShowAvatarInSmoothCameraChanged(bool value)
        {
            UpdateSmoothCamera();
        }

        private void OnFpfcSettingsChanged(IFPFCSettings fpfcSettings)
        {
            UpdateSmoothCamera();
        }

        private void OnFocusChanged(bool focused)
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
            else if (_smoothCamera._thirdPersonEnabled || _fpfcSettings.Enabled || (!_beatSaberUtilities.hasFocus && _settings.hmdCameraBehaviour == HmdCameraBehaviour.AllCameras)) // TODO: consolidate these conditions with the ones in MainCamera
            {
                _camera.cullingMask = _camera.cullingMask | AvatarLayers.kOnlyInThirdPersonMask | AvatarLayers.kAlwaysVisibleMask;
                _camera.nearClipPlane = kCameraDefaultNearClipMask;
            }
            else
            {
                _camera.cullingMask = (_camera.cullingMask & ~AvatarLayers.kOnlyInThirdPersonMask) | AvatarLayers.kAlwaysVisibleMask;
                _camera.nearClipPlane = _settings.cameraNearClipPlane;
            }
        }
    }
}
