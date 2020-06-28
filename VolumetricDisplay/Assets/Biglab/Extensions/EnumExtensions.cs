using System;
using Biglab.Collections;

namespace Biglab.Extensions
{
    public static class EnumExtensions
    {
        public static BiDictionary<int, T> GetIndexMapping<T>() where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException($"{nameof(T)} muse be an enumerated type");
            }

            var mapping = new BiDictionary<int, T>();
            var index = 0;
            foreach(T value in Enum.GetValues(typeof(T)))
            {
                mapping[value] = index;
                mapping[index] = value;
                index++;
            }

            return mapping;
        }
    }
}
