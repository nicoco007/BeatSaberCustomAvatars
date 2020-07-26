using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomAvatar.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    public class ZenjectHelper
    {
        private static Logger _ipaLogger;

        private static ILogger<ZenjectHelper> _logger;
        private static Dictionary<string, SceneContext> _sceneContexts = new Dictionary<string, SceneContext>();

        internal static void Init(Harmony harmony, Logger logger)
        {
            _ipaLogger = logger;
            _logger = new IPALogger<ZenjectHelper>(_ipaLogger);

            ApplyPatches(harmony, _ipaLogger);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var sceneContext = UnityEngine.Object.FindObjectsOfType<SceneContext>().FirstOrDefault(sc => sc.gameObject.scene == scene);

            if (sceneContext)
            {
                _logger.Info($"Got Scene Context for scene '{scene.name}'");
                _sceneContexts.Add(scene.name, sceneContext);
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            if (_sceneContexts.ContainsKey(scene.name))
            {
                _sceneContexts.Remove(scene.name);
            }
        }

        private static void ApplyPatches(Harmony harmony, Logger logger)
        {
            var methodToPatch = typeof(AppCoreInstaller).GetMethod("InstallBindings", BindingFlags.Public | BindingFlags.Instance);
            var patch = new HarmonyMethod(typeof(ZenjectHelper).GetMethod(nameof(InstallBindings), BindingFlags.NonPublic | BindingFlags.Static));

            harmony.Patch(methodToPatch, null, patch);
        }

        public static void GetMainSceneContextAsync(Action<SceneContext> contextInstalled)
        {
            GetSceneContextAsync(contextInstalled, "PCInit");
        }

        public static void GetGameSceneContextAsync(Action<SceneContext> contextInstalled)
        {
            GetSceneContextAsync(contextInstalled, "GameplayCore");
        }

        private static void GetSceneContextAsync(Action<SceneContext> contextInstalled, string sceneName)
        {
            if (contextInstalled == null) throw new ArgumentNullException(nameof(contextInstalled));
            if (string.IsNullOrEmpty(sceneName)) throw new ArgumentNullException(nameof(sceneName));

            if (!SceneManager.GetSceneByName(sceneName).isLoaded) throw new Exception($"Scene '{sceneName}' is not loaded");
            if (!_sceneContexts.ContainsKey(sceneName)) throw new Exception($"Scene '{sceneName}' does not have a Scene Context");

            var sceneContext = _sceneContexts[sceneName];

            if (sceneContext.HasInstalled)
            {
                contextInstalled(sceneContext);
            }
            else
            {
                sceneContext.OnPostInstall.AddListener(() => contextInstalled(sceneContext));
            }
        }

        private static void InstallBindings(AppCoreInstaller __instance)
        {
            _logger.Info("Installing bindings into AppCoreInstaller");

            DiContainer container = new Traverse(__instance).Property<DiContainer>("Container").Value;

            container.Install<CustomAvatarsInstaller>(new object[] { _ipaLogger });
            container.Install<UIInstaller>();
        }
    }
}
