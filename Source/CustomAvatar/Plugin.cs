using CustomAvatar.StereoRendering;
using IPA;
using System;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.UI;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;
using BeatSaberMarkupLanguage;
using CustomAvatar.Logging;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    internal class Plugin
    {
        [Inject] private AvatarManager _avatarManager;
        [Inject] private GameScenesManager _scenesManager;
        [Inject] private AvatarListFlowCoordinator _flowCoordinator;
        [Inject] private Settings _settings;
        [Inject] private SettingsManager _settingsManager;
        [Inject] private MirrorHelper _mirrorHelper;

        private SceneContext _sceneContext;
        private GameObject _mirrorContainer;
        
        public event Action<ScenesTransitionSetupDataSO, DiContainer> sceneTransitionDidFinish;

        public static Plugin instance { get; private set; }

        private ILogger _logger;
        private Logger _ipaLogger;

        [Init]
        public Plugin(Logger logger)
        {
            instance = this;
            _ipaLogger = logger;

            // can't inject at this point so just create it
            _logger = new IPALogger<Plugin>(logger);
            
            BeatSaberEvents.ApplyPatches();
        }

        [OnStart]
        public void OnStart()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [OnExit]
        public void OnExit()
        {
            if (_scenesManager != null)
            {
                _scenesManager.transitionDidFinishEvent -= sceneTransitionDidFinish;
                _scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;

            _settingsManager.Save(_settings);
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            if (newScene.name == "PCInit")
            {
                // Beat Saber has one instance of SceneContext in the PCInit scene
                _sceneContext = Object.FindObjectOfType<SceneContext>();

                // OnActiveSceneChanged runs before OnSceneLoaded for PCInit so bind new stuff here
                _sceneContext.Container.Install<CustomAvatarsInstaller>(new object[] { _ipaLogger });
                _sceneContext.Container.Install<UIInstaller>();
            }
        }

        public void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (newScene.name == "PCInit")
            {
                // OnSceneLoaded runs after OnActiveSceneChanged for PCInit so inject here
                _sceneContext.Container.Inject(this);

                _scenesManager.transitionDidFinishEvent += sceneTransitionDidFinish;
                _scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;

                _avatarManager.LoadAvatarFromSettingsAsync();

                KeyboardInputHandler keyboardInputHandler = _sceneContext.Container.InstantiateComponentOnNewGameObject<KeyboardInputHandler>();
                Object.DontDestroyOnLoad(keyboardInputHandler.gameObject);
            }

            if (newScene.name == "MenuCore")
            {
                try
                {
                    MenuButtons.instance.RegisterButton(new MenuButton("Avatars", () =>
                    {
                        BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator, null, true);
                    }));
                }
                catch (Exception)
                {
                    _logger.Warning("Failed to add menu button, spawning mirror instead");

                    _mirrorContainer = new GameObject();
                    Object.DontDestroyOnLoad(_mirrorContainer);
                    Vector2 mirrorSize = _settings.mirror.size;
                    _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, -1.5f), Quaternion.Euler(-90f, 180f, 0), mirrorSize, _mirrorContainer.transform);
                }
            }
        }

        private void SceneTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            foreach (Camera camera in Camera.allCameras)
            {
                var detector = camera.gameObject.GetComponent<VRRenderEventDetector>();

                if (detector == null)
                {
                    _sceneContext.Container.InstantiateComponent<VRRenderEventDetector>(camera.gameObject);
                    _logger.Info($"Added {nameof(VRRenderEventDetector)} to {camera}");
                }
            }
            
            Camera mainCamera = Camera.main;

            if (mainCamera)
            {
                SetCameraCullingMask(mainCamera);
                mainCamera.nearClipPlane = _settings.cameraNearClipPlane;
            }
            else
            {
                _logger.Error("Could not find main camera!");
            }
        }

        private void SetCameraCullingMask(Camera camera)
        {
            _logger.Debug("Adding third person culling mask to " + camera.name);

            camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
        }
    }
}
