using GimDeks.Core.Models;
using GimDeks.Core.Rules;
using Xunit;

namespace GimDeks.Tests.Rules;

public sealed class GameExecutableMatcherTests
{
    [Fact]
    public void MatchesExactNormalizedNameAndSkipsGamesWithExistingPath()
    {
        var games = new[]
        {
            new Game { Id = 1, Nama = "Resident Evil 4", Status = GameStatus.SudahAda },
            new Game { Id = 2, Nama = "Already Linked", Status = GameStatus.SudahAda, PathExe = @"C:\Games\ linked.exe" },
        };
        var exes = new[]
        {
            new ScannedExecutable(@"D:\Games\RE4\Resident Evil 4.exe", "Resident Evil 4.exe", "resident evil 4"),
        };

        var matches = GameExecutableMatcher.Match(games, exes);

        Assert.Single(matches);
        Assert.Equal(1, matches[0].Game.Id);
        Assert.Equal(@"D:\Games\RE4\Resident Evil 4.exe", matches[0].Executable.Path);
    }

    [Fact]
    public void PrefersExactOverContainedMatchAndUsesEachExecutableOnce()
    {
        var games = new[]
        {
            new Game { Id = 1, Nama = "Stardew Valley", Status = GameStatus.SudahAda },
            new Game { Id = 2, Nama = "Stardew", Status = GameStatus.SudahAda },
        };
        var exes = new[]
        {
            new ScannedExecutable(@"D:\a\Stardew Valley Launcher.exe", "Stardew Valley Launcher.exe", "stardew valley"),
        };

        var matches = GameExecutableMatcher.Match(games, exes);

        Assert.Single(matches);
        Assert.Equal(1, matches[0].Game.Id);
        Assert.True(matches[0].IsExact);
    }

    [Fact]
    public void AcceptsContainedNameOnlyWhenShortSideIsLongEnough()
    {
        var games = new[]
        {
            new Game { Id = 1, Nama = "Hollow Knight", Status = GameStatus.SudahAda },
            new Game { Id = 2, Nama = "Cat", Status = GameStatus.SudahAda },
        };
        var exes = new[]
        {
            new ScannedExecutable(@"D:\a\Hollow Knight Silksong.exe", "Hollow Knight Silksong.exe", "hollow knight silksong"),
            new ScannedExecutable(@"D:\b\Catalog.exe", "Catalog.exe", "catalog"),
        };

        var matches = GameExecutableMatcher.Match(games, exes);

        var match = Assert.Single(matches);
        Assert.Equal(1, match.Game.Id);
        Assert.False(match.IsExact);
    }

    [Fact]
    public void ReturnsEmptyWhenEitherSideIsEmpty()
    {
        var game = new Game { Id = 1, Nama = "A Game", Status = GameStatus.SudahAda };

        Assert.Empty(GameExecutableMatcher.Match(new[] { game }, Array.Empty<ScannedExecutable>()));
        Assert.Empty(GameExecutableMatcher.Match(Array.Empty<Game>(), new[]
        {
            new ScannedExecutable(@"D:\a.exe", "a.exe", "a"),
        }));
    }
}
