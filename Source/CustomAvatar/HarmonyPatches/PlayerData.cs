//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
