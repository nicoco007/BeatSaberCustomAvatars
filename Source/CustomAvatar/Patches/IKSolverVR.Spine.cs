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

        /// <remarks>
        /// This is the same as <see cref="IKSolverVR.VirtualBone.SolveTrigonometric" /> except for the additional <see cref="Mathf.Clamp(float, float, float)"/> on the <c>length</c> variable.
        /// </remarks>
        public static void SolveTrigonometric(IKSolverVR.VirtualBone[] bones, int first, int second, int third, Vector3 targetPosition, Vector3 bendNormal, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            targetPosition = Vector3.Lerp(bones[third].solverPosition, targetPosition, weight);

            Vector3 dir = targetPosition - bones[first].solverPosition;

            float sqrMag = dir.sqrMagnitude;
            if (sqrMag == 0f)
            {
                return;
            }

            // This is the only change from the original method: clamp the distance to the distance between the
            // first and last bones so the spine doesn't get overextended. This isn't perfect because the FABRIK pass
            // that runs before this can straighten the spine slightly but it makes it significantly less jarring.
            float length = Mathf.Clamp(Mathf.Sqrt(sqrMag), 0, Vector3.Distance(bones[third].solverPosition, bones[first].solverPosition));

            float sqrMag1 = (bones[second].solverPosition - bones[first].solverPosition).sqrMagnitude;
            float sqrMag2 = (bones[third].solverPosition - bones[second].solverPosition).sqrMagnitude;

            // Get the general world space bending direction
            var bendDir = Vector3.Cross(dir, bendNormal);

            Vector3 toBendPoint = IKSolverVR.VirtualBone.GetDirectionToBendPoint(dir, length, bendDir, sqrMag1, sqrMag2);

            var q1 = Quaternion.FromToRotation(bones[second].solverPosition - bones[first].solverPosition, toBendPoint);
            if (weight < 1f)
            {
                q1 = Quaternion.Lerp(Quaternion.identity, q1, weight);
            }

            IKSolverVR.VirtualBone.RotateAroundPoint(bones, first, bones[first].solverPosition, q1);

            var q2 = Quaternion.FromToRotation(bones[third].solverPosition - bones[second].solverPosition, targetPosition - bones[second].solverPosition);
            if (weight < 1f)
            {
                q2 = Quaternion.Lerp(Quaternion.identity, q2, weight);
            }

            IKSolverVR.VirtualBone.RotateAroundPoint(bones, second, bones[second].solverPosition, q2);
        }
    }
}
