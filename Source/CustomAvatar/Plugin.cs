using CustomAvatar.StereoRendering;
using IPA;
using System;
using System.Reflection;
using BeatSaberMarkupLanguage.MenuButtons;
using CustomAvatar.Lighting;
using CustomAvatar.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;
using BeatSaberMarkupLanguage;
using CustomAvatar.Logging;
using HarmonyLib;
using CustomAvatar.Configuration;
using CustomAvatar.Utilities;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    internal class Plugin
    {
        private PlayerAvatarManager _avatarManager;
        private GameScenesHelper _gameScenesHelper;
        private AvatarMenuFlowCoordinator _flowCoordinator;
        private Settings _settings;
        private MirrorHelper _mirrorHelper;

        private DiContainer _container;
        private GameObject _mirrorContainer;
        private MenuLightingController _menuLightingRig;
        private KeyboardInputHandler _keyboardInputHandler;
        private GameplayLightingController _gameplayLightingController;

        private ILogger<Plugin> _logger;

        [Init]
        public Plugin(Logger logger)
        {
            // can't inject at this point so just create it
            _logger = new IPALogger<Plugin>(logger);

            _logger.Info("Initializing Custom Avatars");

            try
            {
                Harmony harmony = new Harmony("com.nicoco007.beatsabercustomavatars");

                ZenjectHelper.Init(harmony, logger);
                BeatSaberEvents.ApplyPatches(harmony);
                PatchMirrorRendererSO(harmony);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to apply patches");
                _logger.Error(ex);
            }
        }

        [Inject]
        private void Inject(DiContainer container, PlayerAvatarManager playerAvatarManager, GameScenesHelper gameScenesHelper, AvatarMenuFlowCoordinator avatarMenuFlowCoordinator, Settings settings, MirrorHelper mirrorHelper)
        {
            _container = container;
            _avatarManager = playerAvatarManager;
            _gameScenesHelper = gameScenesHelper;
            _flowCoordinator = avatarMenuFlowCoordinator;
            _settings = settings;
            _mirrorHelper = mirrorHelper;
        }

        // TODO put this somewhere else
        private void PatchMirrorRendererSO(Harmony harmony)
        {
            var methodToPatch = typeof(MirrorRendererSO).GetMethod("CreateOrUpdateMirrorCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            var patch = new HarmonyMethod(typeof(Plugin).GetMethod(nameof(MirrorRendererSOPatch), BindingFlags.NonPublic | BindingFlags.Static));

            harmony.Patch(methodToPatch, null, patch);
        }

        private static void MirrorRendererSOPatch(MirrorRendererSO __instance)
        {
            Camera mirrorCamera = new Traverse(__instance).Field<Camera>("_mirrorCamera").Value;

            mirrorCamera.cullingMask |= (1 << AvatarLayers.kOnlyInThirdPerson) | (1 << AvatarLayers.kOnlyInFirstPerson) | (1 << AvatarLayers.kAlwaysVisible);
        }

        [OnStart]
        public void OnStart()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        [OnExit]
        public void OnExit()
        {
            if (_gameScenesHelper != null)
            {
                _gameScenesHelper.transitionDidFinish -= OnTransitionDidFinish;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (newScene.name == "PCInit")
            {
                ZenjectHelper.GetMainSceneContextAsync(OnSceneContextPostInstall);
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

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "PCInit")
            {
                Object.Destroy(_keyboardInputHandler);
                Object.Destroy(_menuLightingRig);
                Object.Destroy(_mirrorContainer);
                Object.Destroy(_avatarManager.currentlySpawnedAvatar);
            }
        }

        private void OnSceneContextPostInstall(SceneContext context)
        {
            context.Container.Inject(this);

            _gameScenesHelper.transitionDidFinish += OnTransitionDidFinish;

            _avatarManager.LoadAvatarFromSettingsAsync();

            _keyboardInputHandler = _container.InstantiateComponentOnNewGameObject<KeyboardInputHandler>(nameof(KeyboardInputHandler));

            if (_settings.lighting.castShadows)
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = _settings.lighting.shadowResolution;
                QualitySettings.shadowDistance = 25;
            }

            if (_settings.calibrateFullBodyTrackingOnStart && _settings.GetAvatarSettings(_settings.previousAvatarPath).useAutomaticCalibration)
            {
                context.Container.Resolve<AvatarTailor>().CalibrateFullBodyTrackingAuto();
            }
        }

        private void OnTransitionDidFinish(BeatSaberScene scene, DiContainer container)
        {
            UpdateCameras();
            UpdateLighting(scene, container);
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

        private void UpdateLighting(BeatSaberScene scene, DiContainer container)
        {
            if (_settings.lighting.enabled)
            {
                if (scene == BeatSaberScene.Game && _settings.lighting.enableDynamicLighting)
                {
                    Object.Destroy(_menuLightingRig);

                    if (!_gameplayLightingController)
                    {
                        _gameplayLightingController = container.InstantiateComponentOnNewGameObject<GameplayLightingController>();
                    }
                }
                else
                {
                    Object.Destroy(_gameplayLightingController);

                    if (!_menuLightingRig)
                    {
                        _menuLightingRig = _container.InstantiateComponentOnNewGameObject<MenuLightingController>(nameof(MenuLightingController));
                        Object.DontDestroyOnLoad(_menuLightingRig);
                    }
                }
            }
        }
    }
}
