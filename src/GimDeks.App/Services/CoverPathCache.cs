using System.IO;

namespace GimDeks.App.Services;

/// <summary>
/// Single in-memory index of the covers folder. Avoids re-scanning the directory
/// on every rendered row; updates are applied only when a cover changes.
/// </summary>
public static class CoverPathCache
{
    private static readonly object Gate = new();
    private static Dictionary<long, string>? _paths;
    private static string? _directory;

    public static string? GetPath(long gameId)
    {
        lock (Gate)
        {
            EnsureIndex();
            return _paths!.TryGetValue(gameId, out var path) ? path : null;
        }
    }

    /// <summary>Recompute only one game id after a save/delete, without a full folder scan.</summary>
    public static void Refresh(long gameId)
    {
        lock (Gate)
        {
            EnsureIndex();
            _paths!.Remove(gameId);
            var directory = _directory!;
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(directory, gameId + ".*"))
            {
                if (SteamCoverService.IsCoverExtension(file))
                {
                    _paths[gameId] = file;
                    return;
                }
            }
        }
    }

    private static void EnsureIndex()
    {
        var directory = Path.Combine(GimDeks.Data.DatabasePaths.AppDataDirectory, "covers");
        if (_paths is not null && string.Equals(_directory, directory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _directory = directory;
        var paths = new Dictionary<long, string>();
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.*"))
            {
                if (!SteamCoverService.IsCoverExtension(file))
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(file);
                if (long.TryParse(name, out var id))
                {
                    paths.TryAdd(id, file);
                }
            }
        }

        _paths = paths;
    }
}
