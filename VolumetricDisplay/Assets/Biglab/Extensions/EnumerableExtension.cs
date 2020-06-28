using System;
using System.Collections.Generic;
using System.Linq;

namespace Biglab.Extensions
{
    public static class EnumerableExtension
    {
        /// <summary>
        /// Computes the weighted average of the elements in the collection.
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="records">The collection of elements.</param>
        /// <param name="value">Function that returns the value of the element.</param>
        /// <param name="weight">Function that returns the weight of the element.</param>
        /// <returns></returns>
        public static float WeightedAverage<T>(this List<T> records, Func<T, float> value,
            Func<T, float> weight)
        {
            var weightedValueSum = records.Sum(x => value(x) * weight(x));
            var weightSum = records.Sum(weight);

            if (weightSum > 0)
            {
                return weightedValueSum / weightSum;
            }

            throw new DivideByZeroException("Sum of weights must be greater than 0.");
        }

        /// <summary>
        ///   Returns all combinations of a chosen amount of selected elements in the sequence.
        ///   This does not allow repeating elements.
        /// </summary>
        /// <typeparam name = "T">The type of the elements of the input sequence.</typeparam>
        /// <param name = "this">The source for this extension method.</param>
        /// <param name = "select">The amount of elements to select for every combination.</param>
        /// <returns>All combinations of a chosen amount of selected elements in the sequence.</returns>
        public static IEnumerable<IEnumerable<T>> ToCombination<T>(this IEnumerable<T> @this, int select)
            => @this.ToSubset(select, false);

        /// <summary>
        ///   Returns permutation of a chosen amount of selected elements in the sequence. 
        ///   This will allow repeating elements.
        /// </summary>
        /// <typeparam name = "T">The type of the elements of the input sequence.</typeparam>
        /// <param name = "this">The source for this extension method.</param>
        /// <param name = "select">The amount of elements to select for every combination.</param>
        /// <returns>All combinations of a chosen amount of selected elements in the sequence.</returns>
        public static IEnumerable<IEnumerable<T>> ToPermutation<T>(this IEnumerable<T> @this, int select)
            => @this.ToSubset(select, true);

        private static IEnumerable<IEnumerable<T>> ToSubset<T>(this IEnumerable<T> @this, int select, bool repetition)
        // Source: http://www.extensionmethod.net/1973/csharp/ienumerable-t/combinations
        {
            if (select == 0) { return new[] { new T[0] }; }

            return @this.SelectMany(
                (element, index) => @this
                    .Skip(repetition ? index : index + 1)
                    .ToSubset(select - 1, repetition)
                    .Select(c => new[] { element }.Concat(c)));
        }

        // Source: https://stackoverflow.com/a/36656460
        // TODO: add seed setting
        private static readonly Random _randomInstance = new Random();

        public static T RandomElement<T>(this IList<T> list)
            => list[_randomInstance.Next(list.Count)];
    }
}