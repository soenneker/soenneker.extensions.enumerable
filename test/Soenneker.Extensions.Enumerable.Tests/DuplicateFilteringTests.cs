using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;

namespace Soenneker.Extensions.Enumerable.Tests;

public sealed class DuplicateFilteringTests
{
    [Test]
    public void Filtering_IsLazy_AndPreservesFirstOccurrences()
    {
        int enumerations = 0;
        IEnumerable<string> Source()
        {
            enumerations++;
            yield return "first";
            yield return "second";
            yield return "first";
        }

        IEnumerable<string> result = Source().RemoveDuplicates();
        enumerations.Should().Be(0);
        result.Should().Equal("first", "second");
        enumerations.Should().Be(1);
        result.Should().Equal("first", "second");
        enumerations.Should().Be(2);
    }

    [Test]
    public void KeyFiltering_PreservesFirstValue_ForNullAndRepeatedKeys()
    {
        (string? Key, int Value)[] source = [(null, 1), ("a", 2), (null, 3), ("a", 4), ("b", 5)];
        source.RemoveDuplicates(item => item.Key).Select(item => item.Value).Should().Equal(1, 2, 5);
    }

    [Test]
    public void NullArguments_AreRejectedBeforeEnumeration()
    {
        Action nullSource = () => ((IEnumerable<string>)null!).RemoveDuplicates();
        Action nullSelector = () => Array.Empty<string>().RemoveDuplicates<string, int>(null!);
        nullSource.Should().Throw<ArgumentNullException>();
        nullSelector.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Singleton_DoesNotInvokeKeySelector()
    {
        int[] source = [1];
        source.RemoveDuplicates<int, int>(_ => throw new InvalidOperationException()).Should().BeSameAs(source);
    }
}
