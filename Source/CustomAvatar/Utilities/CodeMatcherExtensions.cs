using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CustomAvatar.Utilities
{
    internal static class CodeMatcherExtensions
    {
        internal static CodeMatcher RemoveLabels(this CodeMatcher matcher, out List<Label> labels)
        {
            CodeInstruction instruction = matcher.Instruction;
            if (instruction.labels.Count == 0)
            {
                throw new InvalidOperationException("Instruction has no labels to remove.");
            }
            labels = instruction.labels;
            instruction.labels = [];
            return matcher;
        }
    }
}
