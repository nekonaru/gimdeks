namespace GimDeks.Data;

/// <summary>
/// Resolves where the single SQLite database lives and where backups go.
/// </summary>
public static class DatabasePaths
{
    public static string DatabaseFileName => "gimdeks.db";

    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GimDeks");

    public static string DatabasePath => Path.Combine(AppDataDirectory, DatabaseFileName);

    public static string BackupsDirectory => Path.Combine(AppDataDirectory, "backups");

    /// <summary>Test hook: point every path at a custom root (e.g. a temp folder).</summary>
    public static string? OverrideRoot { get; set; }

    public static string ResolveDatabasePath() =>
        OverrideRoot is null ? DatabasePath : Path.Combine(OverrideRoot, DatabaseFileName);

    public static string ResolveBackupsDirectory() =>
        OverrideRoot is null ? BackupsDirectory : Path.Combine(OverrideRoot, "backups");
}
