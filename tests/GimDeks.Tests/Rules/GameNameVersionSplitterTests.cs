using GimDeks.Core.Rules;
using Xunit;

namespace GimDeks.Tests.Rules;

public class GameNameVersionSplitterTests
{
    [Fact]
    public void SplitsTrailingVersion()
    {
        var (name, versi) = GameNameVersionSplitter.Split("God of War Ragnarok v1.7");
        Assert.Equal("God of War Ragnarok", name);
        Assert.Equal("1.7", versi);
    }

    [Fact]
    public void SplitsCommaSeparatedVersion()
    {
        var (name, versi) = GameNameVersionSplitter.Split("Days Gone v1,06");
        Assert.Equal("Days Gone", name);
        Assert.Equal("1,06", versi);
    }

    [Fact]
    public void ReturnsNullVersionWhenNoVersionToken()
    {
        var (name, versi) = GameNameVersionSplitter.Split("Hollow Knight");
        Assert.Equal("Hollow Knight", name);
        Assert.Null(versi);
    }
}
