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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Scripts;
using CustomAvatar.Utilities;
using HarmonyLib;

namespace CustomAvatar.Patches
{
    /// <summary>
    /// This patch makes the constructor of <see cref="IKSolverVR"/> instantiate our <see cref="IKSolverVR_Arm"/> in the <see cref="IKSolverVR.leftArm"/> and <see cref="IKSolverVR.rightArm"/> fields.
    /// </summary>
    [HarmonyPatch(typeof(IKSolverVR), MethodType.Constructor)]
    internal static class IKSolverVR_Constructor
    {
        private static readonly ConstructorInfo kIKSolverVRArmConstructor = typeof(IKSolverVR.Arm).GetConstructors().Single();
        private static readonly ConstructorInfo kNewIKSolverVRArmConstructor = typeof(IKSolverVR_Arm).GetConstructors().Single();

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Newobj && (ConstructorInfo)instruction.operand == kIKSolverVRArmConstructor)
                {
                    instruction.operand = kNewIKSolverVRArmConstructor;
                }

                yield return instruction;
            }
        }
    }

    /// <summary>
    /// This patch prevents locomotion from fighting against the position we set in <see cref="Avatar.AvatarIK"/> when the pelvis target exists.
    /// </summary>
    [HarmonyPatch(typeof(IKSolverVR), nameof(IKSolverVR.Solve))]
    internal static class IKSolverVR_Solve
    {
        private static readonly MethodInfo kRootBonePropertyGetter = AccessTools.DeclaredPropertyGetter(typeof(IKSolverVR), nameof(IKSolverVR.rootBone));
        private static readonly FieldInfo kVirtualBoneSolverPositionField = AccessTools.DeclaredField(typeof(IKSolverVR.VirtualBone), nameof(IKSolverVR.VirtualBone.solverPosition));
        private static readonly FieldInfo kSpineField = AccessTools.DeclaredField(typeof(IKSolverVR), nameof(IKSolverVR.spine));
        private static readonly FieldInfo kSpinePelvisTargetField = AccessTools.DeclaredField(typeof(IKSolverVR.Spine), nameof(IKSolverVR.Spine.pelvisTarget));
        private static readonly MethodInfo kUnityObjectEqualsMethod = AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "op_Equality");

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
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
}
