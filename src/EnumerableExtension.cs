using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Random;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable PossibleMultipleEnumeration

namespace Soenneker.Extensions.Enumerable;

/// <summary>
/// A collection of helpful enumerable extension methods
/// </summary>
public static class EnumerableExtension
{
    /// <summary>
    /// Determines whether the collection is null or contains no elements.
    /// </summary>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable) => enumerable is null || enumerable.Empty();

    /// <summary>
    /// Shorthand for <see cref="IsNullOrEmpty{T}"/> == false.
    /// </summary>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Populated<T>([NotNullWhen(true)] this IEnumerable<T>? enumerable) => enumerable is not null && !enumerable.Empty();

    /// <summary>
    /// Determines whether the collection is empty. Assumes collection is non-null.
    /// </summary>
    [Pure]
    public static bool Empty<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        // Fast paths
        if (enumerable is ICollection<T> c1)
            return c1.Count == 0;

        if (enumerable is IReadOnlyCollection<T> c2)
            return c2.Count == 0;

        // Enumerate minimally
        using IEnumerator<T> e = enumerable.GetEnumerator();
        return !e.MoveNext();
    }

    /// <summary>
    /// Removes duplicate elements from the specified sequence, preserving the first occurrence order.
    /// </summary>
    [Pure]
    public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        int capacity = enumerable is ICollection<T> c ? c.Count : enumerable is IReadOnlyCollection<T> rc ? rc.Count : 0;

        return RemoveDuplicatesIterator(enumerable, capacity);
    }

    private static IEnumerable<T> RemoveDuplicatesIterator<T>(IEnumerable<T> source, int capacity)
    {
        HashSet<T> seen = capacity > 0 ? new HashSet<T>(capacity) : new HashSet<T>();

        foreach (T item in source)
        {
            if (seen.Add(item))
                yield return item;
        }
    }

    /// <summary>
    /// Removes duplicate elements from the specified sequence based on a specified key.
    /// Preserves the first occurrence order.
    /// </summary>
    [Pure]
    public static IEnumerable<T> RemoveDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        int capacity = source is ICollection<T> c ? c.Count : source is IReadOnlyCollection<T> rc ? rc.Count : 0;

        return RemoveDuplicatesByKeyIterator(source, keySelector, capacity);
    }

    private static IEnumerable<T> RemoveDuplicatesByKeyIterator<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector, int capacity)
    {
        HashSet<TKey> seenKeys = capacity > 0 ? new HashSet<TKey>(capacity) : new HashSet<TKey>();

        foreach (T element in source)
        {
            TKey key = keySelector(element);
            if (seenKeys.Add(key))
                yield return element;
        }
    }

    /// <summary>
    /// Preferably you should use the List extension if you have a list. This will not throw an exception due to null or empty.
    /// </summary>
    [Pure]
    public static T GetRandom<T>(this IEnumerable<T>? enumerable) => TryGetRandom(enumerable, out T value) ? value : default!;

    /// <summary>
    /// Throws an exception if the enumerable is null or empty.
    /// </summary>
    [Pure]
    public static T GetRandomStrict<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        if (!TryGetRandom(enumerable, out T value))
            throw new ArgumentOutOfRangeException(nameof(enumerable), "The collection cannot be empty.");

        return value;
    }

    /// <summary>
    /// Shared random selection logic with fast paths and reservoir sampling fallback.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetRandom<T>(IEnumerable<T>? enumerable, out T value)
    {
        value = default!;

        if (enumerable is null)
            return false;

        // Fast path: arrays
        if (enumerable is T[] array)
        {
            if ((uint)array.Length == 0)
                return false;

            value = array[RandomUtil.Next(0, array.Length)];
            return true;
        }

        // Fast path: IList<T>
        if (enumerable is IList<T> list)
        {
            int count = list.Count;
            if ((uint)count == 0)
                return false;

            value = list[RandomUtil.Next(0, count)];
            return true;
        }

        // Count-known but not indexable: pick a target index then walk once
        if (enumerable is ICollection<T> collection)
        {
            int count = collection.Count;
            if ((uint)count == 0)
                return false;

            int target = RandomUtil.Next(0, count);

            var i = 0;
            foreach (T item in enumerable)
            {
                if (i++ == target)
                {
                    value = item;
                    return true;
                }
            }

            // Should not happen if Count is honest, but keep it safe.
            return false;
        }

        // Reservoir sampling: single pass, no buffering
        var seen = 0;
        foreach (T element in enumerable)
        {
            seen++;
            if (RandomUtil.Next(0, seen) == 0)
                value = element;
        }

        return seen != 0;
    }

    /// <summary>
    /// Determines whether the specified sequence contains any duplicate elements.
    /// </summary>
    [Pure]
    public static bool ContainsDuplicates<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable is null)
            return false;

        int count = enumerable is ICollection<T> c ? c.Count : enumerable is IReadOnlyCollection<T> rc ? rc.Count : -1;

        if (count is 0 or 1)
            return false;

        HashSet<T> set = count > 1 ? new HashSet<T>(count) : new HashSet<T>();

        foreach (T item in enumerable)
        {
            if (!set.Add(item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the sequence contains any elements that satisfy the specified predicate.
    /// </summary>
    [Pure]
    public static bool Contains<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        // Avoid the ICollection split: foreach already avoids enumerator allocations for arrays/lists
        foreach (T item in source)
        {
            if (predicate(item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Removes null elements from the specified sequence of items. If the source sequence is null, it returns null.
    /// </summary>
    [Pure]
    [return: NotNullIfNotNull("source")]
    public static IEnumerable<T>? RemoveNulls<T>(this IEnumerable<T>? source)
    {
        if (source is null)
            return null;

        return RemoveNullsInternal(source);
    }

    private static IEnumerable<T> RemoveNullsInternal<T>(IEnumerable<T> source)
    {
        foreach (T item in source)
        {
            if (item is not null)
                yield return item;
        }
    }

    /// <summary>
    /// Attempts to retrieve the count of elements in the specified enumerable without fully enumerating it. This is NOT an actual count, it's a preallocation hint.
    /// </summary>
    /// <remarks>This method uses efficient mechanisms to obtain the count, such as checking for
    /// implementations of ICollection<T>, IReadOnlyCollection<T>, or TryGetNonEnumeratedCount. If the count cannot be
    /// determined without enumeration, the method returns 0 and does not enumerate the collection.</remarks>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="enumerable">The enumerable collection whose element count is to be determined. Cannot be null.</param>
    /// <returns>The number of elements in the enumerable if the count can be determined without enumeration; otherwise, 0.</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNonEnumeratedCount<T>(this IEnumerable<T> enumerable)
    {
        // Linq.. but this isn't slow nor allocates
        if (enumerable.TryGetNonEnumeratedCount(out int count))
            return count;

        if (enumerable is ICollection<T> c)
            return c.Count;

        if (enumerable is IReadOnlyCollection<T> roc)
            return roc.Count;

        return 0;
    }

    /// <summary>
    /// Computes a hash code for an IEnumerable that incorporates the hash codes of all elements
    /// and optionally the runtime identity of the collection instance.
    /// </summary>
    [Pure]
    public static int GetAggregateHashCode<T>(this IEnumerable<T>? enumerable, bool includeIdentity = true)
    {
        if (enumerable is null)
            return -1;

        var hc = new HashCode();

        if (includeIdentity)
            hc.Add(RuntimeHelpers.GetHashCode(enumerable));

        foreach (T item in enumerable)
            hc.Add(item);

        return hc.ToHashCode();
    }

    /// <summary>
    /// Flattens and returns a collection of recursive children. Preserves underlying references even though it's a new list.
    /// </summary>
    [Pure]
    [return: NotNullIfNotNull("enumerable")]
    public static List<T>? ToFlattenedFromRecursive<T>(this IEnumerable<T>? enumerable, Func<T, IEnumerable<T>?> childSelector)
    {
        if (enumerable is null)
            return null;

        ArgumentNullException.ThrowIfNull(childSelector);

        int estimatedCapacity = enumerable is ICollection<T> c ? c.Count : enumerable is IReadOnlyCollection<T> rc ? rc.Count : 16;

        var result = new List<T>(estimatedCapacity);
        var queue = new Queue<T>();

        foreach (T item in enumerable)
            queue.Enqueue(item);

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            result.Add(current);

            IEnumerable<T>? children = childSelector(current);
            if (children is null)
                continue;

            foreach (T child in children)
                queue.Enqueue(child);
        }

        return result;
    }

    /// <summary>
    /// Expression-based overload; compiles a delegate once per call (no reflection per item).
    /// </summary>
    [Pure]
    [return: NotNullIfNotNull("enumerable")]
    public static List<T>? ToFlattenedFromRecursiveExpr<T>(this IEnumerable<T>? enumerable, Expression<Func<T, IEnumerable<T>?>> childCollection)
    {
        if (enumerable is null)
            return null;

        ArgumentNullException.ThrowIfNull(childCollection);

        // Compile once per call. If you call this in tight loops, prefer the Func<> overload.
        Func<T, IEnumerable<T>?> selector = childCollection.Compile();
        return enumerable.ToFlattenedFromRecursive(selector);
    }

    [Pure]
    public static async IAsyncEnumerable<T> WhereAsync<T>(this IEnumerable<T> source, Func<T, CancellationToken, Task<bool>> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);

        if (cancellationToken.IsCancellationRequested)
            yield break;

        foreach (T item in source)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            bool include;
            try
            {
                include = await filter(item, cancellationToken)
                    .NoSync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (include)
                yield return item;
        }
    }

    [Pure]
    public static async IAsyncEnumerable<T> WhereAsync<T>(this IEnumerable<T> source, Func<T, CancellationToken, ValueTask<bool>> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);

        if (cancellationToken.IsCancellationRequested)
            yield break;

        foreach (T item in source)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            bool include;
            try
            {
                include = await filter(item, cancellationToken)
                    .NoSync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (include)
                yield return item;
        }
    }

    [Pure]
    public static async IAsyncEnumerable<T> WhereAsyncOrThrow<T>(this IEnumerable<T> source, Func<T, CancellationToken, Task<bool>> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);

        foreach (T item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await filter(item, cancellationToken)
                    .NoSync())
                yield return item;
        }
    }

    [Pure]
    public static async IAsyncEnumerable<T> WhereAsyncOrThrow<T>(this IEnumerable<T> source, Func<T, CancellationToken, ValueTask<bool>> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);

        foreach (T item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await filter(item, cancellationToken)
                    .NoSync())
                yield return item;
        }
    }
}