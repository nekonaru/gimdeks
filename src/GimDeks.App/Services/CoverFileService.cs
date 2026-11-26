using System.IO;

namespace GimDeks.App.Services;

/// <summary>
/// Saves or removes cover images (any common web image format) in the local covers folder.
/// Files are named by game id, for example 42.jpg, and a saved cover replaces any previous one.
/// </summary>
public sealed class CoverFileService
{
    public static readonly string[] SupportedCoverExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static string CoverDirectory =>
        Path.Combine(GimDeks.Data.DatabasePaths.AppDataDirectory, "covers");

    public static string GalleryDirectory(long gameId) =>
        Path.Combine(CoverDirectory, gameId + "-gallery");

    public string SaveCover(long gameId, string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedCoverExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            extension = ".png";
        }

        Directory.CreateDirectory(CoverDirectory);
        RemoveCover(gameId);
        var destination = Path.Combine(CoverDirectory, gameId + extension);
        File.Copy(sourcePath, destination, overwrite: true);
        CoverPathCache.Refresh(gameId);
        return destination;
    }

    public bool RemoveCover(long gameId)
    {
        if (!Directory.Exists(CoverDirectory))
        {
            return false;
        }

        var removed = false;
        foreach (var candidate in Directory.EnumerateFiles(CoverDirectory, gameId + ".*"))
        {
            try
            {
                File.Delete(candidate);
                removed = true;
            }
            catch
            {
                // Best effort: an undeletable cover just stays on disk.
            }
        }

        if (removed)
        {
            CoverPathCache.Refresh(gameId);
        }

        return removed;
    }

    public static bool IsCoverImage(string path) =>
        File.Exists(path) && SupportedCoverExtensions.Contains(Path.GetExtension(path).ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
}
