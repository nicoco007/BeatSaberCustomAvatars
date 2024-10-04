extern alias BeatSaberFinalIK;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BeatSaberFinalIK::RootMotion.FinalIK;
using HarmonyLib;

namespace CustomAvatar.Patches
{
    /// <summary>
    /// Patch <see cref="IKSolverVR.Spine.SolvePelvis"/> so it has the pelvis bending fix from FinalIK v2.0+.
    /// </summary>
    [HarmonyPatch(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.SolvePelvis))]
    internal static class IKSolverVR_Spine
    {
        private static readonly FieldInfo kBonesField = AccessTools.DeclaredField(typeof(IKSolverVR.BodyPart), nameof(IKSolverVR.BodyPart.bones));
        private static readonly FieldInfo kPelvisIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.pelvisIndex));
        private static readonly FieldInfo kSpineIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.spineIndex));
        private static readonly FieldInfo kChestIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.chestIndex));
        private static readonly FieldInfo kHeadIndexField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.headIndex));
        private static readonly FieldInfo kHeadPositionField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.headPosition));
        private static readonly FieldInfo kPelvisPositionWeightField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.pelvisPositionWeight));
        private static readonly MethodInfo kSolveTrigonometricMethod = AccessTools.DeclaredMethod(typeof(IKSolverVR.VirtualBone), nameof(IKSolverVR.VirtualBone.SolveTrigonometric));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new(instructions);

            // fully remove VirtualBone.SolveTrigonometric(bones, pelvisIndex, spineIndex, headIndex, headPosition, bendNormal, pelvisPositionWeight * <whatever>);
            while (codeMatcher.MatchForward(
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
                new CodeMatch(i => i.Calls(kSolveTrigonometricMethod))).IsValid)
            {
                codeMatcher.RemoveInstructions(16);
            }

            codeMatcher.Start();

            // replace pelvisIndex with spineIndex (hasChest && hasNeck)
            codeMatcher
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kSpineIndexField);

            // replace `* 0.6f` with `* 0.9f`
            codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).SetOperandAndAdvance(0.9f);

            // replace pelvisIndex with chestIndex (hasChest && hasNeck)
            codeMatcher
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kChestIndexField);

            // remove unnecessary multiplication
            codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).RemoveInstructions(2);

            // replace pelvisIndex with spineIndex (hasChest && !hasNeck)
            codeMatcher
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kSpineIndexField);

            // remove unnecessary multiplication
            codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).RemoveInstructions(2);

            // replace pelvisIndex with spineIndex (!hasChest && hasNeck)
            codeMatcher
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.LoadsField(kPelvisIndexField)))
                .Advance(1)
                .SetOperandAndAdvance(kSpineIndexField);

            // remove unnecessary multiplication
            codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Mul)).RemoveInstructions(2);

            return codeMatcher.Instructions();
        }
    }
}
