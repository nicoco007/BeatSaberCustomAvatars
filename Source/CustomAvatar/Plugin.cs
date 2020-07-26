using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using CustomAvatar.Zenject;
using HarmonyLib;
using IPA;
using System;
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
            BeatSaberUtilities.ApplyPatches(harmony);

            ZenjectHelper.RegisterInstaller<CustomAvatarsInstaller>("PCInit", "AppCoreSceneContext", ipaLogger);
            ZenjectHelper.RegisterInstaller<UIInstaller>("MenuViewControllers");
            ZenjectHelper.RegisterInstaller<GameplayInstaller>("GameplayCore");
        }
    }
}
