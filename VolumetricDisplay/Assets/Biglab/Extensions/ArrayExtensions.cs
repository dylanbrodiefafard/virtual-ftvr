using System;
using System.Collections.Generic;

namespace Biglab.Extensions
{
    public static class ArrayExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> @this, Predicate<T> predicate)
        {
            for (var i = 0; i < @this.Count; i++)
            {
                if (predicate(@this[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}