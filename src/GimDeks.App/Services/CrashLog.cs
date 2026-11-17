using System.IO;
using GimDeks.Data;

namespace GimDeks.App.Services;

/// <summary>
/// Appends a redacted exception to gimdeks.log next to the database.
/// Drive-letter paths are stripped so the log never carries user file locations.
/// </summary>
public static class CrashLog
{
    private static string LogPath => DatabasePaths.ResolveLogPath();

    public static void Write(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(DatabasePaths.AppDataDirectory);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 1_000_000)
            {
                File.Move(LogPath, LogPath + ".old", overwrite: true);
            }

            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {RedactPaths(exception.ToString())}\n\n");
        }
        catch
        {
            // Never crash while reporting a crash.
        }
    }

    private static string RedactPaths(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"[A-Za-z]:\\[^\r\n""']*", "<path>");
}
