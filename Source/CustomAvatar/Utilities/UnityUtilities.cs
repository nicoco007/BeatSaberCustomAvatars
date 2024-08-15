using System.Linq;
using System.Runtime.CompilerServices;

namespace CustomAvatar.Utilities
{
    internal static class UnityUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T FirstNonNullUnityObject<T>(params T[] objects) where T : UnityEngine.Object => objects.FirstOrDefault(o => o != null);
    }
}
