using IPA;
using System;
using System.Reflection;
using UnityEngine;
using Logger = IPA.Logging.Logger;
using CustomAvatar.Logging;
using HarmonyLib;
using CustomAvatar.Zenject;

namespace CustomAvatar
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    internal class Plugin
    {
        private ILogger<Plugin> _logger;

        [Init]
        public Plugin(Logger logger)
        {
            // can't inject at this point so just create it
            _logger = new IPALogger<Plugin>(logger);

            _logger.Info("Initializing Custom Avatars");

            try
            {
                Harmony harmony = new Harmony("com.nicoco007.beatsabercustomavatars");

                ZenjectHelper.Init(harmony, logger);
                BeatSaberEvents.ApplyPatches(harmony);
                PatchMirrorRendererSO(harmony);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to apply patches");
                _logger.Error(ex);
            }

            ZenjectHelper.RegisterInstaller<CustomAvatarsInstaller>("PCInit", "AppCoreSceneContext", logger);
            ZenjectHelper.RegisterInstaller<UIInstaller>("MenuViewControllers");
            ZenjectHelper.RegisterInstaller<GameplayInstaller>("GameplayCore");
        }

        // TODO put this somewhere else
        private void PatchMirrorRendererSO(Harmony harmony)
        {
            var methodToPatch = typeof(MirrorRendererSO).GetMethod("CreateOrUpdateMirrorCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            var patch = new HarmonyMethod(typeof(Plugin).GetMethod(nameof(MirrorRendererSOPatch), BindingFlags.NonPublic | BindingFlags.Static));

            harmony.Patch(methodToPatch, null, patch);
        }

        private static void MirrorRendererSOPatch(MirrorRendererSO __instance)
        {
            Camera mirrorCamera = new Traverse(__instance).Field<Camera>("_mirrorCamera").Value;

            mirrorCamera.cullingMask |= (1 << AvatarLayers.kOnlyInThirdPerson) | (1 << AvatarLayers.kOnlyInFirstPerson) | (1 << AvatarLayers.kAlwaysVisible);
        }
    }
}
