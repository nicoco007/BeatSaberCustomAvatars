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

using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class BeatSaberEvents
    {
        public static event Action<float> playerHeightChanged;

        private static ILogger<BeatSaberUtilities> _logger;

        public static void ApplyPatches(Harmony harmony, IPA.Logging.Logger logger)
        {
            _logger = new IPALogger<BeatSaberUtilities>(logger);

            PatchPlayerHeightProperty(harmony);
            PatchMirrorRendererSO(harmony);
        }

        private static void PatchPlayerHeightProperty(Harmony harmony)
        {
            HarmonyMethod postfixPatch = new HarmonyMethod(typeof(BeatSaberEvents).GetMethod(nameof(OnPlayerHeightChanged), BindingFlags.NonPublic | BindingFlags.Static));

            foreach (ConstructorInfo constructor in typeof(PlayerSpecificSettings).GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                harmony.Patch(constructor, null, postfixPatch);
            }
        }

        private static void PatchMirrorRendererSO(Harmony harmony)
        {
            MethodInfo methodToPatch = typeof(MirrorRendererSO).GetMethod("CreateOrUpdateMirrorCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            HarmonyMethod postfixPatch = new HarmonyMethod(typeof(BeatSaberEvents).GetMethod(nameof(CreateOrUpdateMirrorCamera), BindingFlags.NonPublic | BindingFlags.Static));

            harmony.Patch(methodToPatch, null, postfixPatch);
        }

        private static void OnPlayerHeightChanged(PlayerSpecificSettings __instance)
        {
            float playerHeight = __instance.playerHeight;

            _logger.Info("Player height changed to " + playerHeight);

            playerHeightChanged?.Invoke(playerHeight);
        }

        private static void CreateOrUpdateMirrorCamera(MirrorRendererSO __instance)
        {
            Camera mirrorCamera = new Traverse(__instance).Field<Camera>("_mirrorCamera").Value;

            mirrorCamera.cullingMask |= AvatarLayers.kAllLayersMask;
        }
    }
}
