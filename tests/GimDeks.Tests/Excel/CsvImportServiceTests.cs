using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using GimDeks.Excel;
using Xunit;

namespace GimDeks.Tests.Excel;

public sealed class CsvImportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public CsvImportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-csv-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ExportThenImportCsvRoundtripsGamesAndTags()
    {
        var dbPath = Path.Combine(_tempDir, "gimdeks.db");
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();
        var games = new GameRepository(factory);
        var storages = new StorageRepository(factory);
        var tags = new TagRepository(factory);

        await storages.AddAsync(new Storage
        {
            Nama = "HDD Satu",
            Tipe = StorageType.HddEksternal,
            Urutan = 0,
        });
        var gameId = await games.AddAsync(new Game
        {
            Nama = "Game Satu",
            Versi = "1.0",
            UkuranGb = 12.5,
            Status = GameStatus.SudahAda,
            StorageId = 1,
            SelesaiMain = true,
        });
        var tagId = await tags.EnsureAsync("RPG");
        await tags.SetTagsForGameAsync(gameId, new[] { tagId });

        var csvPath = Path.Combine(_tempDir, "catalog.csv");
        await new CsvExporter(games, storages, tags).ExportAsync(csvPath);

        var otherPath = Path.Combine(_tempDir, "other.db");
        var otherFactory = new SqliteConnectionFactory(otherPath);
        new DatabaseInitializer(otherFactory).EnsureCreated();
        var otherGames = new GameRepository(otherFactory);
        var otherStorages = new StorageRepository(otherFactory);
        var otherTags = new TagRepository(otherFactory);
        await otherStorages.AddAsync(new Storage
        {
            Nama = "HDD Satu",
            Tipe = StorageType.HddEksternal,
            Urutan = 0,
        });

        var service = new CsvImportService(otherGames, otherStorages, otherTags,
            new DatabaseBackupService(otherFactory, Path.Combine(_tempDir, "backups2")));
        var result = await service.ImportAsync(csvPath);

        Assert.Equal(1, result.Added);
        Assert.Equal(0, result.Skipped);
        var imported = Assert.Single(await otherGames.GetAllAsync());
        Assert.Equal("Game Satu", imported.Nama);
        Assert.Equal(12.5, imported.UkuranGb, 1);
        Assert.Equal(GameStatus.SudahAda, imported.Status);
        Assert.True(imported.SelesaiMain);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_tempDir, recursive: true);
    }
}
