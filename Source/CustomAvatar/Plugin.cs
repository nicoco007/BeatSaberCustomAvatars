using CustomAvatar.StereoRendering;
using IPA;
using System;
using System.Linq;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.UI;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Input = UnityEngine.Input;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    internal class Plugin : IBeatSaberPlugin
    {
        private GameScenesManager _scenesManager;
        private GameObject _mirrorContainer;
        
        public event Action<ScenesTransitionSetupDataSO, DiContainer> sceneTransitionDidFinish;

        public static Plugin instance { get; private set; }

        public static Logger logger { get; private set; }

        public void Init(Logger logger)
        {
            Plugin.logger = logger;
            instance = this;
        }

        public void OnApplicationStart()
        {
            SettingsManager.LoadSettings();
            AvatarManager.instance.LoadAvatarFromSettingsAsync();
        }

        public void OnApplicationQuit()
        {
            if (_scenesManager != null)
            {
                _scenesManager.transitionDidFinishEvent -= sceneTransitionDidFinish;
                _scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
            }

            SettingsManager.SaveSettings();
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
                        var flowCoordinator = new GameObject("AvatarListFlowCoordinator")
                            .AddComponent<AvatarListFlowCoordinator>();
                        mainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", flowCoordinator, null, true,
                            false);
                    }));
                }
                catch (Exception)
                {
                    logger.Warn("Failed to add menu button, spawning mirror instead");

                    _mirrorContainer = new GameObject();
                    GameObject.DontDestroyOnLoad(_mirrorContainer);
                    SharedCoroutineStarter.instance.StartCoroutine(MirrorHelper.SpawnMirror(new Vector3(0, 0, -1.5f), Quaternion.Euler(-90f, 180f, 0), new Vector3(0.50f, 1f, 0.25f), _mirrorContainer.transform));
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

        public void OnUpdate()
        {
            AvatarManager avatarManager = AvatarManager.instance;

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                avatarManager.SwitchToNextAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                avatarManager.SwitchToPreviousAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Home))
            {
                SettingsManager.settings.isAvatarVisibleInFirstPerson = !SettingsManager.settings.isAvatarVisibleInFirstPerson;
                logger.Info($"{(SettingsManager.settings.isAvatarVisibleInFirstPerson ? "Enabled" : "Disabled")} first person visibility");
                avatarManager.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
            }
            else if (Input.GetKeyDown(KeyCode.End))
            {
                SettingsManager.settings.resizeMode = (AvatarResizeMode) (((int)SettingsManager.settings.resizeMode + 1) % 3);
                logger.Info($"Set resize mode to {SettingsManager.settings.resizeMode}");
                avatarManager.ResizeCurrentAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Insert))
            {
                SettingsManager.settings.enableFloorAdjust = !SettingsManager.settings.enableFloorAdjust;
                logger.Info($"{(SettingsManager.settings.enableFloorAdjust ? "Enabled" : "Disabled")} floor adjust");
                avatarManager.ResizeCurrentAvatar();
            }
        }

        private void SetCameraCullingMask(Camera camera)
        {
            logger.Debug("Adding third person culling mask to " + camera.name);

            camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
        }

        public void OnFixedUpdate() { }

        public void OnSceneUnloaded(Scene scene) { }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene) { }
    }
}
