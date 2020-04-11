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

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    internal class Plugin
    {
        private AvatarManager _avatarManager;
        private GameScenesManager _scenesManager;
        private AvatarListFlowCoordinator _flowCoordinator;

        private SceneContext _sceneContext;
        private GameObject _mirrorContainer;
        
        public event Action<ScenesTransitionSetupDataSO, DiContainer> sceneTransitionDidFinish;

        public static Plugin instance { get; private set; }

        public static Logger logger { get; private set; }

        [Init]
        public Plugin(Logger logger)
        {
            instance = this;
            Plugin.logger = logger;
            
            SettingsManager.Load();
            BeatSaberEvents.ApplyPatches();
        }

        [OnStart]
        public void OnStart()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;

            KeyboardInputHandler keyboardInputHandler = new GameObject(nameof(KeyboardInputHandler)).AddComponent<KeyboardInputHandler>();
            Object.DontDestroyOnLoad(keyboardInputHandler.gameObject);

            ShaderLoader shaderLoader = new GameObject(nameof(ShaderLoader)).AddComponent<ShaderLoader>();
            Object.DontDestroyOnLoad(shaderLoader.gameObject);
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

            SettingsManager.Save();
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            logger.Info("OnActiveSceneChanged: " + newScene.name);

            if (newScene.name == "PCInit")
            {
                _sceneContext = Object.FindObjectOfType<SceneContext>();

                _sceneContext.Container.Install<CustomAvatarsInstaller>();
                _sceneContext.Container.Install<UIInstaller>();
            }
        }

        public void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (newScene.name == "PCInit")
            {
                _scenesManager = _sceneContext.Container.Resolve<GameScenesManager>();
                _avatarManager = _sceneContext.Container.Resolve<AvatarManager>();

                _scenesManager.transitionDidFinishEvent += sceneTransitionDidFinish;
                _scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;

                _avatarManager.LoadAvatarFromSettingsAsync();
            }

            if (newScene.name == "MenuCore")
            {
                _flowCoordinator = _sceneContext.Container.Resolve<AvatarListFlowCoordinator>();

                try
                {
                    MenuButtons.instance.RegisterButton(new MenuButton("Avatars", () =>
                    {
                        Plugin.logger.Info("flowCoordinator: " + _flowCoordinator);
                        BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator, null, true);
                    }));
                }
                catch (Exception)
                {
                    logger.Warn("Failed to add menu button, spawning mirror instead");

                    _mirrorContainer = new GameObject();
                    Object.DontDestroyOnLoad(_mirrorContainer);
                    Vector2 mirrorSize = SettingsManager.settings.mirror.size;
                    MirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, -1.5f), Quaternion.Euler(-90f, 180f, 0), mirrorSize, _mirrorContainer.transform);
                }
            }
        }

        private void SceneTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.gameObject.GetComponent<VRRenderEventDetector>() == null)
                {
                    camera.gameObject.AddComponent<VRRenderEventDetector>();
                    logger.Info($"Added {nameof(VRRenderEventDetector)} to {camera}");
                }
            }
            
            Camera mainCamera = Camera.main;

            if (mainCamera)
            {
                SetCameraCullingMask(mainCamera);
                mainCamera.nearClipPlane = SettingsManager.settings.cameraNearClipPlane;
            }
            else
            {
                logger.Error("Could not find main camera!");
            }
        }

        private void SetCameraCullingMask(Camera camera)
        {
            logger.Debug("Adding third person culling mask to " + camera.name);

            camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
        }
    }
}
