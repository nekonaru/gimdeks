using GimDeks.Core.Rules;
using Xunit;

namespace GimDeks.Tests.Rules;

public class GameNameNormalizerTests
{
    [Fact]
    public void NormalizesCaseAndPunctuation()
    {
        Assert.Equal("immortals of aveum", GameNameNormalizer.Normalize("Immortals of Aveum!"));
    }

    [Fact]
    public void StripsVersionTokens()
    {
        Assert.Equal("god of war ragnarok", GameNameNormalizer.Normalize("God of War Ragnarok v1.7"));
        Assert.Equal("the last of us part 2", GameNameNormalizer.Normalize("The Last of Us Part 2 v1.4.10515"));
    }

    [Fact]
    public void EquivalentNamesProduceSameKey()
    {
        Assert.Equal(
            GameNameNormalizer.Normalize("Cyberpunk 2077 Ultimate Edition V2,12"),
            GameNameNormalizer.Normalize("Cyberpunk 2077 Ultimate Edition v2.12"));
    }

    [Fact]
    public void HandlesEmptyInput()
    {
        Assert.Equal(string.Empty, GameNameNormalizer.Normalize(""));
        Assert.Equal(string.Empty, GameNameNormalizer.Normalize("   "));
    }
}
