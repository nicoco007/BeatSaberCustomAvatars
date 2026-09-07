//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

extern alias BeatSaberFinalIK;

using System.Collections.Generic;
using System.Reflection.Emit;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Avatar;
using CustomAvatar.Utilities;
using HarmonyLib;
using UnityEngine;

namespace CustomAvatar.Patches
{
    [HarmonyPatch(typeof(IKSolverVR), nameof(IKSolverVR.Solve))]
    internal static class IKSolverVR_Solve
    {
        public static bool Prefix(IKSolverVR __instance)
        {
            if (__instance is not CustomIKSolverVR solver)
            {
                return true;
            }

            CustomIKSolverVR.Solve(solver);
            return false;
        }
    }

    [HarmonyPatch(typeof(IKSolverVR.Spine))]
    internal static class IKSolverVR_Spine_SolvePelvis
    {
        [HarmonyPatch(nameof(IKSolverVR.Spine.Solve))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Solve_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label? maxRootAngle = null;
            Label? rootHeadingOffset = null;

            return new CodeMatcher(instructions, generator)
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 180f), new CodeMatch(i => i.Branches(out maxRootAngle)))
                .ThrowIfInvalid("`maxRootAngle > 180` branch not found")
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 0f), new CodeMatch(i => i.Branches(out rootHeadingOffset)))
                .ThrowIfInvalid("`rootHeadingOffset != 0` branch not found")
                .MatchForward(false, new CodeMatch(OpCodes.Ldarg_1), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Quaternion), nameof(Quaternion.Inverse))))
                .RemoveLabels(out List<Label> labels)
                .ThrowIfInvalid("rootHeadingOffset result not found")
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Isinst, typeof(CustomIKSolverVR.CustomSpine)),
                    new CodeInstruction(OpCodes.Brfalse, rootHeadingOffset),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CustomIKSolverVR.CustomSpine), nameof(CustomIKSolverVR.CustomSpine.RotateRoot))),
                    new CodeInstruction(OpCodes.Br, maxRootAngle))
                .AddLabels(labels)
                .InstructionEnumeration();
        }

        [HarmonyPatch(nameof(IKSolverVR.Spine.SolvePelvis))]
        [HarmonyPrefix]
        public static bool SolvePelvis_Prefix(IKSolverVR.Spine __instance)
        {
            if (__instance is not CustomIKSolverVR.CustomSpine spine)
            {
                return true;
            }

            CustomIKSolverVR.CustomSpine.SolvePelvis(spine);
            return false;
        }
    }

    [HarmonyPatch(typeof(IKSolverVR.Arm), nameof(IKSolverVR.Arm.Solve))]
    internal static class IKSolverVR_Arm_Solve
    {
        public static bool Prefix(IKSolverVR.Arm __instance, bool isLeft)
        {
            if (__instance is not CustomIKSolverVR.CustomArm arm)
            {
                return true;
            }

            CustomIKSolverVR.CustomArm.Solve(arm, isLeft);
            return false;
        }
    }

    [HarmonyPatch(typeof(IKSolverVR.Locomotion))]
    internal static class IKSolverVR_Locomotion
    {
        [HarmonyPatch(nameof(IKSolverVR.Locomotion.centerOfMass), MethodType.Setter)]
        [HarmonyPostfix]
        public static void set_centerOfMass(IKSolverVR.Locomotion __instance, Vector3 value)
        {
            if (__instance is not CustomIKSolverVR.CustomLocomotion locomotion)
            {
                return;
            }

            if (locomotion.firstCenterOfMassCalculation)
            {
                locomotion.lastComPosition = value;
            }
        }

        [HarmonyPatch(nameof(IKSolverVR.Locomotion.Solve))]
        [HarmonyPostfix]
        public static void Solve(IKSolverVR.Locomotion __instance)
        {
            if (__instance is not CustomIKSolverVR.CustomLocomotion locomotion)
            {
                return;
            }

            locomotion.firstCenterOfMassCalculation = false;
        }
    }
}
