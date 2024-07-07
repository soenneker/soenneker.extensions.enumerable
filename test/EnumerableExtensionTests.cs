using System.Collections.Generic;
using System.Linq;
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
        var distinctPeople = people.RemoveDuplicates(p => p.Id).ToList();

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

        string value = test.GetRandom();
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
}