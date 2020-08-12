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

using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    internal class MainCameraController : IInitializable, IDisposable
    {
        private ILogger<MainCameraController> _logger;
        private Settings _settings;
        private GameScenesManager _gameScenesManager;

        public MainCameraController(ILoggerProvider loggerProvider, Settings settings, GameScenesManager gameScenesManager)
        {
            _logger = loggerProvider.CreateLogger<MainCameraController>();
            _settings = settings;
            _gameScenesManager = gameScenesManager;
        }

        public void Initialize()
        {
            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        public void Dispose()
        {
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;
        }

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            UpdateMainCamera();
        }

        private void UpdateMainCamera()
        {
            Camera mainCamera = Camera.main;

            if (!mainCamera)
            {
                _logger.Warning("Main camera not found");
            }

            _logger.Info($"Setting avatar culling mask and near clip plane on '{mainCamera.name}'");

            mainCamera.cullingMask = (mainCamera.cullingMask & ~AvatarLayers.kOnlyInThirdPersonMask) | AvatarLayers.kAlwaysVisibleMask;
            mainCamera.nearClipPlane = _settings.cameraNearClipPlane;
        }
    }
}
