using my_project.Domain;

namespace my_project.Tests.Domain;

public class DisplayNameRulesTests
{
    [Fact]
    public void Trims_surrounding_whitespace() // EC-9
    {
        Assert.True(DisplayNameRules.TryNormalize("  Sam  ", out var normalized));
        Assert.Equal("Sam", normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Rejects_an_empty_or_whitespace_only_name(string? raw) => // EC-9
        Assert.False(DisplayNameRules.TryNormalize(raw, out _));

    [Fact]
    public void Accepts_exactly_fifty_characters() =>
        Assert.True(DisplayNameRules.TryNormalize(new string('a', 50), out _));

    [Fact]
    public void Rejects_fifty_one_characters() =>
        Assert.False(DisplayNameRules.TryNormalize(new string('a', 51), out _));

    [Fact]
    public void Counts_emoji_as_one_character() // EC-10
    {
        // A family emoji is a single grapheme built from many code points; counting UTF-16
        // units would wrongly blow the 50-character budget.
        var name = string.Concat(Enumerable.Repeat("👨‍👩‍👧‍👦", 10));

        Assert.True(DisplayNameRules.TryNormalize(name, out _));
    }

    [Fact]
    public void Accepts_non_latin_script() => // EC-10
        Assert.True(DisplayNameRules.TryNormalize("克里斯", out _));

    [Theory]
    [InlineData("Sam", "sam")]
    [InlineData("  SAM ", "Sam")]
    public void Comparison_key_ignores_case_and_whitespace(string a, string b) => // INV-3
        Assert.Equal(DisplayNameRules.ToComparisonKey(a), DisplayNameRules.ToComparisonKey(b));

    [Fact]
    public void Comparison_key_still_separates_different_names() =>
        Assert.NotEqual(DisplayNameRules.ToComparisonKey("Sam"), DisplayNameRules.ToComparisonKey("Kris"));
}
