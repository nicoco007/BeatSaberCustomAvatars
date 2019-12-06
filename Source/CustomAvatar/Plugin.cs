using CustomAvatar.StereoRendering;
using IPA;
using System;
using System.Linq;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.UI;
using CustomAvatar.Utilities;
using DynamicOpenVR;
using DynamicOpenVR.IO;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Input = UnityEngine.Input;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    public class Plugin : IBeatSaberPlugin
    {
        private GameScenesManager _scenesManager;

        public event Action<Scene> sceneTransitioned;

        public static Plugin instance { get; private set; }

        public static Logger logger { get; private set; }

        public static SkeletalInput leftHandAnimAction;
        public static SkeletalInput rightHandAnimAction;

        public Plugin()
        {
            if (OpenVRActionManager.isRunning)
            {
                OpenVRActionManager actionManager = OpenVRActionManager.instance;

                leftHandAnimAction = actionManager.RegisterAction(new SkeletalInput("/actions/customavatars/in/lefthandanim"));
                rightHandAnimAction = actionManager.RegisterAction(new SkeletalInput("/actions/customavatars/in/righthandanim"));
            }
        }

        public void Init(Logger logger)
        {
            Plugin.logger = logger;
            instance = this;
            
            SettingsManager.LoadSettings();
            AvatarManager.instance.LoadAvatarFromSettingsAsync();
        }

        public void OnApplicationQuit()
        {
            if (_scenesManager != null)
                _scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;

            SettingsManager.SaveSettings();
        }

        public void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (_scenesManager == null)
            {
                _scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

                if (_scenesManager != null)
                {
                    _scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
                    _scenesManager.transitionDidFinishEvent += (setupData, container) => sceneTransitioned?.Invoke(SceneManager.GetActiveScene());
                }
            }

            if (newScene.name == "HealthWarning" && SettingsManager.settings.calibrateFullBodyTrackingOnStart)
            {
                AvatarManager.instance.avatarTailor.CalibrateFullBodyTracking();
            }

            if (newScene.name == "MenuCore")
            {
                MenuButtons.instance.RegisterButton(new MenuButton("Avatars", () =>
                {
                    var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
                    var flowCoordinator = new GameObject("AvatarListFlowCoordinator").AddComponent<AvatarListFlowCoordinator>();
                    mainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", flowCoordinator, null, true, false);
                }));
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

        public void OnApplicationStart() { }
    }
}
