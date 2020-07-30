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
