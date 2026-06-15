using GolfTrackerApp.Core.Helpers;

namespace GolfTrackerApp.Web.Tests;

public class NumericParsingTests
{
    [Theory]
    [InlineData("70.5", 70.5)]
    [InlineData("68", 68)]
    [InlineData(" 68.1 ", 68.1)]   // trimmed
    public void ParseNullableDecimal_ParsesValidNumbers(string input, double expected)
    {
        Assert.Equal((decimal)expected, NumericParsing.ParseNullableDecimal(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("N/A")]
    [InlineData("n/a")]
    [InlineData("-")]
    [InlineData("TBC")]
    [InlineData("abc")]   // unparseable, non-sentinel → null
    public void ParseNullableDecimal_ReturnsNullForSentinelsAndGarbage(string? input)
    {
        Assert.Null(NumericParsing.ParseNullableDecimal(input));
    }

    [Theory]
    [InlineData("120", 120)]
    [InlineData(" 113 ", 113)]
    public void ParseNullableInt_ParsesValidIntegers(string input, int expected)
    {
        Assert.Equal(expected, NumericParsing.ParseNullableInt(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("N/A")]
    [InlineData("12.5")]  // not an integer → null
    [InlineData("none")]
    public void ParseNullableInt_ReturnsNullForSentinelsAndNonIntegers(string? input)
    {
        Assert.Null(NumericParsing.ParseNullableInt(input));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("N/A", true)]
    [InlineData("116", false)]
    public void IsNullSentinel_RecognisesNoValueCells(string? input, bool expected)
    {
        Assert.Equal(expected, NumericParsing.IsNullSentinel(input));
    }
}
