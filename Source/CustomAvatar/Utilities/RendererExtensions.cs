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

using IPA.Utilities;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class RendererExtensions
    {
        // found this property through UnityExplorer - hopefully it doesn't disappear in future versions of Unity
        private static readonly PropertyAccessor<Renderer, Transform>.Setter kStaticBatchRootTransformSetter = PropertyAccessor<Renderer, Transform>.GetSetter("staticBatchRootTransform");

        internal static void SetStaticBatchRootTransform(this Renderer renderer, Transform transform)
        {
            kStaticBatchRootTransformSetter(ref renderer, transform);
        }
    }
}
