//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections.Generic;
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
        internal static Pose GetPose(this Transform transform)
        {
            transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            return new Pose(position, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Pose GetLocalPose(this Transform transform)
        {
            transform.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
            return new Pose(position, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetPose(this Transform transform, Pose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetLocalPose(this Transform transform, Pose pose)
        {
            transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>
        /// Transforms <paramref name="pose"/> from world space to this instance's local space.
        /// </summary>
        /// <remarks>
        /// Similar to <see cref="Transform.InverseTransformPoint(Vector3)"/>.
        /// </remarks>
        /// <param name="self"></param>
        /// <param name="pose"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Pose InverseTransformPose(this Pose self, Pose pose)
        {
            Quaternion inverseRotation = Quaternion.Inverse(self.rotation);
            return new Pose(
                inverseRotation * (pose.position - self.position),
                inverseRotation * pose.rotation);
        }

        internal static string GetTransformPath(Component component)
        {
            if (component == null)
            {
                return "null";
            }

            List<string> parts = [];

            if (component is not Transform transform)
            {
                transform = component.transform;
            }

            Transform parent;

            while ((parent = transform.parent) != null)
            {
                parts.Add(transform.name);
                transform = parent;
            }

            parts.Add(transform.name);
            parts.Add(transform.gameObject.scene.name);

            parts.Reverse();

            return $"'{string.Join("/", parts)}'";
        }
    }
}
