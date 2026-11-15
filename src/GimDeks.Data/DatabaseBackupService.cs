using Microsoft.Data.Sqlite;

namespace GimDeks.Data;

/// <summary>
/// Copies the database file to a timestamped backup and restores from one.
/// The current database is backed up automatically before any restore.
/// Old backups are pruned automatically (keeps the newest 20).
/// </summary>
public sealed class DatabaseBackupService
{
    private const int MaxBackups = 20;

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
        await target.OpenAsync(ct);
        source.BackupDatabase(target);
        PruneBackups();
        return destination;
    }

    public async Task RestoreAsync(string backupFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("The backup file was not found.", backupFilePath);
        }

        ValidateBackup(backupFilePath);

        // Always keep a fresh copy of the current state before overwriting.
        await BackupAsync(ct);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // Copy to a temp file in the same folder, then swap atomically; a plain
        // overwrite can leave a half-written database behind.
        var directory = Path.GetDirectoryName(_factory.DatabasePath)!;
        var temp = Path.Combine(directory, Path.GetFileName(_factory.DatabasePath) + ".restore-tmp");
        File.Copy(backupFilePath, temp, overwrite: true);
        try
        {
            File.Replace(temp, _factory.DatabasePath, null);
        }
        catch (PlatformNotSupportedException)
        {
            File.Copy(temp, _factory.DatabasePath, overwrite: true);
            File.Delete(temp);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { /* best effort */ }
            throw;
        }
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

    private void PruneBackups()
    {
        var backups = ListBackups();
        foreach (var old in backups.Skip(MaxBackups))
        {
            try
            {
                File.Delete(old);
            }
            catch
            {
                // A locked or already-deleted backup is not fatal.
            }
        }
    }

    private static void ValidateBackup(string backupFilePath)
    {
        using var check = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = backupFilePath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString());
        check.Open();

        using var integrity = check.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check;";
        var result = integrity.ExecuteScalar() as string;
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The backup file is damaged (integrity_check failed). Restore was cancelled.");
        }

        using var tables = check.CreateCommand();
        tables.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name IN ('storage', 'game', 'riwayat');";
        var count = Convert.ToInt32(tables.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        if (count != 3)
        {
            throw new InvalidDataException("This file is not a GimDeks database. Restore was cancelled.");
        }
    }
}
