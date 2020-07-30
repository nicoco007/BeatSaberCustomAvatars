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
        public static readonly string kInitSceneName = "PCInit";
        public static readonly string kInitSceneContextName = "AppCoreSceneContext";
        public static readonly string kMenuSceneName = "MenuCore";
        public static readonly string kMenuViewControllersSceneName = "MenuViewControllers";
        public static readonly string kGameplaySceneName = "GameplayCore";

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

        /// <summary>
        /// Registers an installer in the PCInit scene. This is the first scene that loads when starting the game.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to register</typeparam>
        /// <param name="extraArgs">Extra values to be injected into <typeparamref name="TInstaller"/></param>
        public static void RegisterInitInstaller<TInstaller>(params object[] extraArgs) where TInstaller : Installer => RegisterInstaller<TInstaller>(kInitSceneName, kInitSceneContextName, extraArgs);

        /// <summary>
        /// Registers an installer in the MenuCore scene.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to register</typeparam>
        /// <param name="extraArgs">Extra values to be injected into <typeparamref name="TInstaller"/></param>
        public static void RegisterMenuInstaller<TInstaller>(params object[] extraArgs) where TInstaller : Installer => RegisterInstaller<TInstaller>(kMenuSceneName, null, extraArgs);

        /// <summary>
        /// Registers an installer in the MenuViewControllers scene. This is usually used for UI-specific classes; for anything else, use <see cref="RegisterMenuInstaller{TInstaller}(object[])"/>.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to register</typeparam>
        /// <param name="extraArgs">Extra values to be injected into <typeparamref name="TInstaller"/></param>
        public static void RegisterMenuViewControllersInstaller<TInstaller>(params object[] extraArgs) where TInstaller : Installer => RegisterInstaller<TInstaller>(kMenuViewControllersSceneName, null, extraArgs);
        
        /// <summary>
        /// Registers an installer in the GameplayCore scene.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to register</typeparam>
        /// <param name="extraArgs">Extra values to be injected into <typeparamref name="TInstaller"/></param>
        public static void RegisterGameplayInstaller<TInstaller>(params object[] extraArgs) where TInstaller : Installer => RegisterInstaller<TInstaller>(kGameplaySceneName, null, extraArgs);

        /// <summary>
        /// Registers an installer in the specified scene. If there is more than one scene context in a scene (e.g. in PCInit), use <paramref name="sceneContextName"/> to specify the name of the Scene Context.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to register</typeparam>
        /// <param name="sceneName">Name of the Scene Context's scene</param>
        /// <param name="sceneContextName">Name of the Scene Context (optional)</param>
        /// <param name="extraArgs">Extra values to be injected into <typeparamref name="TInstaller"/></param>
        public static void RegisterInstaller<TInstaller>(string sceneName, string sceneContextName = null, params object[] extraArgs) where TInstaller : Installer
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

            if (_installers[sceneName].Any(r => r.installerType == installerType && string.IsNullOrEmpty(r.sceneContextName))) throw new InvalidOperationException($"'{installerType.Name}' already registered globally in '{sceneName}'");
            if (_installers[sceneName].Any(r => r.installerType == installerType && r.sceneContextName == sceneContextName)) throw new InvalidOperationException($"'{installerType.Name}' already registered on '{sceneContextName}' in '{sceneName}'");

            _installers[sceneName].Add(new InstallerRegistration(installerType, sceneContextName, extraArgs));
        }

        /// <summary>
        /// Checks if the specified installer has been registered in the PCInit scene.
        /// </summary>
        /// <typeparam name="TInstaller">Installer for which to check registration</typeparam>
        /// <returns>Whether the installer has been registered or not</returns>
        public static bool IsInitInstallerRegistered<TInstaller>() where TInstaller : Installer => IsInstallerRegistered<TInstaller>(kInitSceneName, kInitSceneContextName);

        /// <summary>
        /// Checks if the specified installer has been registered in the MenuCore scene.
        /// </summary>
        /// <typeparam name="TInstaller">Installer for which to check registration</typeparam>
        /// <returns>Whether the installer has been registered or not</returns>
        public static bool IsMenuInstallerRegistered<TInstaller>() where TInstaller : Installer => IsInstallerRegistered<TInstaller>(kMenuSceneName);

        /// <summary>
        /// Checks if the specified installer has been registered in the MenuViewControllers scene.
        /// </summary>
        /// <typeparam name="TInstaller">Installer for which to check registration</typeparam>
        /// <returns>Whether the installer has been registered or not</returns>
        public static bool IsMenuViewControllersInstallerRegistered<TInstaller>() where TInstaller : Installer => IsInstallerRegistered<TInstaller>(kMenuViewControllersSceneName);

        /// <summary>
        /// Checks if the specified installer has been registered in the GameplayCore scene.
        /// </summary>
        /// <typeparam name="TInstaller">Installer for which to check registration</typeparam>
        /// <returns>Whether the installer has been registered or not</returns>
        public static bool IsGameplayInstallerRegistered<TInstaller>() where TInstaller : Installer => IsInstallerRegistered<TInstaller>(kGameplaySceneName);

        /// <summary>
        /// Checks if the specified installer has been registered in the specified scene & scene context (if applicable).
        /// </summary>
        /// <typeparam name="TInstaller">Installer for which to check registration</typeparam>
        /// <param name="sceneName">Name of the Scene Context's scene</param>
        /// <param name="sceneContextName">Name of the Scene Context (optional)</param>
        /// <returns>Whether the installer has been registered or not</returns>
        public static bool IsInstallerRegistered<TInstaller>(string sceneName, string sceneContextName = null)
        {
            return _installers.ContainsKey(sceneName) && _installers[sceneName].Any(r => r.sceneContextName == sceneContextName);
        }

        /// <summary>
        /// Deregisters an installer from the PCInit scene. This does not destroy injected instances of components bound by the installer.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to deregister</typeparam>
        public static void DeregisterInitInstaller<TInstaller>() where TInstaller : Installer => DeregisterInstaller<TInstaller>(kInitSceneName, kInitSceneContextName);
        /// <summary>
        /// Deregisters an installer from the MenuCore scene. This does not destroy injected instances of components bound by the installer.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to deregister</typeparam>
        public static void DeregisterMenuInstaller<TInstaller>() where TInstaller : Installer => DeregisterInstaller<TInstaller>(kMenuSceneName);

        /// <summary>
        /// Deregisters an installer from the MenuViewControllers scene. This does not destroy injected instances of components bound by the installer.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to deregister</typeparam>
        public static void DeregisterMenuViewControllersInstaller<TInstaller>() where TInstaller : Installer => DeregisterInstaller<TInstaller>(kMenuViewControllersSceneName);

        /// <summary>
        /// Deregisters an installer from the GameplayCore scene. This does not destroy injected instances of components bound by the installer.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to deregister</typeparam>
        public static void DeregisterGameplayInstaller<TInstaller>() where TInstaller : Installer => DeregisterInstaller<TInstaller>(kGameplaySceneName);

        /// <summary>
        /// Deregisters an installer from the specified scene. If there is more than one scene context in a scene (e.g. in PCInit), use <paramref name="sceneContextName"/> to specify the name of the Scene Context. This does not destroy injected instances of components bound by the installer.
        /// </summary>
        /// <typeparam name="TInstaller">Installer to deregister</typeparam>
        /// <param name="sceneName">Name of the Scene Context's scene</param>
        /// <param name="sceneContextName">Name of the Scene Context (optional)</param>
        public static void DeregisterInstaller<TInstaller>(string sceneName, string sceneContextName = null)
        {
            Type installerType = typeof(TInstaller);

            if (!IsInstallerRegistered<TInstaller>(sceneName, sceneContextName)) throw new InvalidOperationException($"'{installerType.Name}' is not registered on scene context '{sceneContextName}' in scene '{sceneName}'");

            if (!string.IsNullOrEmpty(sceneContextName))
            {
                _logger.Trace($"Deregistering '{typeof(TInstaller).Name}' from scene context '{sceneContextName}' in scene '{sceneName}'");
            }
            else
            {
                _logger.Trace($"Deregistering '{typeof(TInstaller).Name}' from scene '{sceneName}'");
            }

            _installers[sceneName].RemoveAll(r => r.installerType == installerType && r.sceneContextName == sceneContextName);
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
