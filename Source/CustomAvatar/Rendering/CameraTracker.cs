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
using JetBrains.Annotations;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal abstract class CameraTracker : MonoBehaviour
    {
        private ILogger<CameraTracker> _logger;
        private Settings _settings;
        private ActiveCameraManager _activeCameraManager;
        private TrackedPoseDriver _trackedPoseDriver;

        internal Camera camera { get; private set; }

        internal Transform playerSpace { get; private protected set; }

        internal Transform origin { get; private protected set; }

        protected IFPFCSettings fpfcSettings { get; private set; }

        protected BeatSaberUtilities beatSaberUtilities { get; private set; }

        internal void UpdateCameraMask()
        {
            if (_logger == null || _settings == null || fpfcSettings == null)
            {
                return;
            }

            _logger.LogTrace($"Setting avatar culling mask and near clip plane on '{camera.name}'");

            camera.cullingMask = GetCameraMask(camera.cullingMask);
            camera.nearClipPlane = _settings.cameraNearClipPlane;
        }

        protected virtual int GetCameraMask(int mask)
        {
            mask |= AvatarLayers.kAlwaysVisibleMask | AvatarLayers.kMirrorMask;

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

        protected virtual void Awake()
        {
            camera = GetComponent<Camera>();
            _trackedPoseDriver = GetComponent<TrackedPoseDriver>();
        }

        protected virtual void OnEnable()
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
            }

            _activeCameraManager?.Add(this);

            UpdateCameraMask();
        }

        protected virtual void OnPreCull()
        {
            if (_settings.hmdCameraBehaviour == HmdCameraBehaviour.HmdOnly && !beatSaberUtilities.hasFocus)
            {
                _trackedPoseDriver.UseRelativeTransform = true;
                _trackedPoseDriver.PerformUpdate();

                if (camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
                {
                    camera.transform.position = camera.ViewportToWorldPoint(Vector3.zero, camera.stereoActiveEye);
                }

                camera.ResetWorldToCameraMatrix();
                camera.cullingMask |= AvatarLayers.kOnlyInThirdPersonMask;
            }
        }

        protected virtual void OnPostRender()
        {
            if (_settings.hmdCameraBehaviour == HmdCameraBehaviour.HmdOnly && !beatSaberUtilities.hasFocus)
            {
                // the VR camera seems to always be rendered last so we don't need to re-update the camera pose/matrix
                _trackedPoseDriver.UseRelativeTransform = false;
                UpdateCameraMask();
            }
        }

        protected virtual void Start()
        {
            // prevent errors if this is instantiated via Object.Instantiate
            if (_logger == null)
            {
                Destroy(this);
                return;
            }

            OnEnable();
        }

        protected virtual void OnDisable()
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

            _activeCameraManager?.Remove(this);
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(
            ILogger<CameraTracker> logger,
            Settings settings,
            ActiveCameraManager activeCameraManager,
            IFPFCSettings fpfcSettings,
            BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            _settings = settings;
            _activeCameraManager = activeCameraManager;
            this.fpfcSettings = fpfcSettings;
            this.beatSaberUtilities = beatSaberUtilities;
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

        private void OnCameraNearClipPlaneChanged(float value)
        {
            UpdateCameraMask();
        }

        private void OnFpfcSettingsChanged(IFPFCSettings fpfcSettings)
        {
            UpdateCameraMask();
        }

        public override string ToString()
        {
            return $"{{ {nameof(camera)} = {UnityUtilities.GetTransformPath(camera)}, {nameof(playerSpace)} = {UnityUtilities.GetTransformPath(playerSpace)}, {nameof(origin)} = {UnityUtilities.GetTransformPath(origin)} }}";
        }
    }
}
