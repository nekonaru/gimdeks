using System.Globalization;
using System.Threading;
using GimDeks.Core.Rules;
using Xunit;

namespace GimDeks.Tests.Rules;

public class NumberParserTests
{
    [Fact]
    public void ParsesInvariantCultureDecimal()
    {
        Assert.Equal(32.2, NumberParser.ParseGb("32.2"));
    }

    [Fact]
    public void ParsesIndonesianCultureDecimal()
    {
        Assert.Equal(32.2, NumberParser.ParseGb("32,2"));
    }

    [Fact]
    public void ParsesUnderIndonesianCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("id-ID");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("id-ID");
            Assert.Equal(32.2, NumberParser.ParseGb("32,2"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("abc")]
    public void ReturnsNullForUnparseableInput(string? raw)
    {
        Assert.Null(NumberParser.ParseGb(raw));
    }

    [Fact]
    public void ParsesWholeNumber()
    {
        Assert.Equal(139.0, NumberParser.ParseGb("139"));
    }
}
