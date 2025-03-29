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
