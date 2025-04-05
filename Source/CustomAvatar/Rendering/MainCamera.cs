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

using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Rendering
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    internal class MainCamera : MonoBehaviour
    {
        private ILogger<MainCamera> _logger;
        private Settings _settings;
        private ActivePlayerSpaceManager _activePlayerSpaceManager;
        private ActiveOriginManager _activeOriginManager;
        private ActiveCameraManager _activeCameraManager;

        private Transform _playerSpace;
        private Transform _origin;
        private Camera _camera;
        private TrackedPoseDriver _trackedPoseDriver;

        protected IFPFCSettings fpfcSettings { get; private set; }

        protected BeatSaberUtilities beatSaberUtilities { get; private set; }

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

        protected virtual int GetCameraMask(int mask)
        {
            mask |= AvatarLayers.kAlwaysVisibleMask;

            // FPFC basically ends up being a 3rd person camera
            if (fpfcSettings.Enabled || (!beatSaberUtilities.hasFocus && _settings.hmdCameraBehaviour == HmdCameraBehaviour.AllCameras))
            {
                mask |= AvatarLayers.kOnlyInThirdPersonMask;
            }
            else
            {
                mask &= ~AvatarLayers.kOnlyInThirdPersonMask;
            }

            return mask;
        }

        protected void Awake()
        {
            _camera = GetComponent<Camera>();
            _trackedPoseDriver = GetComponent<TrackedPoseDriver>();
        }

        protected void OnEnable()
        {
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
                _settings.cameraNearClipPlane.changed += OnCameraNearClipPlaneChanged;
            }

            if (fpfcSettings != null)
            {
                fpfcSettings.Changed -= OnFpfcSettingsChanged;
                fpfcSettings.Changed += OnFpfcSettingsChanged;
            }

            if (beatSaberUtilities != null)
            {
                beatSaberUtilities.focusChanged -= OnFocusChanged;
                beatSaberUtilities.focusChanged += OnFocusChanged;
                OnFocusChanged(beatSaberUtilities.hasFocus);
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
            this.fpfcSettings = fpfcSettings;
            this.beatSaberUtilities = beatSaberUtilities;
        }

        protected void Start()
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

        protected void OnDisable()
        {
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
            }

            if (fpfcSettings != null)
            {
                fpfcSettings.Changed -= OnFpfcSettingsChanged;
            }

            if (beatSaberUtilities != null)
            {
                beatSaberUtilities.focusChanged -= OnFocusChanged;
            }
        }

        protected void OnDestroy()
        {
            RemoveFromPlayerSpaceManager();
        }

        protected void OnPreCull()
        {
            if (_settings.hmdCameraBehaviour == HmdCameraBehaviour.HmdOnly && !beatSaberUtilities.hasFocus)
            {
                _trackedPoseDriver.UseRelativeTransform = true;
                _trackedPoseDriver.PerformUpdate();

                if (_camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
                {
                    _camera.transform.position = _camera.ViewportToWorldPoint(Vector3.zero, _camera.stereoActiveEye);
                }

                _camera.ResetWorldToCameraMatrix();
                _camera.cullingMask |= AvatarLayers.kOnlyInThirdPersonMask;
            }
        }

        protected void OnPostRender()
        {
            if (_settings.hmdCameraBehaviour == HmdCameraBehaviour.HmdOnly && !beatSaberUtilities.hasFocus)
            {
                // the VR camera seems to always be rendered last so we don't need to re-update the camera pose/matrix
                _trackedPoseDriver.UseRelativeTransform = false;
                UpdateCameraMask();
            }
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
            Quaternion rotation = Quaternion.Euler(0, 180, 0);

            _trackedPoseDriver.originPose = hasFocus ? Pose.identity : new Pose(
                Vector3.ProjectOnPlane(rotation * -transform.localPosition * 2, Vector3.up) + Vector3.ProjectOnPlane(transform.localRotation * Vector3.forward, Vector3.up).normalized,
                rotation);
            _trackedPoseDriver.UseRelativeTransform = _settings.hmdCameraBehaviour == HmdCameraBehaviour.AllCameras;

            UpdateCameraMask();
        }

        private void UpdateCameraMask()
        {
            if (_logger == null || _settings == null || fpfcSettings == null)
            {
                return;
            }

            _logger.LogTrace($"Setting avatar culling mask and near clip plane on '{_camera.name}'");

            _camera.cullingMask = GetCameraMask(_camera.cullingMask);
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
