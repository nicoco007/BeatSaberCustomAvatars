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

using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using UnityEngine.SpatialTracking;
using Zenject;

namespace CustomAvatar.Rendering
{
    [DisallowMultipleComponent]
    internal class MainCamera : MonoBehaviour
    {
        private ILogger<MainCamera> _logger;
        private Settings _settings;
        private ActivePlayerSpaceManager _activePlayerSpaceManager;
        private ActiveOriginManager _activeOriginManager;
        private ActiveCameraManager _activeCameraManager;
        private IFPFCSettings _fpfcSettings;
        private BeatSaberUtilities _beatSaberUtilities;

        private Transform _playerSpace;
        private Transform _origin;
        private Camera _camera;
        private TrackedPoseDriver _trackedPoseDriver;

        protected virtual (Transform playerSpace, Transform origin) GetPlayerSpaceAndOrigin()
        {
            VRCenterAdjust center = transform.GetComponentInParent<VRCenterAdjust>();

            if (center != null)
            {
                Transform centerTransform = center.transform;
                return (centerTransform, centerTransform.parent);
            }
            else
            {
                return (transform.parent, transform.parent);
            }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _trackedPoseDriver = GetComponent<TrackedPoseDriver>();
        }

        private void OnEnable()
        {
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed += OnCameraNearClipPlaneChanged;
            }

            if (_fpfcSettings != null)
            {
                _fpfcSettings.Changed += OnFpfcSettingsChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged += OnFocusChanged;
                OnFocusChanged(_beatSaberUtilities.hasFocus);
            }

            UpdateCameraMask();
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(
            ILogger<MainCamera> logger,
            Settings settings,
            ActivePlayerSpaceManager activePlayerSpaceManager,
            ActiveOriginManager activeOriginManager,
            ActiveCameraManager activeCameraManager,
            IFPFCSettings fpfcSettings,
            BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            _settings = settings;
            _activePlayerSpaceManager = activePlayerSpaceManager;
            _activeOriginManager = activeOriginManager;
            _activeCameraManager = activeCameraManager;
            _fpfcSettings = fpfcSettings;
            _beatSaberUtilities = beatSaberUtilities;
        }

        private void Start()
        {
            // prevent errors if this is instantiated via Object.Instantiate
            if (_logger == null)
            {
                Destroy(this);
                return;
            }

            OnEnable();
            AddToPlayerSpaceManager();
        }

        private void OnDisable()
        {
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
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

        private void OnDestroy()
        {
            RemoveFromPlayerSpaceManager();
        }

        private void OnCameraNearClipPlaneChanged(float value)
        {
            UpdateCameraMask();
        }

        private void OnFpfcSettingsChanged(IFPFCSettings fpfcSettings)
        {
            UpdateCameraMask();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            _trackedPoseDriver.UseRelativeTransform = !hasFocus;
            _trackedPoseDriver.originPose = hasFocus ? Pose.identity : new Pose(
                Vector3.ProjectOnPlane(Quaternion.Euler(0, 180, 0) * -transform.localPosition * 2, Vector3.up) + Vector3.ProjectOnPlane(transform.localRotation * Vector3.forward, Vector3.up).normalized * 1.5f,
                Quaternion.Euler(0, 180, 0));

            UpdateCameraMask();
        }

        private void UpdateCameraMask()
        {
            if (_logger == null || _settings == null || _fpfcSettings == null)
            {
                return;
            }

            _logger.LogTrace($"Setting avatar culling mask and near clip plane on '{_camera.name}'");

            int mask = _camera.cullingMask | AvatarLayers.kAlwaysVisibleMask;

            // FPFC basically ends up being a 3rd person camera
            if (_fpfcSettings.Enabled || !_beatSaberUtilities.hasFocus)
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
            (_playerSpace, _origin) = GetPlayerSpaceAndOrigin();
            _activePlayerSpaceManager?.Add(_playerSpace);
            _activeOriginManager?.Add(_origin);
            _activeCameraManager?.Add(_camera);
        }

        private void RemoveFromPlayerSpaceManager()
        {
            _activePlayerSpaceManager?.Remove(_playerSpace);
            _activeOriginManager?.Remove(_origin);
            _activeCameraManager?.Remove(_camera);
        }
    }
}
