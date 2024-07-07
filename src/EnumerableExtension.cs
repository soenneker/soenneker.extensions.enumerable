using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Removes duplicate elements from the specified <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="enumerable">The sequence to remove duplicates from.</param>
    /// <returns>A sequence that contains no duplicate elements.</returns>
    /// <remarks>
    /// This method uses a <see cref="HashSet{T}"/> to eliminate duplicate elements.
    /// </remarks>
    [Pure]
    public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> enumerable)
    {
        // Deduplication within ctor
        var hashSet = new HashSet<T>(enumerable);

        return hashSet;
    }

    /// <summary>
    /// Removes duplicate elements from the specified <see cref="IEnumerable{T}"/> based on a specified key.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key to determine uniqueness.</typeparam>
    /// <param name="source">The sequence to remove duplicates from.</param>
    /// <param name="keySelector">A function to extract the key from an element.</param>
    /// <returns>A sequence that contains no duplicate elements based on the specified key.</returns>
    /// <remarks>
    /// This method uses a <see cref="HashSet{TKey}"/> to track seen keys and eliminate duplicate elements.
    /// </remarks>
    [Pure]
    public static IEnumerable<T> RemoveDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();

        foreach (T element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Preferably you should use the List extension if you have a list. This will not throw an exception due to null or empty.
    /// </summary>
    [Pure]
    public static T? GetRandom<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable.IsNullOrEmpty())
            return default;

        int count = enumerable.Count();

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
    /// Determines whether the specified sequence contains any duplicate elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="enumerable">The sequence to check for duplicates. If the sequence is <c>null</c>, the method returns <c>false</c>.</param>
    /// <returns>
    /// <c>true</c> if the sequence contains duplicate elements; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method uses a <see cref="HashSet{T}"/> to track elements that have been seen.
    /// As soon as a duplicate element is found, the method returns <c>true</c> immediately.
    /// If no duplicates are found, the method returns <c>false</c>.
    /// </remarks>
    [Pure]
    public static bool ContainsDuplicates<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable == null)
            return false;

        var hashSet = new HashSet<T>();

        foreach (T item in enumerable)
        {
            if (!hashSet.Add(item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if any element in the specified <see cref="IEnumerable{T}"/> has a key that matches the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key to match.</typeparam>
    /// <param name="source">The sequence to search for the key.</param>
    /// <param name="keySelector">A function to extract the key from an element.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns><c>true</c> if the key is found; otherwise, <c>false</c>.</returns>
    [Pure]
    public static bool ContainsKey<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, TKey key)
    {
        foreach (T element in source)
        {
            if (EqualityComparer<TKey>.Default.Equals(keySelector(element), key))
            {
                return true;
            }
        }

        return false;
    }

    [Pure]
    [return: NotNullIfNotNull("source")]
    public static IEnumerable<T>? RemoveNulls<T>(this IEnumerable<T>? source)
    {
        if (source == null)
            return null;

        return RemoveNullsInternal(source);
    }

    /// <summary>
    /// Computes a hash code for an IEnumerable that incorporates the hash codes of all elements 
    /// in the collection as well as the hash code based on the runtime identity of the collection instance.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="enumerable">The enumerable for which the hash code is to be calculated.</param>
    /// <param name="includeIdentity"></param>
    /// <returns>An integer representing the combined hash code of the instance identity and the elements within the list. If the enumerable is null, returns -1</returns>
    /// <remarks>
    /// This method is useful for scenarios where lists are used as keys in collections
    /// and you want to differentiate between different instances of a list with the same elements.
    /// Note that using this hash code in persisting collections or across different executions might lead
    /// to different results due to its dependency on runtime instance identities.
    /// </remarks>
    [Pure]
    public static int GetAggregateHashCode<T>(this IEnumerable<T>? enumerable, bool includeIdentity = true)
    {
        if (enumerable == null)
            return -1;

        var hash = new HashCode();

        if (includeIdentity)
            hash.Add(RuntimeHelpers.GetHashCode(enumerable));

        foreach (T item in enumerable)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
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