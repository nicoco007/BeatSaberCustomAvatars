using CustomAvatar.Avatar;
using HarmonyLib;
using UnityEngine;

namespace CustomAvatar.HarmonyPatches
{
    [HarmonyPatch(typeof(MirrorRendererSO), "CreateOrUpdateMirrorCamera")]
    internal static class MirrorRendererSO_CreateOrUpdateMirrorCamera
    {
        public static void Postfix(Camera ____mirrorCamera)
        {
            ____mirrorCamera.cullingMask |= AvatarLayers.kAllLayersMask;
        }
    }
}
