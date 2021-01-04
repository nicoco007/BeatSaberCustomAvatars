using CustomAvatar.Logging;
using HarmonyLib;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class ZenjectHelper
    {
        private const string kExpectedFirstSceneContextName = "AppCoreSceneContext";

        private static readonly Type[] kContextTypesWithSceneBindings = new[] { typeof(ProjectContext), typeof(SceneContext), typeof(GameObjectContext) };

        private static readonly FieldAccessor<SceneContext, List<SceneDecoratorContext>>.Accessor _decoratorContextsAccessor = FieldAccessor<SceneContext, List<SceneDecoratorContext>>.GetAccessor("_decoratorContexts");
        private static readonly FieldAccessor<SceneDecoratorContext, List<MonoBehaviour>>.Accessor _injectableMonoBehavioursAccessor = FieldAccessor<SceneDecoratorContext, List<MonoBehaviour>>.GetAccessor("_injectableMonoBehaviours");

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
            foreach (Type type in kContextTypesWithSceneBindings)
            {
                _logger.Trace($"Applying patch to '{type.FullName}'");

                MethodInfo methodToPatch = type.GetMethod("InstallBindings", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(List<MonoBehaviour>) }, null);
                MethodInfo patch = typeof(ZenjectHelper).GetMethod(nameof(InstallBindings), BindingFlags.NonPublic | BindingFlags.Static);

                harmony.Patch(methodToPatch, null, new HarmonyMethod(patch));
            }
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

        private static void InstallBindings(Context __instance, List<MonoBehaviour> injectableMonoBehaviours)
        {
            if (__instance is SceneContext sceneContext)
            {
                injectableMonoBehaviours.AddRange(_decoratorContextsAccessor(ref sceneContext).SelectMany(dc => _injectableMonoBehavioursAccessor(ref dc)));
            }

            foreach (MonoBehaviour monoBehaviour in injectableMonoBehaviours)
            {
                Type type = monoBehaviour.GetType();

                if (!_typesToExpose.Contains(type)) continue;

                if (!__instance.Container.HasBinding(type))
                {
                    _logger.Trace($"Exposing MonoBehaviour '{type.FullName}' in context '{__instance.name}'");
                    __instance.Container.Bind(type).FromInstance(monoBehaviour).AsSingle();
                }
                else
                {
                    _logger.Trace($"Not exposing MonoBehaviour '{type.FullName}' in context '{__instance.name}' since a binding already exists");
                }
            }
        }
    }
}
