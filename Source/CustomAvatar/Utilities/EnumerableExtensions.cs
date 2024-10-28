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
