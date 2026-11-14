using Microsoft.Data.Sqlite;

namespace GimDeks.Data;

/// <summary>
/// Copies the database file to a timestamped backup and restores from one.
/// The current database is backed up automatically before any restore.
/// </summary>
public sealed class DatabaseBackupService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly string _backupsDirectory;

    public DatabaseBackupService(SqliteConnectionFactory factory, string? backupsDirectory = null)
    {
        _factory = factory;
        _backupsDirectory = backupsDirectory ?? DatabasePaths.ResolveBackupsDirectory();
    }

    public string BackupsDirectory => _backupsDirectory;

    public async Task<string> BackupAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_backupsDirectory);
        var destination = Path.Combine(
            _backupsDirectory,
            $"gimdeks-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        await using var source = _factory.CreateConnection();
        await using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination }.ToString());
        await source.OpenAsync(ct);
        await target.OpenAsync(ct);
        source.BackupDatabase(target);
        return destination;
    }

    public async Task RestoreAsync(string backupFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("File backup tidak ditemukan.", backupFilePath);
        }

        // Always keep a fresh copy of the current state before overwriting.
        await BackupAsync(ct);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Copy(backupFilePath, _factory.DatabasePath, overwrite: true);
    }

    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(_backupsDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(_backupsDirectory, "gimdeks-backup-*.db")
            .OrderByDescending(File.GetCreationTimeUtc)
            .ToList();
    }
}
