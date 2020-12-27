using CustomAvatar.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class ZenjectHelper
    {
        private static readonly string kExpectedFirstSceneContextName = "AppCoreSceneContext";

        private static bool _shouldInstall;

        private static readonly List<InstallerRegistration> _installerRegistrations = new List<InstallerRegistration>();
        private static readonly List<Type> _typesToExpose = new List<Type>();

        private static ILogger<ZenjectHelper> _logger;

        internal static void Init(Harmony harmony, IPA.Logging.Logger logger)
        {
            _logger = new IPALogger<ZenjectHelper>(logger);

            PatchInstallInstallers(harmony);
            PatchInstallBindings(harmony);
        }

        public static InstallerRegistration Register<TInstaller>() where TInstaller : Installer
        {
            var registration = new InstallerRegistration(typeof(TInstaller));

            _installerRegistrations.Add(registration);

            return registration;
        }

        public static void ExposeSceneBinding<T>() where T : MonoBehaviour
        {
            _typesToExpose.Add(typeof(T));
        }

        private static void PatchInstallInstallers(Harmony harmony)
        {
            MethodInfo methodToPatch = typeof(Context).GetMethod("InstallInstallers", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
            MethodInfo patch = typeof(ZenjectHelper).GetMethod(nameof(InstallInstallers), BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(methodToPatch, null, new HarmonyMethod(patch));
        }

        private static void PatchInstallBindings(Harmony harmony)
        {
            MethodInfo methodToPatch = typeof(Context).GetMethod("InstallSceneBindings", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(List<MonoBehaviour>) }, null);
            MethodInfo patch = typeof(ZenjectHelper).GetMethod(nameof(InstallSceneBindings), BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(methodToPatch, new HarmonyMethod(patch));
        }

        private static void InstallInstallers(Context __instance)
        {
            if (!_shouldInstall)
            {
                if (__instance.name == kExpectedFirstSceneContextName)
                {
                    _shouldInstall = true;
                }
                else
                {
                    if (!(__instance is ProjectContext))
                    {
                        _logger.Warning($"Ignoring {__instance.GetType().Name} '{__instance.name}' since SceneContext '{kExpectedFirstSceneContextName}' hasn't loaded yet");
                    }

                    return;
                }
            }

            _logger.Trace($"Handling {__instance.GetType().Name} '{__instance.name}' (scene '{__instance.gameObject.scene.name}')");

            foreach (InstallerRegistration installerRegistration in _installerRegistrations)
            {
                if (installerRegistration.TryInstallInto(__instance))
                {
                    _logger.Trace($"Installed {installerRegistration.installer.FullName}");
                }
            }
        }

        private static void InstallSceneBindings(Context __instance, List<MonoBehaviour> injectableMonoBehaviours)
        {
            foreach (MonoBehaviour monoBehaviour in injectableMonoBehaviours)
            {
                Type type = monoBehaviour.GetType();

                if (_typesToExpose.Contains(type))
                {
                    if (!__instance.Container.HasBinding(type))
                    {
                        __instance.Container.Bind(type).FromInstance(monoBehaviour).AsSingle();
                        _logger.Trace($"Exposed MonoBehaviour '{type.FullName}' in context '{__instance.name}'");
                    }
                    else
                    {
                        _logger.Warning($"Not exposing MonoBehaviour '{type.FullName}' in context '{__instance.name}' since a binding already exists");
                    }
                }
            }
        }
    }
}
