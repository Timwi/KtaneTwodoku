using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Twodoku;

public static class Ut
{
    /// <summary>
    ///     Executes the specified function with the specified argument.</summary>
    /// <typeparam name="TSource">
    ///     Type of the argument to the function.</typeparam>
    /// <typeparam name="TResult">
    ///     Type of the result of the function.</typeparam>
    /// <param name="source">
    ///     The argument to the function.</param>
    /// <param name="func">
    ///     The function to execute.</param>
    /// <returns>
    ///     The result of the function.</returns>
    public static TResult Apply<TSource, TResult>(this TSource source, Func<TSource, TResult> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));
        return func(source);
    }

    /// <summary>Returns a new array with the <paramref name="value"/> appended to the end.</summary>
    public static T[] Append<T>(this T[] array, T value)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        return Insert(array, array.Length, value);
    }

    /// <summary>
    ///     Similar to <see cref="string.Insert(int, string)"/>, but for arrays. Returns a new array with the <paramref
    ///     name="values"/> inserted starting from the specified <paramref name="startIndex"/>.</summary>
    /// <remarks>
    ///     Returns a new copy of the array even if <paramref name="values"/> is empty.</remarks>
    public static T[] Insert<T>(this T[] array, int startIndex, params T[] values)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (startIndex < 0 || startIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be between 0 and the size of the input array.");
        T[] result = new T[array.Length + values.Length];
        Array.Copy(array, 0, result, 0, startIndex);
        Array.Copy(values, 0, result, startIndex, values.Length);
        Array.Copy(array, startIndex, result, startIndex + values.Length, array.Length - startIndex);
        return result;
    }

    /// <summary>
    ///     Instantiates a fully-initialized array with the specified dimensions.</summary>
    /// <param name="size">
    ///     Size of the first dimension.</param>
    /// <param name="initialiser">
    ///     Function to initialise the value of every element.</param>
    /// <typeparam name="T">
    ///     Type of the array element.</typeparam>
    public static T[] NewArray<T>(int size, Func<int, T> initialiser)
    {
        if (initialiser == null)
            throw new ArgumentNullException(nameof(initialiser));
        var result = new T[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = initialiser(i);
        }
        return result;
    }

    /// <summary>
    ///     Returns a random element from the specified collection.</summary>
    /// <typeparam name="T">
    ///     The type of the elements in the collection.</typeparam>
    /// <param name="src">
    ///     The collection to pick from.</param>
    /// <param name="rnd">
    ///     Optionally, a random number generator to use.</param>
    /// <returns>
    ///     The element randomly picked.</returns>
    /// <remarks>
    ///     This method enumerates the entire input sequence into an array.</remarks>
    public static T PickRandom<T>(this IEnumerable<T> src, Random rnd)
    {
        if (src == null)
            throw new ArgumentNullException(nameof(src));
        var list = (src as IList<T>) ?? src.ToArray();
        if (list.Count == 0)
            throw new InvalidOperationException("Cannot pick an element from an empty set.");
        return list[rnd.Next(list.Count)];
    }

    /// <summary>
    ///     Brings the elements of the given list into a random order.</summary>
    /// <typeparam name="T">
    ///     Type of the list.</typeparam>
    /// <param name="list">
    ///     List to shuffle.</param>
    /// <param name="rnd">
    ///     Random number generator, or null to use <see cref="Rnd"/>.</param>
    /// <returns>
    ///     The list operated on.</returns>
    public static T Shuffle<T>(this T list, Random rnd) where T : IList
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        for (int j = list.Count; j >= 1; j--)
        {
            int item = rnd.Next(j);
            if (item < j - 1)
                (list[j - 1], list[item]) = (list[item], list[j - 1]);
        }
        return list;
    }

    /// <summary>
    ///     Brings the elements of the given list into a random order.</summary>
    /// <typeparam name="T">
    ///     Type of the list.</typeparam>
    /// <param name="list">
    ///     List to shuffle.</param>
    /// <param name="rnd">
    ///     Random number generator, or null to use <see cref="Rnd"/>.</param>
    /// <returns>
    ///     The list operated on.</returns>
    public static T Shuffle<T>(this T list) where T : IList
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        for (int j = list.Count; j >= 1; j--)
        {
            int item = UnityEngine.Random.Range(0, j);
            if (item < j - 1)
                (list[j - 1], list[item]) = (list[item], list[j - 1]);
        }
        return list;
    }

    /// <summary>
    ///     Given a set of values and a function that returns true when given this set, will efficiently remove items from
    ///     this set which are not essential for making the function return true. The relative order of items is preserved.
    ///     This method cannot generally guarantee that the result is optimal, but for some types of functions the result will
    ///     be guaranteed optimal.</summary>
    /// <typeparam name="T">
    ///     Type of the values in the set.</typeparam>
    /// <param name="items">
    ///     The set of items to reduce.</param>
    /// <param name="test">
    ///     The function that examines the set. Must always return the same value for the same set.</param>
    /// <param name="breadthFirst">
    ///     A value selecting a breadth-first or a depth-first approach. Depth-first is best at quickly locating a single
    ///     value which will be present in the final required set. Breadth-first is best at quickly placing a lower bound on
    ///     the total number of individual items in the required set.</param>
    /// <param name="skipConsistencyTest">
    ///     When the function is particularly slow, you might want to set this to true to disable calls which are not required
    ///     to reduce the set and are only there to ensure that the function behaves consistently.</param>
    /// <returns>
    ///     A hopefully smaller set of values that still causes the function to return true.</returns>
    public static IEnumerable<T> ReduceRequiredSet<T>(IEnumerable<T> items, Func<ReduceRequiredSetState<T>, bool> test, bool breadthFirst = false, bool skipConsistencyTest = false)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));
        var itemsList = (items as IList<T>) ?? items.ToList();
        if (itemsList.Count == 0)
            throw new ArgumentException("The set of items for ReduceRequiredSet can’t be empty.", nameof(items));
        if (test == null)
            throw new ArgumentNullException(nameof(test));

        var state = new ReduceRequiredSetStateInternal<T>(itemsList);

        if (!skipConsistencyTest)
            if (!test(state))
                throw new Exception("The function does not return true for the original set.");

        while (state.AnyPartitions)
        {
            if (!skipConsistencyTest)
                if (!test(state))
                    throw new Exception("The function is not consistently returning the same value for the same set, or there is an internal error in this algorithm.");

            var rangeToSplit = breadthFirst ? state.LargestRange : state.SmallestRange;
            int mid = (rangeToSplit.from + rangeToSplit.to) / 2;
            var split1 = (rangeToSplit.from, to: mid);
            var split2 = (from: mid + 1, rangeToSplit.to);

            state.ApplyTemporarySplit(rangeToSplit, split1);
            if (test(state))
            {
                state.SolidifyTemporarySplit();
                continue;
            }
            state.ApplyTemporarySplit(rangeToSplit, split2);
            if (test(state))
            {
                state.SolidifyTemporarySplit();
                continue;
            }
            state.ResetTemporarySplit();
            state.RemoveRange(rangeToSplit);
            state.AddRange(split1);
            state.AddRange(split2);
        }

        state.ResetTemporarySplit();
        return state.SetToTest;
    }

    /// <summary>Encapsulates the state of the <see cref="ReduceRequiredSet"/> algorithm and exposes statistics about it.</summary>
    public abstract class ReduceRequiredSetState<T>
    {
        /// <summary>Internal; do not use.</summary>
        protected List<(int from, int to)> Ranges;
        /// <summary>Internal; do not use.</summary>
        protected IList<T> Items;
        /// <summary>Internal; do not use.</summary>
        protected (int from, int to)? ExcludedRange, IncludedRange;

        /// <summary>
        ///     Enumerates every item that is known to be in the final required set. "Definitely" doesn't mean that there
        ///     exists no subset resulting in "true" without these members. Rather, it means that the algorithm will
        ///     definitely return these values, and maybe some others too.</summary>
        public IEnumerable<T> DefinitelyRequired { get { return Ranges.Where(r => r.from == r.to).Select(r => Items[r.from]); } }
        /// <summary>
        ///     Gets the current number of partitions containing uncertain items. The more of these, the slower the algorithm
        ///     will converge from here onwards.</summary>
        public int PartitionsCount { get { return Ranges.Count - Ranges.Count(r => r.from == r.to); } }
        /// <summary>
        ///     Gets the number of items in the smallest partition. This is the value that is halved upon a successful
        ///     depth-first iteration.</summary>
        public int SmallestPartitionSize { get { return Ranges.Where(r => r.from != r.to).Min(r => r.to - r.from + 1); } }
        /// <summary>
        ///     Gets the number of items in the largest partition. This is the value that is halved upon a successful
        ///     breadth-first iteration.</summary>
        public int LargestPartitionSize { get { return Ranges.Max(r => r.to - r.from + 1); } }
        /// <summary>Gets the total number of items about which the algorithm is currently undecided.</summary>
        public int ItemsRemaining { get { return Ranges.Where(r => r.from != r.to).Sum(r => r.to - r.from + 1); } }

        /// <summary>Gets the set of items for which the function should be evaluated in the current step.</summary>
        public IEnumerable<T> SetToTest
        {
            get
            {
                var ranges = Ranges.AsEnumerable();
                if (ExcludedRange != null)
                    ranges = ranges.Where(r => !r.Equals(ExcludedRange.Value));
                if (IncludedRange != null)
                    ranges = ranges.Concat([IncludedRange.Value]);
                return ranges
                    .SelectMany(range => Enumerable.Range(range.from, range.to - range.from + 1))
                    .OrderBy(i => i)
                    .Select(i => Items[i]);
            }
        }
    }

    internal sealed class ReduceRequiredSetStateInternal<T> : ReduceRequiredSetState<T>
    {
        public ReduceRequiredSetStateInternal(IList<T> items)
        {
            Items = items;
            Ranges = [(0, Items.Count - 1)];
        }

        public bool AnyPartitions { get { return Ranges.Any(r => r.from != r.to); } }
        public (int from, int to) LargestRange { get { return Ranges.MaxElement(t => t.to - t.from); } }
        public (int from, int to) SmallestRange { get { return Ranges.Where(r => r.from != r.to).MinElement(t => t.to - t.from); } }

        public void AddRange((int from, int to) range) { Ranges.Add(range); }
        public void RemoveRange((int from, int to) range) { if (!Ranges.Remove(range)) throw new InvalidOperationException("Ut.ReduceRequiredSet has a bug. Code: 826432"); }

        public void ResetTemporarySplit()
        {
            ExcludedRange = IncludedRange = null;
        }
        public void ApplyTemporarySplit((int from, int to) rangeToSplit, (int from, int to) splitRange)
        {
            ExcludedRange = rangeToSplit;
            IncludedRange = splitRange;
        }

        public void SolidifyTemporarySplit()
        {
            RemoveRange(ExcludedRange.Value);
            AddRange(IncludedRange.Value);
            ResetTemporarySplit();
        }
    }

    /// <summary>
    ///     Returns the index of the first element in this <paramref name="source"/> that is equal to the specified <paramref
    ///     name="element"/> as determined by the specified <paramref name="comparer"/>. If no such elements are found,
    ///     returns <c>-1</c>.</summary>
    public static int IndexOf<T>(this IEnumerable<T> source, T element, IEqualityComparer<T> comparer = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        comparer ??= EqualityComparer<T>.Default;
        var index = 0;
        foreach (var v in source)
        {
            if (comparer.Equals(v, element))
                return index;
            index++;
        }
        return -1;
    }

    /// <summary>
    ///     Returns the minimum resulting value in a sequence, or a default value if the sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the minimum value of.</param>
    /// <param name="default">
    ///     A default value to return in case the sequence is empty.</param>
    /// <returns>
    ///     The minimum value in the sequence, or the specified default value if the sequence is empty.</returns>
    public static TSource MinOrDefault<TSource>(this IEnumerable<TSource> source, TSource @default = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        var (result, found) = minMax(source, min: true);
        return found ? result : @default;
    }

    /// <summary>
    ///     Invokes a selector on each element of a collection and returns the minimum resulting value, or a default value if
    ///     the sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">
    ///     A transform function to apply to each element.</param>
    /// <param name="default">
    ///     A default value to return in case the sequence is empty.</param>
    /// <returns>
    ///     The minimum value in the sequence, or the specified default value if the sequence is empty.</returns>
    public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult @default = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        var (result, found) = minMax(source.Select(selector), min: true);
        return found ? result : @default;
    }

    /// <summary>
    ///     Returns the maximum resulting value in a sequence, or a default value if the sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the maximum value of.</param>
    /// <param name="default">
    ///     A default value to return in case the sequence is empty.</param>
    /// <returns>
    ///     The maximum value in the sequence, or the specified default value if the sequence is empty.</returns>
    public static TSource MaxOrDefault<TSource>(this IEnumerable<TSource> source, TSource @default = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        var (result, found) = minMax(source, min: false);
        return found ? result : @default;
    }

    /// <summary>
    ///     Invokes a selector on each element of a collection and returns the maximum resulting value, or a default value if
    ///     the sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">
    ///     A transform function to apply to each element.</param>
    /// <param name="default">
    ///     A default value to return in case the sequence is empty.</param>
    /// <returns>
    ///     The maximum value in the sequence, or the specified default value if the sequence is empty.</returns>
    public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult @default = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        var (result, found) = minMax(source.Select(selector), min: false);
        return found ? result : @default;
    }

    /// <summary>
    ///     Returns the minimum resulting value in a sequence, or <c>null</c> if the sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the minimum value of.</param>
    /// <returns>
    ///     The minimum value in the sequence, or <c>null</c> if the sequence is empty.</returns>
    public static TSource? MinOrNull<TSource>(this IEnumerable<TSource> source) where TSource : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        var (result, found) = minMax(source, min: true);
        return found ? result : null;
    }

    /// <summary>
    ///     Invokes a selector on each element of a collection and returns the minimum resulting value, or <c>null</c> if the
    ///     sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">
    ///     A transform function to apply to each element.</param>
    /// <returns>
    ///     The minimum value in the sequence, or <c>null</c> if the sequence is empty.</returns>
    public static TResult? MinOrNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        var (result, found) = minMax(source.Select(selector), min: true);
        return found ? result : null;
    }

    /// <summary>
    ///     Returns the maximum resulting value in a sequence, or <c>null</c> if the sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the maximum value of.</param>
    /// <returns>
    ///     The maximum value in the sequence, or <c>null</c> if the sequence is empty.</returns>
    public static TSource? MaxOrNull<TSource>(this IEnumerable<TSource> source) where TSource : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        var (result, found) = minMax(source, min: false);
        return found ? result : null;
    }

    /// <summary>
    ///     Invokes a selector on each element of a collection and returns the maximum resulting value, or <c>null</c> if the
    ///     sequence is empty.</summary>
    /// <typeparam name="TSource">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">
    ///     A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">
    ///     A transform function to apply to each element.</param>
    /// <returns>
    ///     The maximum value in the sequence, or <c>null</c> if the sequence is empty.</returns>
    public static TResult? MaxOrNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        var (result, found) = minMax(source.Select(selector), min: false);
        return found ? result : null;
    }

    private static (T result, bool found) minMax<T>(IEnumerable<T> source, bool min)
    {
        var cmp = Comparer<T>.Default;
        var curBest = default(T);
        var haveBest = false;
        foreach (var elem in source)
        {
            if (!haveBest || (min ? cmp.Compare(elem, curBest) < 0 : cmp.Compare(elem, curBest) > 0))
            {
                curBest = elem;
                haveBest = true;
            }
        }
        return (curBest, haveBest);
    }

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the smallest value.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The input collection is empty.</exception>
    public static T MinElement<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: true).Value.minMaxElem;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the smallest value, or a
    ///     default value if the collection is empty.</summary>
    public static T MinElementOrDefault<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector, T defaultValue = default)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: false) is var (minMaxElem, _, _) ? minMaxElem : defaultValue;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the smallest value, or
    ///     <c>null</c> if the collection is empty.</summary>
    public static T? MinElementOrNull<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where T : struct
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: false)?.minMaxElem;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the largest value.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The input collection is empty.</exception>
    public static T MaxElement<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: true).Value.minMaxElem;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the largest value, or a
    ///     default value if the collection is empty.</summary>
    public static T MaxElementOrDefault<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector, T defaultValue = default)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: false) is var (minMaxElem, _, _) ? minMaxElem : defaultValue;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the largest value, or
    ///     <c>null</c> if the collection is empty.</summary>
    public static T? MaxElementOrNull<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where T : struct
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: false)?.minMaxElem;

    /// <summary>
    ///     Returns the index of the first element from the input sequence for which the value selector returns the smallest
    ///     value.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The input collection is empty.</exception>
    public static int MinIndex<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: true).Value.minMaxIndex;

    /// <summary>
    ///     Returns the index of the first element from the input sequence for which the value selector returns the smallest
    ///     value, or <c>null</c> if the collection is empty.</summary>
    public static int? MinIndexOrNull<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: false)?.minMaxIndex;

    /// <summary>
    ///     Returns the index of the first element from the input sequence for which the value selector returns the largest
    ///     value.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The input collection is empty.</exception>
    public static int MaxIndex<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: true).Value.minMaxIndex;

    /// <summary>
    ///     Returns the index of the first element from the input sequence for which the value selector returns the largest
    ///     value, or a default value if the collection is empty.</summary>
    public static int? MaxIndexOrNull<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: false)?.minMaxIndex;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the smallest value, its index, as well as that smallest value.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The input collection is empty.</exception>
    public static (T element, int index, TValue value) MinTuple<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: true).Value;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the smallest value, its index, as well as that smallest value, or
    ///     <c>null</c> if the collection is empty.</summary>
    public static (T element, int index, TValue value)? MinTupleOrNull<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where T : struct
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: true, doThrow: false);

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the largest value, its index, as well as that largest value.</summary>
    /// <exception cref="InvalidOperationException">
    ///     The input collection is empty.</exception>
    public static (T element, int index, TValue value) MaxTuple<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: true).Value;

    /// <summary>
    ///     Returns the first element from the input sequence for which the value selector returns the largest value, its index, as well as that largest value, or
    ///     <c>null</c> if the collection is empty.</summary>
    public static (T element, int index, TValue value)? MaxTupleOrNull<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector)
        where T : struct
        where TValue : IComparable<TValue> =>
        minMaxElement(source, valueSelector, min: false, doThrow: false);

    private static (T minMaxElem, int minMaxIndex, TValue minMaxValue)? minMaxElement<T, TValue>(IEnumerable<T> source, Func<T, TValue> valueSelector, bool min, bool doThrow)
        where TValue : IComparable<TValue>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (valueSelector == null)
            throw new ArgumentNullException(nameof(valueSelector));

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return doThrow ? throw new InvalidOperationException("source contains no elements.") : null;
        }
        var minMaxElem = enumerator.Current;
        var minMaxValue = valueSelector(minMaxElem);
        var minMaxIndex = 0;
        var curIndex = 0;
        while (enumerator.MoveNext())
        {
            curIndex++;
            var value = valueSelector(enumerator.Current);
            if (min ? (value.CompareTo(minMaxValue) < 0) : (value.CompareTo(minMaxValue) > 0))
            {
                minMaxValue = value;
                minMaxElem = enumerator.Current;
                minMaxIndex = curIndex;
            }
        }
        return (minMaxElem, minMaxIndex, minMaxValue);
    }

    /// <summary>
    ///     Returns all elements for which the <paramref name="valueSelector"/> returns the smallest value, or an empty
    ///     sequence if the input sequence is empty.</summary>
    public static IEnumerable<T> MinElements<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector) where TValue : IComparable<TValue>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return valueSelector == null
            ? throw new ArgumentNullException(nameof(valueSelector))
            : minMaxElements(source, valueSelector, min: true);
    }

    /// <summary>
    ///     Returns all elements for which the <paramref name="valueSelector"/> returns the largest value, or an empty
    ///     sequence if the input sequence is empty.</summary>
    public static IEnumerable<T> MaxElements<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector) where TValue : IComparable<TValue>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return valueSelector == null
            ? throw new ArgumentNullException(nameof(valueSelector))
            : minMaxElements(source, valueSelector, min: false);
    }

    private static List<T> minMaxElements<T, TValue>(IEnumerable<T> source, Func<T, TValue> valueSelector, bool min) where TValue : IComparable<TValue>
    {
        var results = new List<T>();
        TValue minMaxValue = default;
        foreach (var el in source)
        {
            var value = valueSelector(el);
            if (results.Count == 0)
                minMaxValue = value;
            var compare = value.CompareTo(minMaxValue);
            if (min ? (compare < 0) : (compare > 0))
            {
                minMaxValue = value;
                results.Clear();
                results.Add(el);
            }
            else if (compare == 0)
                results.Add(el);
        }

        return results;
    }

    /// <summary>
    ///     Returns the first element of a sequence, or <c>null</c> if the sequence contains no elements.</summary>
    /// <typeparam name="T">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">
    ///     The <see cref="IEnumerable&lt;T&gt;"/> to return the first element of.</param>
    /// <returns>
    ///     <c>null</c> if <paramref name="source"/> is empty; otherwise, the first element in <paramref name="source"/>.</returns>
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        using var e = source.GetEnumerator();
        return e.MoveNext() ? e.Current : null;
    }

    /// <summary>
    ///     Returns the first element of a sequence that satisfies a given predicate, or <c>null</c> if the sequence contains
    ///     no elements.</summary>
    /// <typeparam name="T">
    ///     The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">
    ///     The <see cref="IEnumerable&lt;T&gt;"/> to return the first element of.</param>
    /// <param name="predicate">
    ///     Only consider elements that satisfy this predicate.</param>
    /// <returns>
    ///     <c>null</c> if <paramref name="source"/> is empty; otherwise, the first element in <paramref name="source"/>.</returns>
    public static T? FirstOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        using var e = source.GetEnumerator();
        while (e.MoveNext())
            if (predicate(e.Current))
                return e.Current;
        return null;
    }

    /// <summary>
    ///     Turns all elements in the enumerable to strings and joins them using the specified <paramref name="separator"/>
    ///     and the specified <paramref name="prefix"/> and <paramref name="suffix"/> for each string.</summary>
    /// <param name="values">
    ///     The sequence of elements to join into a string.</param>
    /// <param name="separator">
    ///     Optionally, a separator to insert between each element and the next.</param>
    /// <param name="prefix">
    ///     Optionally, a string to insert in front of each element.</param>
    /// <param name="suffix">
    ///     Optionally, a string to insert after each element.</param>
    /// <param name="lastSeparator">
    ///     Optionally, a separator to use between the second-to-last and the last element.</param>
    /// <example>
    ///     <code>
    ///         // Returns "[Paris], [London], [Tokyo]"
    ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]")
    ///
    ///         // Returns "[Paris], [London] and [Tokyo]"
    ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]", " and ");</code></example>
    public static string JoinString<T>(this IEnumerable<T> values, string separator = null, string prefix = null, string suffix = null, string lastSeparator = null)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        lastSeparator ??= separator;

        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
            return "";

        // Optimise the case where there is only one element
        var one = enumerator.Current;
        if (!enumerator.MoveNext())
            return prefix + one + suffix;

        // Optimise the case where there are only two elements
        var two = enumerator.Current;
        if (!enumerator.MoveNext())
        {
            // Optimise the (common) case where there is no prefix/suffix; this prevents an array allocation when calling string.Concat()
            return prefix == null && suffix == null ? one + lastSeparator + two : prefix + one + suffix + lastSeparator + prefix + two + suffix;
        }

        var sb = new StringBuilder()
            .Append(prefix).Append(one).Append(suffix).Append(separator)
            .Append(prefix).Append(two).Append(suffix);
        var prev = enumerator.Current;
        while (enumerator.MoveNext())
        {
            sb.Append(separator).Append(prefix).Append(prev).Append(suffix);
            prev = enumerator.Current;
        }
        sb.Append(lastSeparator).Append(prefix).Append(prev).Append(suffix);
        return sb.ToString();
    }

    /// <summary>Creates a <see cref="HashSet{T}"/> from an enumerable collection.</summary>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return comparer == null ? new HashSet<T>(source) : new HashSet<T>(source, comparer);
    }

    /// <summary>
    ///     Returns a collection of integers containing the indexes at which the elements of the source collection match the
    ///     given predicate.</summary>
    /// <typeparam name="T">
    ///     The type of elements in the collection.</typeparam>
    /// <param name="source">
    ///     The source collection whose elements are tested using <paramref name="predicate"/>.</param>
    /// <param name="predicate">
    ///     The predicate against which the elements of <paramref name="source"/> are tested.</param>
    /// <returns>
    ///     A collection containing the zero-based indexes of all the matching elements, in increasing order.</returns>
    public static IEnumerable<int> SelectIndexWhere<T>(this IEnumerable<T> source, Predicate<T> predicate)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        IEnumerable<int> selectIndexWhereIterator()
        {
            var i = 0;
            using var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                if (predicate(e.Current))
                    yield return i;
                i++;
            }
        }
        return selectIndexWhereIterator();
    }

    /// <summary>
    ///     Returns the index of the first element in this <paramref name="source"/> satisfying the specified <paramref
    ///     name="predicate"/>. If no such elements are found, returns <c>-1</c>.</summary>
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        var index = 0;
        foreach (var v in source)
        {
            if (predicate(v))
                return index;
            index++;
        }
        return -1;
    }
}
