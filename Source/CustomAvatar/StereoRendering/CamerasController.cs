//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    internal class CamerasController : IInitializable, IDisposable
    {
        private readonly ILogger<CamerasController> _logger;
        private readonly Settings _settings;
        private readonly GameScenesManager _gameScenesManager;
        private readonly MainSettingsModelSO _mainSettingsModel;

        public CamerasController(ILoggerProvider loggerProvider, Settings settings, GameScenesManager gameScenesManager, MainSettingsModelSO mainSettingsModel)
        {
            _logger = loggerProvider.CreateLogger<CamerasController>();
            _settings = settings;
            _gameScenesManager = gameScenesManager;
            _mainSettingsModel = mainSettingsModel;
        }

        public void Initialize()
        {
            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
            _mainSettingsModel.smoothCameraThirdPersonEnabled.didChangeEvent += OnSmoothCameraThirdPersonEnabled;
        }

        public void Dispose()
        {
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;
            _mainSettingsModel.smoothCameraThirdPersonEnabled.didChangeEvent -= OnSmoothCameraThirdPersonEnabled;
        }

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            UpdateMainCamera(container);
            UpdateSmoothCamera(container);
        }

        private void UpdateMainCamera(DiContainer container)
        {
            MainCamera mainCamera = container.TryResolve<MainCamera>();

            if (!mainCamera)
            {
                _logger.Warning("Main camera not found!");
                return;
            }

            Camera camera = mainCamera.camera;

            _logger.Info($"Setting avatar culling mask and near clip plane on '{camera.name}'");

            camera.cullingMask = (camera.cullingMask & ~AvatarLayers.kOnlyInThirdPersonMask) | AvatarLayers.kAlwaysVisibleMask;
            camera.nearClipPlane = _settings.cameraNearClipPlane;
        }

        private void OnSmoothCameraThirdPersonEnabled()
        {
            UpdateSmoothCamera(_gameScenesManager.currentScenesContainer);
        }

        private void UpdateSmoothCamera(DiContainer container)
        {
            if (!_mainSettingsModel.smoothCameraEnabled) return;

            SmoothCamera smoothCamera = container.TryResolve<SmoothCamera>();

            if (!smoothCamera)
            {
                _logger.Warning("Smooth camera not found!");
                return;
            }

            Camera camera = smoothCamera.GetPrivateField<Camera>("_camera");
            bool thirdPersonEnabled = smoothCamera.GetPrivateField<bool>("_thirdPersonEnabled");

            _logger.Info($"Setting avatar culling mask and near clip plane on '{camera.name}'");

            if (thirdPersonEnabled)
            {
                camera.cullingMask = camera.cullingMask | AvatarLayers.kOnlyInThirdPersonMask | AvatarLayers.kAlwaysVisibleMask;
                camera.nearClipPlane = 0.1f;
            }
            else
            {
                camera.cullingMask = (camera.cullingMask & ~AvatarLayers.kOnlyInThirdPersonMask) | AvatarLayers.kAlwaysVisibleMask;
                camera.nearClipPlane = _settings.cameraNearClipPlane;
            }
        }
    }
}
