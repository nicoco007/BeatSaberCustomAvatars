//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Scripts;
using CustomAvatar.Utilities;
using HarmonyLib;

namespace CustomAvatar.Patches
{
    [HarmonyPatch(typeof(IKSolverVR), MethodType.Constructor)]
    internal static class IKSolverVR_Constructor
    {
        public static void Postfix(IKSolverVR __instance)
        {
            __instance.leftArm = new IKSolverVR_Arm();
            __instance.rightArm = new IKSolverVR_Arm();
        }
    }

    [HarmonyPatch(typeof(IKSolverVR.Arm), nameof(IKSolverVR.Arm.Solve))]
    internal static class IKSolverVR_Arm_PitchAngleOffset
    {
        private static readonly FieldInfo kPitchOffsetAngleField = AccessTools.DeclaredField(typeof(IKSolverVR_Arm), nameof(IKSolverVR_Arm.shoulderPitchOffset));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                /* Quaternion.AngleAxis(isLeft ? pitchOffsetAngle : -pitchOffsetAngle, chestForward) */
                .MatchForward(false,
                    new CodeMatch(i => i.Equals(OpCodes.Ldc_R4, 30f)),
                    new CodeMatch(i => i.Branches(out Label? _)),
                    new CodeMatch(i => i.Equals(OpCodes.Ldc_R4, -30f)))
                .SetAndAdvance(OpCodes.Ldarg_0, null)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, kPitchOffsetAngleField),
                    new CodeInstruction(OpCodes.Neg))
                .Advance(1)
                .SetAndAdvance(OpCodes.Ldarg_0, null)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, kPitchOffsetAngleField))

                /* pitch -= pitchOffsetAngle */
                .MatchForward(false,
                    new CodeMatch(i => i.LoadsLocal(11)),
                    new CodeMatch(i => i.opcode == OpCodes.Ldc_R4 && (float)i.operand == -30f),
                    new CodeMatch(OpCodes.Sub),
                    new CodeMatch(i => i.SetsLocal(11)))
                .ThrowIfInvalid("`pitch -= pitchOffsetAngle` not found")
                .Advance(1)
                .SetAndAdvance(OpCodes.Ldarg_0, null)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, kPitchOffsetAngleField))

                /* DamperValue(pitch, -45f - pitchOffsetAngle, 45f - pitchOffsetAngle) */
                .MatchForward(false,
                    new CodeMatch(i => i.LoadsLocal(11)),
                    new CodeMatch(i => i.Equals(OpCodes.Ldc_R4, -15f)),
                    new CodeMatch(i => i.Equals(OpCodes.Ldc_R4, 75f)))
                .Advance(1)
                .SetOperandAndAdvance(-45f)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0, null),
                    new CodeInstruction(OpCodes.Ldfld, kPitchOffsetAngleField),
                    new CodeInstruction(OpCodes.Sub))
                .SetOperandAndAdvance(45f)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0, null),
                    new CodeInstruction(OpCodes.Ldfld, kPitchOffsetAngleField),
                    new CodeInstruction(OpCodes.Sub))
                .InstructionEnumeration();
        }
    }
}
