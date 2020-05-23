using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace CustomAvatar
{
    public class ZenjectHelper
    {
        internal static void ApplyPatches(Harmony harmony, IPA.Logging.Logger logger)
        {
            var methodToPatch = typeof(AppCoreInstaller).GetMethod("InstallBindings", BindingFlags.Public | BindingFlags.Instance);

            var patch = new HarmonyMethod(new Action<AppCoreInstaller>((__instance) =>
            {
                DiContainer container = new Traverse(__instance).Property<DiContainer>("Container").Value;

                container.Install<CustomAvatarsInstaller>(new object[] { logger });
                container.Install<UIInstaller>();
            }).Method);

            harmony.Patch(methodToPatch, null, patch);
        }

        public static void GetMainSceneContext(Action<SceneContext> success)
        {
            GetSceneContext(success, "PCInit");
        }

        public static void GetGameSceneContext(Action<SceneContext> success)
        {
            GetSceneContext(success, "GameplayCore");
        }

        private static void GetSceneContext(Action<SceneContext> success, string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded) throw new Exception($"Scene '{sceneName}' is not loaded");

            List<SceneContext> sceneContexts = Resources.FindObjectsOfTypeAll<SceneContext>().Where(sc => sc.gameObject.scene.name == sceneName).ToList();

            if (sceneContexts.Count == 0)
            {
                throw new Exception($"Scene context not found in scene '{sceneName}'");
            }

            if (sceneContexts.Count > 1)
            {
                throw new Exception($"More than one scene context found in scene '{sceneName}'");
            }

            SceneContext sceneContext = sceneContexts[0];

            if (sceneContext.HasInstalled)
            {
                success(sceneContext);
            }
            else
            {
                sceneContext.OnPostInstall.AddListener(() => success(sceneContext));
            }
        }
    }
}
