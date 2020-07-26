using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomAvatar.Logging;
using HarmonyLib;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Zenject
{
    public class ZenjectHelper
    {
        private static ILogger<ZenjectHelper> _logger;
        private static Harmony _harmony;

        private static readonly Dictionary<string, List<InstallerRegistration>> _installers = new Dictionary<string, List<InstallerRegistration>>();
        private static readonly MethodInfo _installMethod = typeof(DiContainer).GetMethod("Install", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, new[] { typeof(object[]) }, null);

        internal static void Init(Harmony harmony, Logger logger)
        {
            _logger = new IPALogger<ZenjectHelper>(logger);
            _harmony = harmony;

            var methodToPatch = typeof(Context).GetMethod("InstallInstallers", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
            var patch = new HarmonyMethod(typeof(ZenjectHelper).GetMethod(nameof(InstallInstallers), BindingFlags.NonPublic | BindingFlags.Static));

            harmony.Patch(methodToPatch, null, patch);
        }

        public static void RegisterCoreInstaller<TInstaller>(params object[] extraArgs) where TInstaller : IInstaller => RegisterInstaller<TInstaller>("PCInit", "AppCoreSceneContext", extraArgs);
        public static void RegisterMenuInstaller<TInstaller>(params object[] extraArgs) where TInstaller : IInstaller => RegisterInstaller<TInstaller>("MenuCore", null, extraArgs);
        public static void RegisterMenuViewControllersInstaller<TInstaller>(params object[] extraArgs) where TInstaller : IInstaller => RegisterInstaller<TInstaller>("MenuViewControllers", null, extraArgs);
        public static void RegisterGameplayInstaller<TInstaller>(params object[] extraArgs) where TInstaller : IInstaller => RegisterInstaller<TInstaller>("GameplayCore", null, extraArgs);

        public static void RegisterInstaller<TInstaller>(string sceneName, string sceneContextName = null, params object[] extraArgs) where TInstaller : IInstaller
        {
            if (string.IsNullOrEmpty(sceneName)) throw new ArgumentNullException(nameof(sceneName));
            if (extraArgs == null) throw new ArgumentNullException(nameof(extraArgs));

            if (!Application.CanStreamedLevelBeLoaded(sceneName)) throw new InvalidOperationException($"Scene '{sceneName}' does not exist!");

            if (!string.IsNullOrEmpty(sceneContextName))
            {
                _logger.Trace($"Registering '{typeof(TInstaller).Name}' on scene context '{sceneContextName}' in scene '{sceneName}'");
            }
            else
            {
                _logger.Trace($"Registering '{typeof(TInstaller).Name}' in scene '{sceneName}'");
            }

            if (!_installers.ContainsKey(sceneName))
            {
                _installers.Add(sceneName, new List<InstallerRegistration>());
            }

            Type installerType = typeof(TInstaller);

            if (_installers[sceneName].Any(r => r.installerType == installerType && r.sceneContextName == sceneContextName)) throw new InvalidOperationException($"'{installerType.Name}' already registered on '{sceneContextName}' in '{sceneName}'");

            _installers[sceneName].Add(new InstallerRegistration(installerType, sceneContextName, extraArgs));
        }

        private static void InstallInstallers(Context __instance)
        {
            string sceneContextName = __instance.name;
            string sceneName = __instance.gameObject.scene.name;

            if (!_installers.ContainsKey(sceneName))
            {
                _logger.Trace($"Nothing to do in scene '{sceneName}'");
                return;
            }

            List<InstallerRegistration> installersForSceneContext = _installers[sceneName].Where(r => string.IsNullOrEmpty(r.sceneContextName) || r.sceneContextName == sceneContextName).ToList();

            if (installersForSceneContext.Count == 0)
            {
                _logger.Trace($"Nothing to do on Scene Context '{sceneContextName}' (scene '{sceneName}')");
                return;
            }

            _logger.Info($"Installing registered installers on Scene Context '{sceneContextName}' (scene '{sceneName}')");

            foreach (InstallerRegistration registration in installersForSceneContext)
            {
                _logger.Trace($"Installing '{registration.installerType.Name}'");
                _installMethod.MakeGenericMethod(registration.installerType).Invoke(__instance.Container, new[] { registration.extraArgs });
            }
        }

        private struct InstallerRegistration
        {
            public readonly Type installerType;
            public readonly string sceneContextName;
            public readonly object[] extraArgs;

            public InstallerRegistration(Type installerType, string sceneContextName, object[] extraArgs)
            {
                this.installerType = installerType;
                this.sceneContextName = sceneContextName;
                this.extraArgs = extraArgs;
            }
        }
    }
}
