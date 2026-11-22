using GimDeks.Core.Models;

namespace GimDeks.Excel;

/// <summary>Import strategy: replace everything, or merge and skip duplicates.</summary>
public enum ImportMode
{
    GantiSemua,
    Gabung,
}

public sealed class ImportPreview
{
    public List<StoragePreview> Storages { get; init; } = new();
    public List<GamePreview> Games { get; init; } = new();
    public List<string> SkippedSheets { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public RingkasanComparison? Ringkasan { get; set; }

    public int TotalGames => Games.Count;
    public int TotalStorages => Storages.Count;
}

public sealed class StoragePreview
{
    public required string SheetName { get; init; }
    public required Storage Storage { get; init; }
    public int GameCount { get; init; }
    public double TotalGb { get; init; }
}

public sealed class GamePreview
{
    public required string SheetName { get; init; }
    public required Game Game { get; init; }
}

public sealed class RingkasanRow
{
    public required string Label { get; init; }
    public string? StorageSheet { get; init; }
    public int? ExpectedCount { get; init; }
    public double? ExpectedGb { get; init; }
    public int ActualCount { get; init; }
    public double ActualGb { get; init; }
    public double DeltaGb => ActualGb - (ExpectedGb ?? 0);
    public int DeltaCount => ActualCount - (ExpectedCount ?? 0);
    public bool Matches => ExpectedCount is not null
        && DeltaCount == 0
        && (!ExpectedGb.HasValue || Math.Abs(DeltaGb) < 0.05);
}

public sealed class RingkasanComparison
{
    public List<RingkasanRow> Rows { get; init; } = new();
    public bool AllMatch
    {
        get
        {
            var verifiable = Rows.Where(r => r.ExpectedCount is not null).ToList();
            return verifiable.Count > 0 && verifiable.All(r => r.Matches);
        }
    }
}

public sealed class ImportResult
{
    public int StoragesAdded { get; init; }
    public int GamesAdded { get; init; }
    public int GamesSkippedAsDuplicate { get; init; }
    public RingkasanComparison? Ringkasan { get; init; }
    public string? BackupPath { get; init; }
}
