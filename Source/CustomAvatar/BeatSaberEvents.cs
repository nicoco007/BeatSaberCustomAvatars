using System;
using System.Reflection;
using HarmonyLib;

namespace CustomAvatar
{
    internal static class BeatSaberEvents
    {
        public static event Action<float> playerHeightChanged;

        private static Harmony _harmony;

        public static void ApplyPatches()
        {
            _harmony = new Harmony(typeof(BeatSaberEvents).FullName + "_" + Guid.NewGuid());

            HarmonyMethod prefixPatch = new HarmonyMethod(typeof(BeatSaberEvents).GetMethod(nameof(OnPlayerHeightChanged), BindingFlags.Static | BindingFlags.NonPublic));
            MethodBase playerHeightSetter = typeof(PlayerSpecificSettings).GetProperty(nameof(PlayerSpecificSettings.playerHeight), BindingFlags.Instance | BindingFlags.Public).SetMethod;
        
            _harmony.Patch(playerHeightSetter, null, prefixPatch);
        }

        private static void OnPlayerHeightChanged(float value)
        {
            //Plugin.logger.Info($"Player height set to {value} m");
            playerHeightChanged?.Invoke(value);
        }
    }
}
