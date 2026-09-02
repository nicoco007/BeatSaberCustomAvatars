//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using BGLib.AppFlow.Initialization;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Rendering.Cameras;
using CustomAvatar.Zenject;
using HarmonyLib;
using IPA;
using IPA.Loader;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.DynamicInit)]
    [HarmonyPatch]
    internal class Plugin
    {
        private static event Action<ProjectContext> _projectContextInstalling;

        private readonly Harmony _harmony = new("com.nicoco007.beatsabercustomavatars");

        private readonly Logger _ipaLogger;

        private readonly PluginMetadata _pluginMetadata;

        [Init]
        public Plugin(Logger ipaLogger, PluginMetadata pluginMetadata, Zenjector zenjector)
        {
            _ipaLogger = ipaLogger;
            _pluginMetadata = pluginMetadata;

            // can't inject at this point so just create it
            ILogger<Plugin> logger = new IPALogger<Plugin>(ipaLogger);

            logger.LogInformation("Initializing Custom Avatars");

            zenjector.Mutate<MainCamera, MainCameraTracker>();
            zenjector.Mutate<SmoothCamera, Rendering.Cameras.SmoothCamera>();
            zenjector.Mutate<MenuEnvironmentManager, EnvironmentObject>();
            zenjector.Mutate<MultiplayerLocalActivePlayerFacade>((ctx, inst) =>
            {
                Transform transform = inst.transform.Find("IsActiveObjects");
                DiContainer container = ctx.Container;
                container.QueueForInject(transform.Find("Lasers").gameObject.AddComponent<EnvironmentObject>());
                container.QueueForInject(transform.Find("Construction").gameObject.AddComponent<EnvironmentObject>());
                container.QueueForInject(transform.Find("CenterRings").gameObject.AddComponent<EnvironmentObject>());
            });
            zenjector.Mutate<MultiplayerLocalInactivePlayerFacade, EnvironmentObject>(gameObjectGetter: (ctx, m) => m.transform.Find("MultiplayerLocalInactivePlayerPlayerPlace/CirclePlayerPlace").gameObject);
            zenjector.Mutate<MultiplayerConnectedPlayerFacade, EnvironmentObject>();
            zenjector.Mutate<VRController, VRControllerVisuals>();

            zenjector.Install<MainMenuInstaller>(Location.Menu);
            zenjector.Install<HealthWarningInstaller>(Location.HealthWarning | Location.Credits);
            zenjector.Install<GameInstaller>(Location.Player);
        }

        [OnEnable]
        public void OnEnable()
        {
            _projectContextInstalling += ProjectContextInstalling;
            _harmony.PatchAll();
        }

        [OnDisable]
        public void OnDisable()
        {
            _harmony.UnpatchSelf();
            _projectContextInstalling -= ProjectContextInstalling;
        }

        private void ProjectContextInstalling(ProjectContext projectContext)
        {
            projectContext._normalInstallers.Add(projectContext.Container.Instantiate<ProjectInstaller>([_ipaLogger, _pluginMetadata]));
        }

        [HarmonyPatch(typeof(Context), nameof(Context.InstallInstallers), [typeof(List<InstallerBase>), typeof(List<Type>), typeof(List<ScriptableObjectInstaller>), typeof(List<MonoInstaller>), typeof(List<MonoInstaller>)])]
        [HarmonyPrefix]
        private static void Context_InstallInstallers(Context __instance)
        {
            if (__instance is ProjectContext projectContext)
            {
                _projectContextInstalling?.Invoke(projectContext);
            }
        }

        [HarmonyPatch(typeof(AsyncSceneContext), nameof(AsyncSceneContext.LoadInstallersAsync))]
        [HarmonyPrefix]
        private static void Prefix(AsyncSceneContext __instance)
        {
            if (__instance.name != "AppCoreSceneContext")
            {
                return;
            }

            __instance._asyncInstallers.Add(__instance.gameObject.AddComponent<CustomAvatarsInstaller>());
        }
    }
}
