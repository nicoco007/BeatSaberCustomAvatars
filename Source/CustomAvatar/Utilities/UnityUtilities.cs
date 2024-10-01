using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class UnityUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T FirstNonNullUnityObject<T>(params T[] objects) where T : Object => objects.FirstOrDefault(o => o != null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetLocalPose(this Transform transform, Pose pose)
        {
            transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
