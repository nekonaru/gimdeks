using Dapper;
using GimDeks.Core.Interfaces;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using Xunit;

namespace GimDeks.Tests.Data;

public sealed class ExtendedDataTests : IDisposable
{
    private readonly string _tempDir;

    public ExtendedDataTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-v20-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void MigrationV4AddsExtendedTablesAndColumns()
    {
        var factory = new SqliteConnectionFactory(Path.Combine(_tempDir, "v4.db"));
        new DatabaseInitializer(factory).EnsureCreated();

        using var connection = factory.CreateConnection();
        var version = connection.ExecuteScalar<long>("PRAGMA user_version;");
        Assert.Equal(4, version);

        var tables = connection.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('sesi_main','catatan_game','gambar_game','preset_filter','histori_import','snapshot_kapasitas');").ToList();
        Assert.Equal(6, tables.Count);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    [Fact]
    public async Task FavoriteRatingAndSessionRoundtrip()
    {
        var factory = new SqliteConnectionFactory(Path.Combine(_tempDir, "round.db"));
        new DatabaseInitializer(factory).EnsureCreated();
        var games = new GameRepository(factory);
        var sessions = new PlaySessionRepository(factory);
        var notes = new GameNoteRepository(factory);

        var id = await games.AddAsync(new Core.Models.Game
        {
            Nama = "Extended Game",
            Status = Core.Models.GameStatus.SudahAda,
            Favorit = true,
            Rating = 4,
            Prioritas = 3,
            FolderPath = @"D:\Games\Extended",
            TotalDetik = 120,
        });

        var game = await games.GetByIdAsync(id);
        Assert.NotNull(game);
        Assert.True(game!.Favorit);
        Assert.Equal(4, game.Rating);
        Assert.Equal(3, game.Prioritas);
        Assert.Equal(@"D:\Games\Extended", game.FolderPath);
        Assert.Equal(120, game.TotalDetik);

        var sessionId = await sessions.StartAsync(id, "test");
        await sessions.EndAsync(sessionId, 90);
        var all = await sessions.GetByGameAsync(id);
        Assert.Single(all);
        Assert.Equal(90, all[0].DurasiDetik);
        var totals = await sessions.GetTotalsAsync();
        Assert.Equal(90, totals[id]);

        await notes.AddAsync(id, "A note");
        Assert.Single(await notes.GetByGameAsync(id));
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_tempDir, recursive: true);
    }
}
