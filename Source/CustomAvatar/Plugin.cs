using CustomAvatar.StereoRendering;
using IPA;
using System;
using System.Linq;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.UI;
using CustomAvatar.Utilities;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    internal class Plugin
    {
        private GameScenesManager _scenesManager;
        private GameObject _mirrorContainer;
        
        public event Action<ScenesTransitionSetupDataSO, DiContainer> sceneTransitionDidFinish;

        public static Plugin instance { get; private set; }

        public static Logger logger { get; private set; }
        public static Settings settings { get; set; }

        [Init]
        public Plugin(Logger logger, [Config.Name("CustomAvatars")] Config config)
        {
            Plugin.logger = logger;
            Plugin.settings = config.Generated<Settings>();

            instance = this;
        }

        [OnStart]
        public void OnStart()
        {
            AvatarManager.instance.LoadAvatarFromSettingsAsync();
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
        }

        public void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (_scenesManager == null)
            {
                _scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

                if (_scenesManager != null)
                {
                    _scenesManager.transitionDidFinishEvent += sceneTransitionDidFinish;
                    _scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
                }
            }

            if (newScene.name == "MenuCore")
            {
                try
                {
                    MenuButtons.instance.RegisterButton(new MenuButton("Avatars", () =>
                    {
                        var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
                        var flowCoordinator = new GameObject(nameof(AvatarListFlowCoordinator)).AddComponent<AvatarListFlowCoordinator>();
                        mainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", flowCoordinator, null, true, false);
                    }));
                }
                catch (Exception)
                {
                    logger.Warn("Failed to add menu button, spawning mirror instead");

                    _mirrorContainer = new GameObject();
                    Object.DontDestroyOnLoad(_mirrorContainer);
                    Vector2 mirrorSize = settings.mirrorSize;
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
                mainCamera.nearClipPlane = Plugin.settings.cameraNearClipPlane;
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
