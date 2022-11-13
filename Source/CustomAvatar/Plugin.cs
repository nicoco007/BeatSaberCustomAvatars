//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using BeatSaberMarkupLanguage;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.UI.CustomTags;
using CustomAvatar.Zenject;
using CustomAvatar.Zenject.Internal;
using HarmonyLib;
using IPA;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.DynamicInit)]
    internal class Plugin
    {
        private readonly Harmony _harmony = new Harmony("com.nicoco007.beatsabercustomavatars");

        [Init]
        public Plugin(Logger ipaLogger)
        {
            var armSpanSliderTag = new ValuePickerTag();

            BSMLParser.instance.RegisterTag(armSpanSliderTag);
            BSMLParser.instance.RegisterTypeHandler(new ValuePickerHandler());

            // can't inject at this point so just create it
            ILogger<Plugin> logger = new IPALogger<Plugin>(ipaLogger);

            logger.LogInformation("Initializing Custom Avatars");

            ZenjectHelper.Init(ipaLogger);

            ZenjectHelper.BindSceneComponent<PCAppInit>();
            ZenjectHelper.BindSceneComponent<ObstacleSaberSparkleEffectManager>();

            ZenjectHelper.AddComponentAlongsideExisting<MainCamera, CustomAvatarsMainCameraController>();
            ZenjectHelper.AddComponentAlongsideExisting<SmoothCamera, CustomAvatarsSmoothCameraController>();

            ZenjectHelper.AddComponentAlongsideExisting<MenuEnvironmentManager, EnvironmentObject>();
            ZenjectHelper.AddComponentAlongsideExisting<MultiplayerLocalActivePlayerFacade, EnvironmentObject>("IsActiveObjects/Lasers");
            ZenjectHelper.AddComponentAlongsideExisting<MultiplayerLocalActivePlayerFacade, EnvironmentObject>("IsActiveObjects/Construction");
            ZenjectHelper.AddComponentAlongsideExisting<MultiplayerLocalActivePlayerFacade, EnvironmentObject>("IsActiveObjects/CenterRings");
            ZenjectHelper.AddComponentAlongsideExisting<MultiplayerLocalInactivePlayerFacade, EnvironmentObject>("MultiplayerLocalInactivePlayerPlayerPlace/CirclePlayerPlace");
            ZenjectHelper.AddComponentAlongsideExisting<MultiplayerConnectedPlayerFacade, EnvironmentObject>();

            ZenjectHelper.Register<CustomAvatarsInstaller>().WithArguments(ipaLogger).OnMonoInstaller<PCAppInit>();
            ZenjectHelper.Register<MainMenuInstaller>().WithArguments(armSpanSliderTag).OnContext("MainMenu", "MenuCore");
            ZenjectHelper.Register<HealthWarningInstaller>().OnContext("HealthWarning", "SceneContext");
            ZenjectHelper.Register<GameInstaller>().OnMonoInstaller<GameplayCoreInstaller>();
            ZenjectHelper.Register<EnvironmentInstaller>().OnDecoratorContext("Environment");
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
