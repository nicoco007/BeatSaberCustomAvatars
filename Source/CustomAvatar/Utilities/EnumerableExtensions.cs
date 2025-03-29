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

using System;
using System.Collections.Generic;

namespace CustomAvatar.Utilities
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<ValueTuple<TFirst, TSecond>> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            using IEnumerator<TFirst> e1 = first.GetEnumerator();
            using IEnumerator<TSecond> e2 = second.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext())
            {
                yield return (e1.Current, e2.Current);
            }
        }
    }
}
