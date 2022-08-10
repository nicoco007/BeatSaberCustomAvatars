using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class MaterialExtensions
    {
        public static bool HasKeyword(this Material material, string keyword)
        {
            return material.shaderKeywords?.IndexOf(keyword) >= 0;
        }
    }
}
