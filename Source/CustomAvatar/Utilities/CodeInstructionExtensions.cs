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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace CustomAvatar.Utilities
{
    internal static class CodeInstructionExtensions
    {
        private static readonly OpCode[] kLoadLocalCodes =
        {
            OpCodes.Ldloc_S,
            OpCodes.Ldloc,
        };

        private static readonly OpCode[] kStoreLocalCodes =
        {
            OpCodes.Stloc,
            OpCodes.Stloc_S,
        };

        internal static bool LoadsLocal(this CodeInstruction instruction, int index)
        {
            if (!kLoadLocalCodes.Contains(instruction.opcode))
            {
                return false;
            }

            return instruction.operand switch
            {
                LocalBuilder localBuilder => localBuilder.LocalIndex == index,
                int localIndex => index == localIndex,
                _ => throw new InvalidCastException(),
            };
        }

        internal static bool StoresLocal(this CodeInstruction instruction, int index)
        {
            if (!kStoreLocalCodes.Contains(instruction.opcode))
            {
                return false;
            }

            return instruction.operand switch
            {
                LocalBuilder localBuilder => localBuilder.LocalIndex == index,
                int localIndex => index == localIndex,
                _ => throw new InvalidCastException(),
            };
        }

        internal static bool Equals<T>(this CodeInstruction codeInstruction, OpCode opcode, T operand)
        {
            return codeInstruction.opcode == opcode && EqualityComparer<T>.Default.Equals((T)codeInstruction.operand, operand);
        }
    }
}
