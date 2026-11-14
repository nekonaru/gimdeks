using GimDeks.Core.Models;

namespace GimDeks.Core.Rules;

/// <summary>
/// Capacity rules shared by the whole app. Only games with status
/// BELUM_DOWNLOAD or SUDAH_ADA count toward a storage total.
/// Sisa ruang uses batas_gb when set, otherwise kapasitas_gb.
/// A storage is over its limit only when the total is strictly
/// greater than batas_gb; exactly the limit is not over.
/// </summary>
public static class CapacityCalculator
{
    public static bool CountsTowardTotal(GameStatus status) =>
        status is GameStatus.BelumDownload or GameStatus.SudahAda;

    public static double? EffectiveLimit(Storage storage) =>
        storage.BatasGb ?? storage.KapasitasGb;

    public static double CountedTotalGb(IEnumerable<Game> games) =>
        games.Where(g => CountsTowardTotal(g.Status)).Sum(g => g.UkuranGb);

    public static int CountedGameCount(IEnumerable<Game> games) =>
        games.Count(g => CountsTowardTotal(g.Status));

    /// <summary>True when the storage has a batas and the counted total is strictly greater than it.</summary>
    public static bool IsOverLimit(Storage storage, double totalGb) =>
        storage.BatasGb.HasValue && totalGb > storage.BatasGb.Value;

    public static double? RemainingGb(Storage storage, double totalGb)
    {
        var limit = EffectiveLimit(storage);
        return limit.HasValue ? limit.Value - totalGb : null;
    }

    /// <summary>Progress toward the effective limit, clamped to [0, 1]; null when no limit is known.</summary>
    public static double? ProgressFraction(Storage storage, double totalGb)
    {
        var limit = EffectiveLimit(storage);
        if (!limit.HasValue || limit.Value <= 0)
        {
            return null;
        }

        return Math.Clamp(totalGb / limit.Value, 0.0, 1.0);
    }
}
