//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Utilities;
using HarmonyLib;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    [HarmonyPatch]
    internal class CustomIKSolverVR : IKSolverVR
    {
        public CustomIKSolverVR() : base()
        {
            spine = new CustomSpine();
            leftArm = new CustomArm();
            rightArm = new CustomArm();
        }

        private static readonly MethodInfo kRootBonePropertyGetter = AccessTools.DeclaredPropertyGetter(typeof(IKSolverVR), nameof(rootBone));
        private static readonly FieldInfo kVirtualBoneSolverPositionField = AccessTools.DeclaredField(typeof(VirtualBone), nameof(VirtualBone.solverPosition));
        private static readonly FieldInfo kSpineField = AccessTools.DeclaredField(typeof(IKSolverVR), nameof(spine));
        private static readonly FieldInfo kSpinePelvisTargetField = AccessTools.DeclaredField(typeof(Spine), nameof(Spine.pelvisTarget));
        private static readonly MethodInfo kUnityObjectEqualsMethod = AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "op_Equality");

        /// <summary>
        /// This patch prevents locomotion from fighting against the position we set in <see cref="AvatarIK"/> when the pelvis target exists.
        /// </summary>
        [HarmonyPatch(typeof(IKSolverVR), nameof(IKSolverVR.Solve))]
        [HarmonyReversePatch]
#pragma warning disable IDE0060, IDE0062, CS8321
        internal static void Solve(IKSolverVR self)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                return new CodeMatcher(instructions, ilGenerator)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.Calls(kRootBonePropertyGetter)),
                    new CodeMatch(i => i.LoadsLocal(14)),
                    new CodeMatch(i => i.StoresField(kVirtualBoneSolverPositionField)))
                .CreateLabelWithOffsets(4, out Label label)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, kSpineField),
                    new CodeInstruction(OpCodes.Ldfld, kSpinePelvisTargetField),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Call, kUnityObjectEqualsMethod),
                    new CodeInstruction(OpCodes.Brfalse_S, label))
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.Calls(kRootBonePropertyGetter)),
                    new CodeMatch(i => i.LoadsField(kVirtualBoneSolverPositionField)))
                .RemoveInstructions(3)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 14))
                .InstructionEnumeration();
            }
        }
#pragma warning restore IDE0060, IDE0062, CS8321

        [HarmonyPatch]
        internal class CustomSpine : Spine
        {
            private static readonly FieldInfo kBonesField = AccessTools.DeclaredField(typeof(BodyPart), nameof(bones));
            private static readonly FieldInfo kPelvisIndexField = AccessTools.DeclaredField(typeof(Spine), nameof(pelvisIndex));
            private static readonly FieldInfo kSpineIndexField = AccessTools.DeclaredField(typeof(Spine), nameof(spineIndex));
            private static readonly FieldInfo kChestIndexField = AccessTools.DeclaredField(typeof(Spine), nameof(chestIndex));
            private static readonly FieldInfo kHeadIndexField = AccessTools.DeclaredField(typeof(Spine), nameof(headIndex));
            private static readonly FieldInfo kHeadPositionField = AccessTools.DeclaredField(typeof(Spine), nameof(headPosition));
            private static readonly FieldInfo kPelvisPositionWeightField = AccessTools.DeclaredField(typeof(Spine), nameof(pelvisPositionWeight));
            private static readonly MethodInfo kSolveTrigonometricMethod = AccessTools.DeclaredMethod(typeof(VirtualBone), nameof(VirtualBone.SolveTrigonometric));
            private static readonly MethodInfo kNewSolveTrigonometricMethod = AccessTools.DeclaredMethod(typeof(CustomSpine), nameof(SolveTrigonometric));

            /// <summary>
            /// Patch <see cref="IKSolverVR.Spine.SolvePelvis"/> so it has the pelvis bending fix from FinalIK v2.0+.
            /// </summary>
            [HarmonyPatch(typeof(Spine), nameof(Spine.SolvePelvis))]
            [HarmonyReversePatch]
#pragma warning disable IDE0060, IDE0062, CS8321
            internal static void SolvePelvis(CustomSpine self)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
            }
#pragma warning restore IDE0060, IDE0062, CS8321

            private static readonly MethodInfo kMathfSqrtMethod = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Sqrt));
            private static readonly MethodInfo kMathfClampMethod = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Clamp), [typeof(float), typeof(float), typeof(float)]);
            private static readonly MethodInfo kVector3DistanceMethod = AccessTools.DeclaredMethod(typeof(Vector3), nameof(Vector3.Distance));
            private static readonly FieldInfo kVirtualBoneSolverPositionField = AccessTools.DeclaredField(typeof(VirtualBone), nameof(VirtualBone.solverPosition));

            /// <summary>
            /// A patched version of <see cref="IKSolverVR.VirtualBone.SolveTrigonometric"/> where the
            /// target distance between the bones is clamped to their resting distance. This prevents the bones from being straightened more than they are in the rest pose.
            /// </summary>
            [HarmonyPatch(typeof(VirtualBone), nameof(VirtualBone.SolveTrigonometric))]
            [HarmonyReversePatch]
