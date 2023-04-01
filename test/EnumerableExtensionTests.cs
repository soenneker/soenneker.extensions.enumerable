using FluentAssertions;
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
    public void GetRandom_should_retrieve_value()
    {
        var test = new[] { "a", "b" };

        string value = test.GetRandom();
        value.Should().NotBeNull();
    }
}