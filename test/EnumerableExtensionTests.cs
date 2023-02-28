using FluentAssertions;
using Xunit;

namespace Soenneker.Extensions.Enumerable.Tests;

public class EnumerableExtensionTests 
{
    [Fact]
    public void RemoveDuplicates_should_remove_duplicates()
    {
        var test = new List<string>() {"a", "a", "b"};

        IEnumerable<string> dupesRemoved = test.RemoveDuplicates();
        dupesRemoved.Count().Should().Be(2);
    }
}