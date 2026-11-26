using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.Core.Rules;

/// <summary>A suggested link between a collection game and a local executable.</summary>
/// <param name="Game">The collection game that has no executable path yet.</param>
/// <param name="Executable">The local executable found by a scan.</param>
/// <param name="IsExact">True when the normalized names are identical.</param>
public sealed record ExecutableMatch(Game Game, ScannedExecutable Executable, bool IsExact);

/// <summary>
/// Pairs games without a launch path to scanned local executables by normalized name.
/// Exact pairs win first; a contained name is accepted only when the shorter side
/// has at least five letters or digits, so short words cannot cause false matches.
/// One game and one executable are used at most once.
/// </summary>
public static class GameExecutableMatcher
{
    private const int MinimumContainedLength = 5;

    public static IReadOnlyList<ExecutableMatch> Match(
        IReadOnlyList<Game> games,
        IReadOnlyList<ScannedExecutable> executables)
    {
        if (games.Count == 0 || executables.Count == 0)
        {
            return Array.Empty<ExecutableMatch>();
        }

        var candidates = games
            .Where(g => string.IsNullOrWhiteSpace(g.PathExe))
            .Select(g => (Game: g, Key: GameNameNormalizer.Normalize(g.Nama)))
            .Where(x => x.Key.Length > 0)
            .ToList();
        var scanned = executables
            .Where(e => e.NormalizedName.Length > 0)
            .ToList();

        var exact = new List<ExecutableMatch>();
        var partial = new List<ExecutableMatch>();
        var usedGames = new HashSet<long>();
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var best = scanned
                .Where(e => !usedPaths.Contains(e.Path))
                .FirstOrDefault(e => string.Equals(e.NormalizedName, candidate.Key, StringComparison.OrdinalIgnoreCase));
            if (best is not null)
            {
                exact.Add(new ExecutableMatch(candidate.Game, best, true));
                usedGames.Add(candidate.Game.Id);
                usedPaths.Add(best.Path);
            }
        }

        foreach (var candidate in candidates.Where(c => !usedGames.Contains(c.Game.Id)))
        {
            var best = scanned
                .Where(e => !usedPaths.Contains(e.Path))
                .FirstOrDefault(e => IsContainedMatch(candidate.Key, e.NormalizedName));
            if (best is not null)
            {
                partial.Add(new ExecutableMatch(candidate.Game, best, false));
                usedGames.Add(candidate.Game.Id);
                usedPaths.Add(best.Path);
            }
        }

        exact.AddRange(partial);
        return exact;
    }

    private static bool IsContainedMatch(string gameKey, string exeKey)
    {
        var shorter = gameKey.Length <= exeKey.Length ? gameKey : exeKey;
        var longer = gameKey.Length <= exeKey.Length ? exeKey : gameKey;
        return shorter.Length >= MinimumContainedLength
            && longer.Contains(shorter, StringComparison.OrdinalIgnoreCase);
    }
}
