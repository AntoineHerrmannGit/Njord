using Njord.Core.Extensions;

namespace Njord.Core.Extensions.Tests;

public sealed class EnumerableExtensionsTests
{
    #region Sequence checks

    [Fact]
    public void IsEmpty_ReturnsTrueForEmptySequence()
    {
        var values = Enumerable.Empty<int>();

        Assert.True(values.IsEmpty());
    }

    [Fact]
    public void IsEmpty_ReturnsFalseForNonEmptySequence()
    {
        IEnumerable<int> values = [1];

        Assert.False(values.IsEmpty());
    }

    [Fact]
    public void IsNotEmpty_ReturnsTrueForNonEmptySequence()
    {
        IEnumerable<int> values = [1];

        Assert.True(values.IsNotEmpty());
    }

    [Fact]
    public void IsNotEmpty_ReturnsFalseForEmptySequence()
    {
        var values = Enumerable.Empty<int>();

        Assert.False(values.IsNotEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_ReturnsTrueForNullSequence()
    {
        IEnumerable<int>? values = null;

        Assert.True(values.IsNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_ReturnsTrueForEmptySequence()
    {
        IEnumerable<int>? values = Enumerable.Empty<int>();

        Assert.True(values.IsNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_ReturnsFalseForNonEmptySequence()
    {
        IEnumerable<int>? values = [1];

        Assert.False(values.IsNullOrEmpty());
    }

    [Fact]
    public void IsNotNullOrEmpty_ReturnsFalseForNullOrEmptySequence()
    {
        IEnumerable<int>? missing = null;
        IEnumerable<int>? empty = Enumerable.Empty<int>();

        Assert.False(missing.IsNotNullOrEmpty());
        Assert.False(empty.IsNotNullOrEmpty());
    }

    [Fact]
    public void IsNotNullOrEmpty_ReturnsTrueForNonEmptySequence()
    {
        IEnumerable<int>? values = [1];

        Assert.True(values.IsNotNullOrEmpty());
    }

    [Fact]
    public void IsEmpty_ThrowsForNullSequence()
    {
        IEnumerable<int>? values = null;

        Assert.Throws<ArgumentNullException>(() => values!.IsEmpty());
    }

    [Fact]
    public void IsNotEmpty_ThrowsForNullSequence()
    {
        IEnumerable<int>? values = null;

        Assert.Throws<ArgumentNullException>(() => values!.IsNotEmpty());
    }

    [Fact]
    public void IsNotEmpty_OnlyRequestsTheFirstElement()
    {
        var enumerationCount = 0;
        var values = CreateSequence(() => enumerationCount++);

        var hasValues = values.IsNotEmpty();

        Assert.True(hasValues);
        Assert.Equal(1, enumerationCount);
    }

    [Fact]
    public void None_ReturnsTrueWhenNoElementMatches()
    {
        IEnumerable<int> values = [1, 2, 3];

        Assert.True(values.None(value => value > 3));
    }

    [Fact]
    public void None_ReturnsFalseWhenAnElementMatches()
    {
        IEnumerable<int> values = [1, 2, 3];

        Assert.False(values.None(value => value == 2));
    }

    [Fact]
    public void None_ReturnsTrueForAnEmptySequence()
    {
        var values = Enumerable.Empty<int>();

        Assert.True(values.None(value => value > 0));
    }

    [Fact]
    public void None_StopsAfterTheFirstMatchingElement()
    {
        var predicateCallCount = 0;
        IEnumerable<int> values = [1, 2, 3];

        var noMatches = values.None(_ => ++predicateCallCount == 2);

        Assert.False(noMatches);
        Assert.Equal(2, predicateCallCount);
    }

    [Fact]
    public void None_ThrowsForNullSequence()
    {
        IEnumerable<int>? values = null;

        Assert.Throws<ArgumentNullException>(() => values!.None(value => value > 0));
    }

    [Fact]
    public void None_ThrowsForNullPredicate()
    {
        IEnumerable<int> values = [1, 2, 3];

        Assert.Throws<ArgumentNullException>(() => values.None(null!));
    }

    #endregion

    #region Test sequences

    private static IEnumerable<int> CreateSequence(Action onEnumerated)
    {
        onEnumerated();
        yield return 1;
        onEnumerated();
        yield return 2;
    }

    #endregion
}
