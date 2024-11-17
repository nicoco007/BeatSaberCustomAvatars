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

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BeatSaberFinalIK::RootMotion.FinalIK;
using HarmonyLib;
using UnityEngine;

namespace CustomAvatar.Patches
{
    /// <summary>
    /// Patch <see cref="IKSolverVR.Spine.SolvePelvis"/> so it has the pelvis bending fix from FinalIK v2.0+.
    /// </summary>
    [HarmonyPatch(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.SolvePelvis))]
    internal static class IKSolverVR_Spine_SolvePelvis
    {
        private static readonly FieldInfo kBonesField = AccessTools.DeclaredField(typeof(IKSolverVR.BodyPart), nameof(IKSolverVR.BodyPart.bones));
        private static readonly FieldInfo kPelvisIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.pelvisIndex));
        private static readonly FieldInfo kSpineIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.spineIndex));
        private static readonly FieldInfo kChestIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.chestIndex));
        private static readonly FieldInfo kHeadIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.headIndex));
        private static readonly FieldInfo kHeadPositionField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.headPosition));
        private static readonly FieldInfo kPelvisPositionWeightField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.pelvisPositionWeight));
        private static readonly MethodInfo kSolveTrigonometricMethod = AccessTools.DeclaredMethod(typeof(IKSolverVR.VirtualBone), nameof(IKSolverVR.VirtualBone.SolveTrigonometric));
        private static readonly MethodInfo kNewSolveTrigonometricMethod = AccessTools.DeclaredMethod(typeof(IKSolverVR_Spine_SolvePelvis), nameof(SolveTrigonometric));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                // fully remove VirtualBone.SolveTrigonometric(bones, pelvisIndex, spineIndex, headIndex, headPosition, bendNormal, pelvisPositionWeight * <whatever>);
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kBonesField)),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kSpineIndexField)),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kHeadIndexField)),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kHeadPositionField)),
                    new CodeMatch(OpCodes.Ldloc_2),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisPositionWeightField)),
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Mul),
                    new CodeMatch(i => i.Calls(kSolveTrigonometricMethod)))
                .Repeat(cm => cm.RemoveInstructions(16))
                .Start()

                // replace pelvisIndex with spineIndex (hasChest && hasNeck)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kSpineIndexField)

                // replace `* 0.6f` with `* 0.9f`
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).SetOperandAndAdvance(0.9f)

                // call new method
                .MatchForward(false, new CodeMatch(i => i.Calls(kSolveTrigonometricMethod)))
                .SetOperandAndAdvance(kNewSolveTrigonometricMethod)

                // replace pelvisIndex with chestIndex (hasChest && hasNeck)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kChestIndexField)

                // remove unnecessary multiplication
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).RemoveInstructions(2)

                // call new method
                .MatchForward(false, new CodeMatch(i => i.Calls(kSolveTrigonometricMethod)))
                .SetOperandAndAdvance(kNewSolveTrigonometricMethod)

                // replace pelvisIndex with spineIndex (hasChest && !hasNeck)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kSpineIndexField)

                // remove unnecessary multiplication
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).RemoveInstructions(2)

                // call new method
                .MatchForward(false, new CodeMatch(i => i.Calls(kSolveTrigonometricMethod)))
                .SetOperandAndAdvance(kNewSolveTrigonometricMethod)

                // replace pelvisIndex with spineIndex (!hasChest && hasNeck)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kSpineIndexField)

                // remove unnecessary multiplication
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).RemoveInstructions(2)

                // call new method
                .MatchForward(false, new CodeMatch(i => i.Calls(kSolveTrigonometricMethod)))
                .SetOperandAndAdvance(kNewSolveTrigonometricMethod)

                // call new method (!hasNeck && !hasChest)
                .MatchForward(false, new CodeMatch(i => i.Calls(kSolveTrigonometricMethod)))
                .SetOperandAndAdvance(kNewSolveTrigonometricMethod)

                .Instructions();
        }

        private static readonly MethodInfo kMathfSqrtMethod = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Sqrt));
        private static readonly MethodInfo kMathfClampMethod = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Clamp), [typeof(float), typeof(float), typeof(float)]);
        private static readonly MethodInfo kVector3DistanceMethod = AccessTools.DeclaredMethod(typeof(Vector3), nameof(Vector3.Distance));
        private static readonly FieldInfo kVirtualBoneSolverPositionField = AccessTools.DeclaredField(typeof(IKSolverVR.VirtualBone), nameof(IKSolverVR.VirtualBone.solverPosition));

        /// <summary>
        /// A patched version of <see cref="IKSolverVR.VirtualBone.SolveTrigonometric"/> where the
        /// target distance between the bones is clamped to their resting distance. This prevents the bones from being straightened more than they are in the rest pose.
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(IKSolverVR.VirtualBone), nameof(IKSolverVR.VirtualBone.SolveTrigonometric))]
        private static void SolveTrigonometric(IKSolverVR.VirtualBone[] bones, int first, int second, int third, Vector3 targetPosition, Vector3 bendNormal, float weight)
        {
            // Replace `float directionMag = Mathf.Sqrt(sqrMagnitude);`
            // with `float directionMag = Mathf.Clamp(Mathf.Sqrt(sqrMagnitude), 0, Vector3.Distance(bones[third].solverPosition, bones[first].solverPosition));`
#pragma warning disable IDE0062, CS8321
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(
                        true,
                        new CodeMatch(i => i.Calls(kMathfSqrtMethod)))
                    .Advance(1)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldc_R4, 0f),
                        // bones[first].solverPosition
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldelem_Ref),
                        new CodeInstruction(OpCodes.Ldfld, kVirtualBoneSolverPositionField),
                        // bones[third].solverPosition
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Ldelem_Ref),
                        new CodeInstruction(OpCodes.Ldfld, kVirtualBoneSolverPositionField),
                        // Vector3.Distance(bones[third].solverPosition, bones[first].solverPosition)
                        new CodeInstruction(OpCodes.Call, kVector3DistanceMethod),
                        // Mathf.Clamp(Mathf.Sqrt(sqrMag), 0, Vector3.Distance(bones[third].solverPosition, bones[first].solverPosition));
                        new CodeInstruction(OpCodes.Call, kMathfClampMethod))
                    .InstructionEnumeration();
            }
#pragma warning restore IDE0062, CS8321
        }
    }
}
