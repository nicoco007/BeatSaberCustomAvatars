using System;
using System.Reflection;
using HarmonyLib;

namespace CustomAvatar
{
    internal static class BeatSaberEvents
    {
        public static event Action<float> playerHeightChanged;

        public static void ApplyPatches(Harmony harmony)
        {
            HarmonyMethod prefixPatch = new HarmonyMethod(typeof(BeatSaberEvents).GetMethod(nameof(OnPlayerHeightChanged), BindingFlags.Static | BindingFlags.NonPublic));
            MethodBase playerHeightSetter = typeof(PlayerSpecificSettings).GetProperty(nameof(PlayerSpecificSettings.playerHeight), BindingFlags.Instance | BindingFlags.Public).SetMethod;
        
            harmony.Patch(playerHeightSetter, null, prefixPatch);
        }

        private static void OnPlayerHeightChanged(float value)
        {
            playerHeightChanged?.Invoke(value);
        }
    }
}
