using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Soenneker.Utils.Random;

namespace Soenneker.Extensions.Enumerable;

/// <summary>
/// A collection of helpful enumerable extension methods
/// </summary>
public static class EnumerableExtension
{
    /// <summary>
    /// Determines whether the collection is null or contains no elements.
    /// </summary>
    /// <typeparam name="T">The IEnumerable type.</typeparam>
    /// <param name="enumerable">The enumerable, which may be null or empty.</param>
    /// <returns>
    ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
    /// </returns>
    [Pure]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable)
    {
        if (enumerable == null)
            return true;

        return Empty(enumerable);
    }

    /// <summary>
    /// Shorthand for <see cref="IsNullOrEmpty{T}"/> == false.
    /// </summary>
    /// <remarks>Makes calls like <code>if (!list.IsNullOrEmpty())</code> easier to read</remarks>
    [Pure]
    public static bool Populated<T>([NotNullWhen(true)] this IEnumerable<T>? enumerable)
    {
        return !IsNullOrEmpty(enumerable);
    }

    /// <summary>
    /// Determines whether the collection is empty <para></para>
    /// Assumes collection is non-null, will throw if it is. <see cref="IsNullOrEmpty{T}"/> for more safety.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    [Pure]
    public static bool Empty<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));

        /* If this is a list, use the Count property for efficiency.
         * The Count property is O(1) while IEnumerable.Count() is O(N). */

        if (enumerable is ICollection<T> collection)
            return collection.Count == 0;

        if (enumerable is IReadOnlyCollection<T> readonlyCollection) // Need this because IReadOnlyCollection does not inherit from ICollection
            return readonlyCollection.Count == 0;

        return !enumerable.Any();
    }

    [Pure]
    public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> enumerable)
    {
        // Deduplication within ctor
        var hashSet = new HashSet<T>(enumerable);

        return hashSet;
    }

    /// <summary>
    /// Preferably you should use the List extension if you have a list. This will not throw an exception due to null or empty.
    /// </summary>
    [Pure]
    public static T? GetRandom<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable.IsNullOrEmpty())
            return default;

        var count = enumerable.Count();

        if (count == 1)
            return enumerable.ElementAt(0);

        int index = RandomUtil.Next(0, count);

        T result = enumerable.ElementAt(index);
        return result;
    }

    /// <summary>
    /// Throws an exception if the enumerable is null or empty.
    /// </summary>
    /// <remarks>Preferably you should use the List extension if you have a list.</remarks>
    [Pure]
    public static T GetRandomStrict<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
        {
            throw new ArgumentNullException(nameof(enumerable));
        }

        int count = enumerable.Count();

        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(enumerable));

        if (count == 1)
            return enumerable.ElementAt(0);

        int index = RandomUtil.Next(0, count);

        T result = enumerable.ElementAt(index);
        return result;
    }

    /// <summary>
    /// Iterates through the enumerable, adding each item to a HashSet. If it ever fails to add it early exits and returns true.
    /// </summary>
    [Pure]
    public static bool ContainsDuplicates<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable.IsNullOrEmpty())
            return false;

        var hashSet = new HashSet<T>();

        foreach (T item in enumerable)
        {
            bool result = hashSet.Add(item);

            if (!result)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Iterates through the async enumerable, awaiting
    /// </summary>
    /// <remarks>Does not maintain synchronization context</remarks>
    [Pure]
    public static async ValueTask<List<T>> ToList<T>(this IAsyncEnumerable<T> enumerable)
    {
        var result = new List<T>();

        await foreach (T item in enumerable.ConfigureAwait(false))
        {
            result.Add(item);
        }

        return result;
    }

    [Pure]
    [return: NotNullIfNotNull("source")]
    public static IEnumerable<T>? RemoveNulls<T>(this IEnumerable<T>? source)
    {
        if (source == null)
            return null;

        return RemoveNullsInternal(source);
    }

    private static IEnumerable<T> RemoveNullsInternal<T>(IEnumerable<T> source)
    {
        foreach (T item in source)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Flattens and returns a collection of recursive children. Preserves underlying references even though it's a new list.
    /// </summary>
    /// <returns>Null if the source collection is is null. Otherwise returns an empty list if there are no children.</returns>
    [Pure]
    [return: NotNullIfNotNull("enumerable")]
    public static List<T>? ToFlattenedFromRecursive<T>(this IEnumerable<T>? enumerable, Expression<Func<T, IEnumerable<T>?>> childCollection)
    {
        if (enumerable == null)
            return null;

        var resultList = new List<T>();
        var currentItems = new Queue<(int Index, T Item, int Depth)>(enumerable.Select(i => (0, i, 0)));
        var depthItemCounter = 0;
        var previousItemDepth = 0;
        var childProperty = (PropertyInfo) ((MemberExpression) childCollection.Body).Member;
        while (currentItems.Count > 0)
        {
            (int Index, T Item, int Depth) currentItem = currentItems.Dequeue();
            // Reset counter for number of items at this depth when the depth changes.
            if (currentItem.Depth != previousItemDepth) depthItemCounter = 0;
            int resultIndex = currentItem.Index + depthItemCounter++;
            resultList.Insert(resultIndex, currentItem.Item);

            IEnumerable<T> childItems = childProperty.GetValue(currentItem.Item) as IEnumerable<T> ?? System.Linq.Enumerable.Empty<T>();
            foreach (T childItem in childItems)
            {
                currentItems.Enqueue((resultIndex + 1, childItem, currentItem.Depth + 1));
            }

            previousItemDepth = currentItem.Depth;
        }

        return resultList;
    }
}