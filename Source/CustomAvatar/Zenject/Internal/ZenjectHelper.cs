//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Logging;
using HarmonyLib;
using UnityEngine;
using Zenject;

#if DEBUG
using System.Diagnostics;
#endif

namespace CustomAvatar.Zenject.Internal
{
    internal class ZenjectHelper
    {
        private static readonly Dictionary<Type, List<ComponentRegistration>> kComponentsToAdd = new();

        private static ILogger<ZenjectHelper> _logger;

        internal static void Init(IPA.Logging.Logger logger)
        {
            _logger = new IPALogger<ZenjectHelper>(logger);
        }

        public static void AddComponentAlongsideExisting<TExisting, TAdd>(string childTransformName = null, Func<GameObject, bool> condition = null) where TExisting : MonoBehaviour where TAdd : MonoBehaviour
        {
            var componentRegistration = new ComponentRegistration(typeof(TAdd), childTransformName, condition);

            if (kComponentsToAdd.TryGetValue(typeof(TExisting), out List<ComponentRegistration> types))
            {
                types.Add(componentRegistration);
            }
            else
            {
                kComponentsToAdd.Add(typeof(TExisting), new List<ComponentRegistration> { componentRegistration });
            }
        }

        private static void InstallBindings(Context __instance, List<MonoBehaviour> injectableMonoBehaviours)
        {
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            if (__instance is SceneContext sceneContext)
            {
                injectableMonoBehaviours.AddRange(sceneContext._decoratorContexts.SelectMany(dc => dc._injectableMonoBehaviours));
            }

            foreach (MonoBehaviour monoBehaviour in injectableMonoBehaviours)
            {
                AddComponents(__instance, monoBehaviour);
            }

#if DEBUG
            _logger.LogTrace($"InstallBindings: {stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000)} us");
#endif
        }

        private static void AddComponents(Context context, MonoBehaviour monoBehaviour)
        {
            Type monoBehaviourType = monoBehaviour.GetType();

            if (!kComponentsToAdd.TryGetValue(monoBehaviourType, out List<ComponentRegistration> componentsToAdd)) return;

            foreach (ComponentRegistration componentRegistration in componentsToAdd)
            {
                GameObject target = monoBehaviour.gameObject;

                if (!string.IsNullOrEmpty(componentRegistration.childTransformName))
                {
                    Transform transform = target.transform.Find(componentRegistration.childTransformName);

                    if (!transform)
                    {
                        _logger.LogWarning($"Could not find transform '{componentRegistration.childTransformName}' under '{target.name}'");
                        continue;
                    }

                    target = transform.gameObject;
                }

                if (componentRegistration.condition != null && !componentRegistration.condition(target))
                {
                    _logger.LogTrace($"Condition not met for putting '{componentRegistration.type.FullName}' onto '{target.name}'");
                    continue;
                }

                _logger.LogTrace($"Adding '{componentRegistration.type.FullName}' to GameObject '{target.name}' (for '{monoBehaviourType.FullName}')");

                Component component = target.AddComponent(componentRegistration.type);
                context.Container.QueueForInject(component);
            }
        }

        private class ComponentRegistration
        {
            public Type type { get; }
            public string childTransformName { get; }
            public Func<GameObject, bool> condition { get; }

            public ComponentRegistration(Type type, string childTransformName, Func<GameObject, bool> condition)
            {
                this.type = type;
                this.childTransformName = childTransformName;
                this.condition = condition;
            }
        }

        [HarmonyPatch(typeof(ProjectContext), nameof(ProjectContext.InstallBindings))]
        private static class ProjectContext_InstallBindings
        {
            public static void Postfix(ProjectContext __instance, List<MonoBehaviour> injectableMonoBehaviours)
            {
                InstallBindings(__instance, injectableMonoBehaviours);
            }
        }

        [HarmonyPatch(typeof(SceneContext), nameof(SceneContext.InstallBindings))]
        private static class SceneContext_InstallBindings
        {
            public static void Postfix(SceneContext __instance, List<MonoBehaviour> injectableMonoBehaviours)
            {
                InstallBindings(__instance, injectableMonoBehaviours);
            }
        }

        [HarmonyPatch(typeof(GameObjectContext), nameof(GameObjectContext.InstallBindings))]
        private static class GameObjectContext_InstallBindings
        {
            public static void Postfix(GameObjectContext __instance, List<MonoBehaviour> injectableMonoBehaviours)
            {
                InstallBindings(__instance, injectableMonoBehaviours);
            }
        }
    }
}
