using GimDeks.Data;
using Xunit;

namespace GimDeks.Tests.Data;

public sealed class DatabaseBackupServiceTests : IDisposable
{
    private readonly string _tempDir;

    public DatabaseBackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-backup-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task RapidBackupsGetDistinctFileNames()
    {
        var dbPath = Path.Combine(_tempDir, "gimdeks.db");
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();
        var backups = new DatabaseBackupService(factory, Path.Combine(_tempDir, "backups"));

        var first = await backups.BackupAsync();
        var second = await backups.BackupAsync();

        Assert.NotEqual(first, second);
        Assert.Equal(2, backups.ListBackups().Count);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    [Fact]
    public async Task PruneKeepsTheProtectedBackupEvenWhenItIsTheOldest()
    {
        var dbPath = Path.Combine(_tempDir, "gimdeks.db");
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();
        var backupsDirectory = Path.Combine(_tempDir, "backups");
        var backups = new DatabaseBackupService(factory, backupsDirectory);

        // Build 20 backups, so the next one pushes the oldest past the limit.
        for (var i = 0; i < 20; i++)
        {
            await backups.BackupAsync();
            await Task.Delay(5);
        }
        var oldest = backups.ListBackups().Last();

        await backups.BackupAsync(protectPath: oldest);

        Assert.True(File.Exists(oldest), "The protected backup must survive pruning.");
        // Protecting one extra file means the folder can hold 21 snapshots briefly.
        Assert.Equal(21, backups.ListBackups().Count);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_tempDir, recursive: true);
    }
}
