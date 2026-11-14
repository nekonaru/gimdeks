using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using Xunit;

namespace GimDeks.Tests.Data;

public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public DatabaseInitializerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "test.db");
    }

    [Fact]
    public async Task InitializesSchemaAndSupportsRoundtrip()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        var initializer = new DatabaseInitializer(factory);

        initializer.EnsureCreated();

        var storageRepo = new StorageRepository(factory);
        var gameRepo = new GameRepository(factory);

        var storageId = await storageRepo.AddAsync(new Storage { Nama = "HDD Eksternal 1", Tipe = StorageType.HddEksternal, KapasitasGb = 1000, BatasGb = 900, Urutan = 1 });
        var gameId = await gameRepo.AddAsync(new Game { Nama = "AI Limit", UkuranGb = 16, Status = GameStatus.BelumDownload, StorageId = storageId });

        var storage = await storageRepo.GetByIdAsync(storageId);
        var games = await gameRepo.GetByStorageAsync(storageId);

        Assert.NotNull(storage);
        Assert.Equal(1000, storage!.KapasitasGb);
        Assert.Single(games);
        Assert.Equal(gameId, games[0].Id);
    }

    [Fact]
    public void EnsureCreatedIsIdempotent()
    {
        var factory = new SqliteConnectionFactory(_dbPath);
        var initializer = new DatabaseInitializer(factory);

        initializer.EnsureCreated();
        initializer.EnsureCreated();

        Assert.True(File.Exists(_dbPath));
    }

    public void Dispose()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup of temp files.
        }
    }
}
