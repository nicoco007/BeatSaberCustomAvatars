using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    class CameraManager : MonoBehaviour
    {
        private ILogger<CameraManager> _logger;
        private Settings _settings;
        private GameScenesManager _gameScenesManager;

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, Settings settings, GameScenesManager gameScenesManager)
        {
            _logger = loggerProvider.CreateLogger<CameraManager>();
            _settings = settings;
            _gameScenesManager = gameScenesManager;

            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        private void OnDestroy()
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
