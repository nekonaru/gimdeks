using System.Diagnostics;
using System.IO;

namespace GimDeks.App.Services;

/// <summary>
/// Launches a locally installed game from a validated executable path.
/// No shell, no arguments, no paths from the internet: only a .exe file the user chose locally.
/// </summary>
public static class GameLauncherService
{
    public static bool IsValidExecutable(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)
        && File.Exists(path);

    public static bool Launch(string? path)
    {
        using var process = LaunchAndGetProcess(path);
        return process is not null;
    }

    /// <summary>Starts a validated local exe and returns the live process for session tracking.</summary>
    public static Process? LaunchAndGetProcess(string? path)
    {
        if (!IsValidExecutable(path))
        {
            return null;
        }

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = path!,
                WorkingDirectory = Path.GetDirectoryName(path!) ?? "",
                UseShellExecute = false,
            };
            return Process.Start(start);
        }
        catch
        {
            return null;
        }
    }
}