#pragma warning disable IDE0060, IDE0062, CS8321
            private static void SolveTrigonometric(VirtualBone[] bones, int first, int second, int third, Vector3 targetPosition, Vector3 bendNormal, float weight)
            {
                // Replace `float directionMag = Mathf.Sqrt(sqrMagnitude);`
                // with `float directionMag = Mathf.Clamp(Mathf.Sqrt(sqrMagnitude), 0, Vector3.Distance(bones[third].solverPosition, bones[first].solverPosition));`
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
            }
#pragma warning restore IDE0060, IDE0062, CS8321
        }

        [HarmonyPatch]
        internal class CustomArm : Arm
        {
            private static readonly MethodInfo kFromMethod = AccessTools.DeclaredMethod(typeof(CustomArm), nameof(From));
            private static readonly MethodInfo kToMethod = AccessTools.DeclaredMethod(typeof(CustomArm), nameof(To));

            // The extra 15 degrees here comes from the pitchOffsetAngle expecting the vector from the shoulder bone to the upper arm bone to be 15 degrees under the horizon.
            // We could instead adjust the pitch math to compensate but it seems to make the shoulder rotate in a slightly different way when compared to the original method.
            private static Vector3 From(Arm self, bool isLeft) => Quaternion.AngleAxis(isLeft ? 15 : -15, self.chestForward) * self.chestRotation * (isLeft ? Vector3.left : Vector3.right);

            // The return value of this method is `workingSpace` without `yOA` added to the angle.
            private static Quaternion To(Arm self, bool isLeft) => Quaternion.AngleAxis(isLeft ? -90f : 90f, self.chestUp) * self.chestRotation;

            /// <summary>
            /// A patched version of <see cref="IKSolverVR.Arm.Solve"/> that assumes the shoulder is initially at its relaxed position instead of forcing the relaxed position that is flat on the XY plane and 15 degrees under the horizon.
            /// </summary>
            /// <param name="arm">The arm to solve (<see langword="this" />).</param>
            /// <param name="isLeft">Whether or not this is the left arm.</param>
            [HarmonyPatch(typeof(Arm), nameof(Arm.Solve))]
            [HarmonyReversePatch]
#pragma warning disable IDE0060, IDE0062, CS8321
            internal static void Solve(Arm arm, bool isLeft)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    return new CodeMatcher(instructions)
                        // Add `+ yOA` after `Mathf.Atan2(sDirWorking.x, sDirWorking.z) * Mathf.Rad2Deg`
                        .MatchForward(true,
                            new CodeMatch(i => i.Equals(OpCodes.Ldc_R4, Mathf.Rad2Deg)),
                            new CodeMatch(OpCodes.Mul))
                        .ThrowIfInvalid("Initial yaw calculation not found")
                        .Advance(1)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_2),
                            new CodeInstruction(OpCodes.Add))
                        // Remove `yaw -= yOA`
                        .MatchForward(false,
                            new CodeMatch(i => i.LoadsLocal(5)),
                            new CodeMatch(OpCodes.Ldloc_2),
                            new CodeMatch(OpCodes.Sub),
                            new CodeMatch(i => i.StoresLocal(5)))
                        .ThrowIfInvalid("`yaw -= yOA` not found")
                        .RemoveInstructions(4)
                        // Remove `- yOA` from `yawLimitMin` in call to `DamperValue`
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldloc_2),
                            new CodeMatch(OpCodes.Sub))
                        .RemoveInstructions(2)
                        // Remove `- yOA` from `yawLimitMax` in call to `DamperValue`
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldloc_2),
                            new CodeMatch(OpCodes.Sub))
                        .RemoveInstructions(2)
                        // Replace `shoulder.solverRotation * shoulder.axis` with `From(this, isLeft)`
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldarg_0),
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Ldfld),
                            new CodeMatch(OpCodes.Ldarg_0),
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Ldfld),
                            new CodeMatch(OpCodes.Call))
                        .ThrowIfInvalid("From calculation not found")
                        .Advance(1)
                        .SetAndAdvance(OpCodes.Ldarg_1, null)
                        .SetAndAdvance(OpCodes.Call, kFromMethod)
                        .RemoveInstructions(4)
                        // Replace `workingSpace` with `To(this, isLeft)`
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldloc_3),
                            new CodeMatch(i => i.LoadsLocal(5)),
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Call))
                        .ThrowIfInvalid("To calculation not found")
                        .SetAndAdvance(OpCodes.Ldarg_0, null)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldarg_1),
                            new CodeInstruction(OpCodes.Call, kToMethod))
                        .InstructionEnumeration();
                }
            }
#pragma warning restore IDE0060, IDE0062, CS8321
        }
    }
}
