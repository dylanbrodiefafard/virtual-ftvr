using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Biglab.Utility
{
    public class Permutation<T> : IEnumerable<T>
        // Author: Christopher Chamberlain - 2018
    {
        /// <summary>
        /// The number of items in the ordering.
        /// </summary>
        public int Count => _permuted.Length;

        /// <summary>
        /// Gets the items in their current ordering.
        /// </summary>
        public T this[int index] => _permuted[index];

        private IEnumerator<IEnumerable<T>> _enumerator;

        private readonly T[] _permuted;
        private readonly T[] _original;

        /// <summary>
        /// Creates an object of walking through all orderings of n items.
        /// </summary>
        public Permutation(IEnumerable<T> indices)
        {
            _original = indices.ToArray();
            _permuted = indices.ToArray();
        }

        public void NextPermutation()
        {
            // Reset enumeration if expired
            if (_enumerator == null || !_enumerator.MoveNext())
            {
                // TODO: Notify user that all permutations have been cycled?
                _enumerator = GetPermutations(_original);
                _enumerator.MoveNext();
            }

            var index = 0;
            if (_enumerator.Current == null)
            {
                return;
            }

            foreach (var state in _enumerator.Current)
            {
                _permuted[index] = state;
                index++;
            }
        }

        #region Static Generator

        /// <summary>
        /// Get an enumerator that walks overs all possible permutations of the given items.
        /// </summary> 
        public static IEnumerator<IEnumerable<T>> GetPermutations(IEnumerable<T> @this)
        {
            var enumerable = GetPermutations(@this.ToList());
            return enumerable.GetEnumerator();
        }

        private static IEnumerable<List<T>> GetPermutations(List<T> pattern)
        {
            var n = pattern.Count;

            // Base case, return the input in a list of lists
            if (n == 1)
            {
                yield return new List<T> {pattern[0]};
            }

            // Recursive Case, Remove each element from the input list and sequence the rest
            for (var i = 0; i < n; i++)
            {
                // Get element at i
                var element = pattern[i];

                // Remove ith element
                pattern.RemoveAt(i);

                foreach (var subPermutations in GetPermutations(pattern))
                {
                    // Insert ith element at the first of sublist
                    subPermutations.Insert(0, element);

                    // Clone list
                    yield return new List<T>(subPermutations);
                }

                // Reinsert ith element
                pattern.Insert(i, element);
            }
        }

        #endregion

        public IEnumerator<T> GetEnumerator()
            => ((IEnumerable<T>)_permuted).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<T>)_permuted).GetEnumerator();
    }
}