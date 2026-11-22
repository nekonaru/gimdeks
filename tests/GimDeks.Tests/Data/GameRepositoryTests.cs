using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using Xunit;

namespace GimDeks.Tests.Data;

public sealed class GameRepositoryTests : IDisposable
{
    private readonly string _tempDir;

    public GameRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-repository-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task MoveToStorageBatchMovesGamesAndRecordsHistory()
    {
        var dbPath = Path.Combine(_tempDir, "move.db");
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();
        var storages = new StorageRepository(factory);
        var games = new GameRepository(factory);
        var history = new HistoryRepository(factory);

        var first = await storages.AddAsync(new Storage { Nama = "A", Tipe = StorageType.HddEksternal });
        var second = await storages.AddAsync(new Storage { Nama = "B", Tipe = StorageType.HddEksternal });
        var game1 = await games.AddAsync(new Game { Nama = "Game 1", Status = GameStatus.BelumDownload, StorageId = first });
        var game2 = await games.AddAsync(new Game { Nama = "Game 2", Status = GameStatus.BelumDownload, StorageId = first });

        await games.MoveToStorageAsync(new[] { game1, game2 }, second);

        Assert.All(await games.GetAllAsync(), game => Assert.Equal(second, game.StorageId));
        var entries = await history.GetAllAsync();
        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry =>
        {
            Assert.Equal(first, entry.DariStorageId);
            Assert.Equal(second, entry.KeStorageId);
        });

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
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
            // Best effort cleanup.
        }
    }
}
