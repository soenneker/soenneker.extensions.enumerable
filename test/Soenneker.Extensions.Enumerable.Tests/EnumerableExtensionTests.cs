using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using FluentAssertions;
using Soenneker.Dtos.IdNamePair;
using Xunit;

namespace Soenneker.Extensions.Enumerable.Tests;

public class EnumerableExtensionTests 
{
    [Fact]
    public void RemoveDuplicates_should_remove_duplicates()
    {
        var test = new List<string> {"a", "a", "b"};

        IEnumerable<string> dupesRemoved = test.RemoveDuplicates();
        dupesRemoved.Count().Should().Be(2);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var pairs = new List<IdNamePair>
        {
            new() { Id = "1", Name = "Alice" },
            new() { Id = "2", Name = "Bob" },
            new() { Id = "3", Name = "Charlie" }
        };

        // Act
        bool result = pairs.Contains(p => p.Id == "2");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RemoveDuplicates_ShouldRemoveDuplicatesBasedOnId()
    {
        // Arrange
        var people = new List<IdNamePair>
        {
            new() { Id = "1", Name = "Alice" },
            new() { Id = "2", Name = "Bob" },
            new() { Id = "1", Name = "Alice" }, // Duplicate
            new() { Id = "3", Name = "Charlie" }
        };

        // Act
        List<IdNamePair> distinctPeople = people.RemoveDuplicates(p => p.Id).ToList();

        // Assert
        distinctPeople.Should().HaveCount(3);
        distinctPeople.Should().Contain(p => p.Id == "1" && p.Name == "Alice");
        distinctPeople.Should().Contain(p => p.Id == "2" && p.Name == "Bob");
        distinctPeople.Should().Contain(p => p.Id == "3" && p.Name == "Charlie");
    }

    [Fact]
    public void GetRandom_should_retrieve_value()
    {
        var test = new[] { "a", "b" };

        string? value = test.GetRandom();
        value.Should().NotBeNull();
    }

    [Fact]
    public void RemoveNulls_should_remove_nulls_without_nulls()
    {
        var test = new[] { "a", "b" };

        IEnumerable<string> value = test.RemoveNulls();
        value.Count().Should().Be(2);
    }

    [Fact]
    public void RemoveNulls_should_remove_nulls()
    {
        var test = new[] { "a", null, "b" };

        IEnumerable<string?> value = test.RemoveNulls();
        value.Count().Should().Be(2);
    }

    [Fact]
    public async System.Threading.Tasks.Task WhereAsync_Should_FilterItemsBasedOnPredicate()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };
        Func<int, CancellationToken, Task<bool>> filter = async (item, token) =>
        {
            await System.Threading.Tasks.Task.Delay(10, token);
            return item % 2 == 0;
        };

        List<int> result = await source.WhereAsync(filter).ToListAsync();

        result.Should().BeEquivalentTo(new List<int> { 2, 4 });
    }

    [Fact]
    public async System.Threading.Tasks.Task WhereAsync_Should_StopFiltering_WhenCancellationIsRequested()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };
        var cts = new CancellationTokenSource();
        Func<int, CancellationToken, Task<bool>> filter = async (item, token) =>
        {
            await System.Threading.Tasks.Task.Delay(10, token);
            if (item == 3)
            {
                await cts.CancelAsync();
            }
            return true;
        };

        List<int> result = await source.WhereAsync(filter, cts.Token).ToListAsync();

        result.Should().BeEquivalentTo(new List<int> { 1, 2, 3 });
    }
}