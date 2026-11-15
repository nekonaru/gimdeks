namespace GimDeks.Data;

/// <summary>
/// Resolves where the single SQLite database lives and where backups go.
/// Search order: the folder next to the executable (portable mode), then
/// the GIMDEKS_HOME environment variable, then LOCALAPPDATA.
/// Portable mode is enabled by creating a "data" folder next to the exe.
/// </summary>
public static class DatabasePaths
{
    public static string DatabaseFileName => "gimdeks.db";

    /// <summary>Portable data folder next to the executable; null when not portable.</summary>
    public static string? PortableRoot => FindPortableRoot();

    private static string? FindPortableRoot()
    {
        try
        {
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
            if (!string.IsNullOrEmpty(exeDir) && Directory.Exists(Path.Combine(exeDir, "data")))
            {
                return Path.Combine(exeDir, "data");
            }
        }
        catch
        {
            // Fall through to the user profile location.
        }

        return null;
    }

    public static string AppDataDirectory =>
        EffectiveRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GimDeks");

    public static string DatabasePath => Path.Combine(AppDataDirectory, DatabaseFileName);

    public static string BackupsDirectory => Path.Combine(AppDataDirectory, "backups");

    /// <summary>Test hook: point every path at a custom root (e.g. a temp folder).</summary>
    public static string? OverrideRoot { get; set; }

    private static string? EffectiveRoot => OverrideRoot ?? Environment.GetEnvironmentVariable("GIMDEKS_HOME") ?? PortableRoot;

    public static string ResolveDatabasePath() =>
        EffectiveRoot is null ? DatabasePath : Path.Combine(EffectiveRoot, DatabaseFileName);

    public static string ResolveBackupsDirectory() =>
        EffectiveRoot is null ? BackupsDirectory : Path.Combine(EffectiveRoot, "backups");

    public static string ResolveSettingsPath() =>
        EffectiveRoot is null ? Path.Combine(DatabasePaths.AppDataDirectory, "settings.json") : Path.Combine(EffectiveRoot, "settings.json");

    public static string ResolveLogPath() =>
        EffectiveRoot is null ? Path.Combine(DatabasePaths.AppDataDirectory, "gimdeks.log") : Path.Combine(EffectiveRoot, "gimdeks.log");
}
