//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
    internal class Plugin
    {
        private readonly Harmony _harmony = new("com.nicoco007.beatsabercustomavatars");

        [Init]
        public Plugin(Logger ipaLogger, PluginMetadata pluginMetadata, Zenjector zenjector)
        {
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

            zenjector.Install<CustomAvatarsInstaller>(Location.App, ipaLogger, pluginMetadata);
            zenjector.Install<MainMenuInstaller>(Location.Menu);
            zenjector.Install<HealthWarningInstaller>(Location.HealthWarning | Location.Credits);
            zenjector.Install<GameInstaller>(Location.Player);
        }

        [OnEnable]
        public void OnEnable()
        {
            _harmony.PatchAll();
        }

        [OnDisable]
        public void OnDisable()
        {
            _harmony.UnpatchSelf();
        }
    }
}
