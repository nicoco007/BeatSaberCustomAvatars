using System;
using HarmonyLib;

namespace CustomAvatar.HarmonyPatches
{
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.playerSpecificSettings), MethodType.Setter)]
    internal static class PlayerData_playerSpecificSettings
    {
        public static event Action<float> playerHeightChanged;

        public static void Postfix(PlayerData __instance)
        {
            playerHeightChanged?.Invoke(__instance.playerSpecificSettings.playerHeight);
        }
    }
}
