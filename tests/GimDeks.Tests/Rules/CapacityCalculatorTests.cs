using GimDeks.Core.Models;
using GimDeks.Core.Rules;
using Xunit;

namespace GimDeks.Tests.Rules;

public class CapacityCalculatorTests
{
    private static Storage Hdd(double? batas = 900, double? kapasitas = 1000) => new()
    {
        Nama = "HDD Eksternal 1",
        Tipe = StorageType.HddEksternal,
        KapasitasGb = kapasitas,
        BatasGb = batas,
    };

    private static Game GameOf(GameStatus status, double gb) => new() { Nama = "Game", Status = status, UkuranGb = gb };

    [Fact]
    public void CountedTotalSkipsDikecualikanAndDieliminasi()
    {
        var games = new[]
        {
            GameOf(GameStatus.BelumDownload, 50),
            GameOf(GameStatus.SudahAda, 30),
            GameOf(GameStatus.Dikecualikan, 999),
            GameOf(GameStatus.Dieliminasi, 999),
        };

        Assert.Equal(80, CapacityCalculator.CountedTotalGb(games));
        Assert.Equal(2, CapacityCalculator.CountedGameCount(games));
    }

    [Fact]
    public void ExactlyAtLimitIsNotOverLimit()
    {
        var storage = Hdd(batas: 900);
        Assert.False(CapacityCalculator.IsOverLimit(storage, 900));
        Assert.True(CapacityCalculator.IsOverLimit(storage, 900.1));
    }

    [Fact]
    public void RemainingUsesBatasWhenSetOtherwiseKapasitas()
    {
        Assert.Equal(100, CapacityCalculator.RemainingGb(Hdd(batas: 900), 800));
        Assert.Equal(200, CapacityCalculator.RemainingGb(Hdd(batas: null, kapasitas: 1000), 800));
        Assert.Null(CapacityCalculator.RemainingGb(Hdd(batas: null, kapasitas: null), 800));
    }

    [Fact]
    public void OverLimitIgnoredWhenNoBatas()
    {
        var storage = Hdd(batas: null, kapasitas: 1000);
        Assert.False(CapacityCalculator.IsOverLimit(storage, 5000));
    }

    [Fact]
    public void ProgressFractionIsClamped()
    {
        var storage = Hdd(batas: 900);
        Assert.Equal(0.5, CapacityCalculator.ProgressFraction(storage, 450));
        Assert.Equal(1.0, CapacityCalculator.ProgressFraction(storage, 1200));
        Assert.Equal(0.0, CapacityCalculator.ProgressFraction(storage, 0));
    }

    [Fact]
    public void ProgressFractionIsNullWithoutLimit()
    {
        Assert.Null(CapacityCalculator.ProgressFraction(Hdd(batas: null, kapasitas: null), 100));
    }
}
