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

using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Rendering
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    internal class CameraFlipper : MonoBehaviour
    {
        private ILogger<CameraFlipper> _logger;
        private Settings _settings;
        private BeatSaberUtilities _beatSaberUtilities;

        private Camera _camera;
        private TrackedPoseDriver _trackedPoseDriver;
        private MainCamera _mainCamera;

        protected void Awake()
        {
            _camera = GetComponent<Camera>();
            _trackedPoseDriver = GetComponent<TrackedPoseDriver>();
            _mainCamera = GetComponent<MainCamera>();
        }

        protected void OnEnable()
        {
            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged -= OnFocusChanged;
                _beatSaberUtilities.focusChanged += OnFocusChanged;
                OnFocusChanged(_beatSaberUtilities.hasFocus);
            }
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(
            ILogger<CameraFlipper> logger,
            Settings settings,
            BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
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
        }

        protected void OnDisable()
        {
            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged -= OnFocusChanged;
            }
        }

        protected void OnPreCull()
        {
            if (_settings.hmdCameraBehaviour == HmdCameraBehaviour.HmdOnly && !_beatSaberUtilities.hasFocus)
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
            if (_settings.hmdCameraBehaviour == HmdCameraBehaviour.HmdOnly && !_beatSaberUtilities.hasFocus)
            {
                // the VR camera seems to always be rendered last so we don't need to re-update the camera pose/matrix
                _trackedPoseDriver.UseRelativeTransform = false;
                _mainCamera.UpdateCameraMask();
            }
        }

        private void OnFocusChanged(bool hasFocus)
        {
            Quaternion rotation = Quaternion.Euler(0, 180, 0);

            _trackedPoseDriver.originPose = hasFocus ? Pose.identity : new Pose(
                Vector3.ProjectOnPlane(rotation * -transform.localPosition * 2, Vector3.up) + Vector3.ProjectOnPlane(transform.localRotation * Vector3.forward, Vector3.up).normalized,
                rotation);
            _trackedPoseDriver.UseRelativeTransform = _settings.hmdCameraBehaviour == HmdCameraBehaviour.AllCameras;

            _mainCamera.UpdateCameraMask();
        }
    }
}
