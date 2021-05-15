using CustomAvatar.Logging;
using HarmonyLib;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;

#if DEBUG
using System.Diagnostics;
#endif

namespace CustomAvatar.Zenject.Internal
{
    internal class ZenjectHelper
    {
        private const string kExpectedFirstSceneContextName = "AppCoreSceneContext";

        private static readonly Type[] kContextTypesWithSceneBindings = new[] { typeof(ProjectContext), typeof(SceneContext), typeof(GameObjectContext) };

        private static readonly FieldAccessor<SceneContext, List<SceneDecoratorContext>>.Accessor _decoratorContextsAccessor = FieldAccessor<SceneContext, List<SceneDecoratorContext>>.GetAccessor("_decoratorContexts");
        private static readonly FieldAccessor<SceneDecoratorContext, List<MonoBehaviour>>.Accessor _injectableMonoBehavioursAccessor = FieldAccessor<SceneDecoratorContext, List<MonoBehaviour>>.GetAccessor("_injectableMonoBehaviours");

        private static bool _shouldInstall;

        private static readonly HashSet<InstallerRegistration> _installerRegistrations = new HashSet<InstallerRegistration>();
        private static readonly HashSet<Type> _componentsToBind = new HashSet<Type>();
        private static readonly Dictionary<Type, List<ComponentRegistration>> _componentsToAdd = new Dictionary<Type, List<ComponentRegistration>>();

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

        public static void BindSceneComponent<T>() where T : MonoBehaviour
        {
            _componentsToBind.Add(typeof(T));
        }

        public static void AddComponentAlongsideExisting<TExisting, TAdd>(string childTransformName = null, Func<GameObject, bool> condition = null, params object[] extraArgs) where TExisting : MonoBehaviour where TAdd : MonoBehaviour
        {
            var componentRegistration = new ComponentRegistration(typeof(TAdd), childTransformName, condition, extraArgs);

            if (_componentsToAdd.TryGetValue(typeof(TExisting), out List<ComponentRegistration> types))
            {
                types.Add(componentRegistration);
            }
            else
            {
                _componentsToAdd.Add(typeof(TExisting), new List<ComponentRegistration> { componentRegistration });
            }
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
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

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

            foreach (MonoInstaller installer in __instance.Installers)
            {
                BindIfNeeded(__instance, installer);
            }

            foreach (InstallerRegistration installerRegistration in _installerRegistrations)
            {
                if (installerRegistration.TryInstallInto(__instance))
                {
                    _logger.Trace($"Installed {installerRegistration.installer.FullName}");
                }
            }

#if DEBUG
            _logger.Trace($"InstallInstallers: {stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000)} us");
#endif
        }

        private static void InstallBindings(Context __instance, List<MonoBehaviour> injectableMonoBehaviours)
        {
            if (!_shouldInstall) return;

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            if (__instance is SceneContext sceneContext)
            {
                injectableMonoBehaviours.AddRange(_decoratorContextsAccessor(ref sceneContext).SelectMany(dc => _injectableMonoBehavioursAccessor(ref dc)));
            }

            foreach (MonoBehaviour monoBehaviour in injectableMonoBehaviours)
            {
                BindIfNeeded(__instance, monoBehaviour);
                AddComponents(__instance, monoBehaviour);
            }

#if DEBUG
            _logger.Trace($"InstallBindings: {stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000)} us");
#endif
        }

        private static void BindIfNeeded(Context context, MonoBehaviour monoBehaviour)
        {
            Type type = monoBehaviour.GetType();

            if (!_componentsToBind.Contains(type)) return;

            if (!context.Container.HasBinding(type))
            {
                _logger.Trace($"Binding '{type.FullName}' from {context.GetType().Name} '{context.name}' (scene '{context.gameObject.scene.name}')");

                context.Container.Bind(type).FromInstance(monoBehaviour).AsSingle().IfNotBound();
            }
            else
            {
                _logger.Trace($"'{type.FullName}' is already bound on {context.GetType().Name} '{context.name}' (scene '{context.gameObject.scene.name}')");
            }
        }

        private static void AddComponents(Context context, MonoBehaviour monoBehaviour)
        {
            Type monoBehaviourType = monoBehaviour.GetType();

            if (!_componentsToAdd.TryGetValue(monoBehaviourType, out List<ComponentRegistration> componentsToAdd)) return;

            foreach (ComponentRegistration componentRegistration in componentsToAdd)
            {
                GameObject target = monoBehaviour.gameObject;

                if (!string.IsNullOrEmpty(componentRegistration.childTransformName))
                {
                    Transform transform = target.transform.Find(componentRegistration.childTransformName);

                    if (!transform)
                    {
                        _logger.Warning($"Could not find transform '{componentRegistration.childTransformName}' under '{target.name}'");
                        continue;
                    }

                    target = transform.gameObject;
                }

                if (componentRegistration.condition != null && !componentRegistration.condition(target))
                {
                    _logger.Trace($"Condition not met for putting '{componentRegistration.type.FullName}' onto '{target.name}'");
                    continue;
                }

                _logger.Trace($"Adding '{componentRegistration.type.FullName}' to GameObject '{target.name}' (for '{monoBehaviourType.FullName}')");
                context.Container.InstantiateComponent(componentRegistration.type, target, componentRegistration.extraArgs);
            }
        }

        private class ComponentRegistration
        {
            public Type type { get; }
            public string childTransformName { get; }
            public Func<GameObject, bool> condition { get; }
            public object[] extraArgs { get; }

            public ComponentRegistration(Type type, string childTransformName, Func<GameObject, bool> condition, object[] extraArgs)
            {
                this.type = type;
                this.childTransformName = childTransformName;
                this.condition = condition;
                this.extraArgs = extraArgs;
            }
        }
    }
}
