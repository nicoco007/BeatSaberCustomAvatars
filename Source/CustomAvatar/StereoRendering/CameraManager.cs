using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    class CameraManager : MonoBehaviour
    {
        private ILogger<CameraManager> _logger;
        private DiContainer _container;
        private Settings _settings;
        private GameScenesManager _gameScenesManager;

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, DiContainer container, Settings settings, GameScenesManager gameScenesManager)
        {
            _logger = loggerProvider.CreateLogger<CameraManager>();
            _container = container;
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
            UpdateCameras();
        }

        private void UpdateCameras()
        {
            foreach (Camera camera in Camera.allCameras)
            {
                var detector = camera.gameObject.GetComponent<VRRenderEventDetector>();

                if (detector == null)
                {
                    _logger.Info($"Adding {nameof(VRRenderEventDetector)} to '{camera.name}'");
                    _container.InstantiateComponent<VRRenderEventDetector>(camera.gameObject);
                }

                if (camera.GetComponent<MainCamera>())
                {
                    _logger.Info($"Setting up avatar culling mask on '{camera.name}'");

                    int cullingMask = camera.cullingMask;

                    cullingMask &= ~(1 << AvatarLayers.kOnlyInThirdPerson);
                    cullingMask |= (1 << AvatarLayers.kAlwaysVisible);
                    cullingMask |= (1 << AvatarLayers.kOnlyInFirstPerson);

                    camera.cullingMask = cullingMask;

                    camera.nearClipPlane = _settings.cameraNearClipPlane;
                }
            }
        }
    }
}
