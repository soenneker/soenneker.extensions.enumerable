[![](https://img.shields.io/nuget/v/Soenneker.Extensions.Enumerable.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Enumerable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.enumerable/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.enumerable/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Extensions.Enumerable.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Enumerable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.enumerable/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.enumerable/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.Enumerable

General-purpose extensions for probing, filtering, deduplicating, randomly selecting, hashing, and recursively flattening sequences, plus sequential asynchronous filtering.

## Installation

```bash
dotnet add package Soenneker.Extensions.Enumerable
```

## Null and emptiness checks

```csharp
using Soenneker.Extensions.Enumerable;

IEnumerable<string>? values = null;

values.IsNullOrEmpty(); // true
values.Populated();     // false

new[] { "a" }.Empty(); // false
```

`IsNullOrEmpty()` and `Populated()` accept null. `Empty()` requires a non-null sequence. Each method uses a known collection count when available; otherwise it opens an enumerator and probes one item. Avoid probing a single-use enumerable and then assuming that first item remains available.

`GetNonEnumeratedCount()` returns a known count without consuming the sequence, or `0` when no count is available. A returned zero is therefore a sizing hint, not proof that an arbitrary enumerable is empty.

## Deduplication and null filtering

```csharp
IEnumerable<string> unique = new[] { "a", "a", "b" }.RemoveDuplicates();
IEnumerable<Person> uniquePeople = people.RemoveDuplicates(person => person.Id);
bool hasDuplicates = people.ContainsDuplicates();

IEnumerable<string>? nonNull = nullableStrings.RemoveNulls();
```

Deduplication is lazy, uses the default equality comparer, and preserves the first occurrence of each value or selected key. `ContainsDuplicates()` stops at the first duplicate and returns `false` for a null source. Both class and nullable-struct sequences have `RemoveNulls()` overloads; a null source remains null.

`Contains(predicate)` is an `Any`-style predicate search with optimized array and list paths. It short-circuits on the first match.

## Random selection

```csharp
string? optional = values.GetRandom();
string required = values.GetRandomStrict();
```

`GetRandom()` returns `default(T)` for null or empty input. That can be indistinguishable from a selected default value. `GetRandomStrict()` throws for null or empty input. Indexable collections use direct selection; other enumerables are visited once with reservoir sampling. Selection uses the library’s ordinary pseudo-random source and is not suitable for security decisions. Infinite sequences never complete.

## Recursive flattening

```csharp
var root = new Node("root", [new Node("child", [])]);

List<Node> nodes = new[] { root }
    .ToFlattenedFromRecursive(node => node.Children)!;
// root, child
```

Traversal is breadth-first and includes every item in the starting sequence. The returned list is new, but its elements are the original references. Null child collections are treated as empty. The expression overload compiles once per call; prefer the `Func` overload when the selector can be reused.

The traversal does not detect cycles or repeated nodes. Supply an acyclic tree, or filter visited nodes before using it with a graph.

## Asynchronous filtering

```csharp
List<int> even = await numbers
    .WhereAsync(async (number, token) =>
    {
        await CheckAsync(number, token);
        return number % 2 == 0;
    }, cancellationToken)
    .ToListAsync(cancellationToken);
```

Task- and `ValueTask`-based predicates are supported. Items are evaluated sequentially and yielded lazily in source order; this API does not run predicates in parallel.

`WhereAsync()` treats cancellation through its supplied token as normal early completion. `WhereAsyncOrThrow()` calls `ThrowIfCancellationRequested()` and propagates cancellation to the consumer. Predicate failures other than matching cancellation propagate from both variants.

## Aggregate hash codes

`GetAggregateHashCode()` combines each element in enumeration order. By default it also includes the sequence object’s runtime identity, so two separate sequences with equal contents need not match. Pass `includeIdentity: false` for a content-only hash. Hash codes are process-local and should not be persisted or used as cryptographic digests.
