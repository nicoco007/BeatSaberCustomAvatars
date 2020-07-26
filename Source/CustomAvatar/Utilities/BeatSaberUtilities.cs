using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BeatSaberUtilities
    {
        public static event Action<float> playerHeightChanged;

        public static void ApplyPatches(Harmony harmony)
        {
            PatchPlayerHeightProperty(harmony);
            PatchMirrorRendererSO(harmony);
        }

        private static void PatchPlayerHeightProperty(Harmony harmony)
        {
            MethodInfo playerHeightSetter = typeof(PlayerSpecificSettings).GetProperty(nameof(PlayerSpecificSettings.playerHeight), BindingFlags.Public | BindingFlags.Instance).SetMethod;
            HarmonyMethod postfixPatch = new HarmonyMethod(typeof(BeatSaberUtilities).GetMethod(nameof(OnPlayerHeightChanged), BindingFlags.NonPublic | BindingFlags.Static));
        
            harmony.Patch(playerHeightSetter, null, postfixPatch);
        }

        private static void PatchMirrorRendererSO(Harmony harmony)
        {
            MethodInfo methodToPatch = typeof(MirrorRendererSO).GetMethod("CreateOrUpdateMirrorCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod postfixPatch = new HarmonyMethod(typeof(BeatSaberUtilities).GetMethod(nameof(CreateOrUpdateMirrorCamera), BindingFlags.NonPublic | BindingFlags.Static));

            harmony.Patch(methodToPatch, null, postfixPatch);
        }

        private static void OnPlayerHeightChanged(float value)
        {
            playerHeightChanged?.Invoke(value);
        }

        private static void CreateOrUpdateMirrorCamera(MirrorRendererSO __instance)
        {
            Camera mirrorCamera = new Traverse(__instance).Field<Camera>("_mirrorCamera").Value;

            mirrorCamera.cullingMask |= (1 << AvatarLayers.kOnlyInThirdPerson) | (1 << AvatarLayers.kOnlyInFirstPerson) | (1 << AvatarLayers.kAlwaysVisible);
        }
    }
}
