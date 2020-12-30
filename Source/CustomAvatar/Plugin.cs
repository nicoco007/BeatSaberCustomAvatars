//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using CustomAvatar.Zenject;
using CustomAvatar.Zenject.Internal;
using HarmonyLib;
using IPA;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    internal class Plugin
    {
        [Init]
        public Plugin(Logger ipaLogger)
        {
            // can't inject at this point so just create it
            ILogger<Plugin> logger = new IPALogger<Plugin>(ipaLogger);

            logger.Info("Initializing Custom Avatars");

            Harmony harmony = new Harmony("com.nicoco007.beatsabercustomavatars");

            ZenjectHelper.Init(harmony, ipaLogger);
            BeatSaberEvents.ApplyPatches(harmony, ipaLogger);

            ZenjectHelper.ExposeSceneBinding<SmoothCamera>();

            ZenjectHelper.Register<CustomAvatarsInstaller>().WithArguments(ipaLogger).OnMonoInstaller<PCAppInit>();
            ZenjectHelper.Register<UIInstaller>().OnMonoInstaller<MenuViewControllersInstaller>();

            ZenjectHelper.Register<LightingInstaller>().OnContext("HealthWarning", "SceneContext");
            ZenjectHelper.Register<LightingInstaller>().OnContext("MenuEnvironment", "SceneDecoratorContext");
            ZenjectHelper.Register<LightingInstaller>().OnContext("GameCore", "SceneContext");

            ZenjectHelper.Register<GameInstaller>().OnMonoInstaller<GameplayCoreInstaller>();

            ZenjectHelper.Register<CustomAvatarsLocalInactivePlayerInstaller>().OnMonoInstaller<MultiplayerLocalInactivePlayerInstaller>();
        }

        [OnStart, OnExit]
        public void NoOp() { }
    }
}
