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
using SiraUtil.Tools.FPFC;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    internal class MainCamera : MonoBehaviour
    {
        private ILogger<MainCamera> _logger;
        private Settings _settings;
        private ActiveCameraManager _activeCameraManager;

        internal Camera camera { get; private set; }

        internal Transform playerSpace { get; private protected set; }

        internal Transform origin { get; private protected set; }

        internal virtual bool showAvatar => true;

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

        protected void Awake()
        {
            camera = GetComponent<Camera>();

            VRCenterAdjust center = transform.GetComponentInParent<VRCenterAdjust>();

            if (center != null)
            {
                Transform centerTransform = center.transform;
                playerSpace = centerTransform;
                origin = centerTransform.parent;
            }
            else
            {
                playerSpace = origin = transform.parent;
            }
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

            _activeCameraManager?.Add(this);

            UpdateCameraMask();
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(
            ILogger<MainCamera> logger,
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
            if (_settings != null)
            {
                _settings.cameraNearClipPlane.changed -= OnCameraNearClipPlaneChanged;
            }

            if (fpfcSettings != null)
            {
                fpfcSettings.Changed -= OnFpfcSettingsChanged;
            }

            _activeCameraManager?.Remove(this);
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
