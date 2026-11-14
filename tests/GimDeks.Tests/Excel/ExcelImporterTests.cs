using ClosedXML.Excel;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using GimDeks.Excel;
using Xunit;

namespace GimDeks.Tests.Excel;

public sealed class ExcelImporterTests : IDisposable
{
    private readonly string _tempDir;

    public ExcelImporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-excel-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private string CreateMiniWorkbook()
    {
        var path = Path.Combine(_tempDir, "mini.xlsx");
        using var workbook = new XLWorkbook();

        var hdd = workbook.Worksheets.Add("HDD Eksternal 1");
        hdd.Cell(1, 1).Value = "\U0001F4E6 HDD Eksternal 1: 1 TB  (Maks ≤ 900 GB)";
        hdd.Cell(2, 1).FormulaA1 = "\"  Total: \"&TEXT(C23,\"0.0\")&\" GB\"";
        hdd.Cell(3, 1).Value = "No";
        hdd.Cell(3, 2).Value = "Nama Game";
        hdd.Cell(3, 3).Value = "Ukuran (GB)";
        hdd.Cell(3, 4).Value = "Status";
        hdd.Cell(4, 1).Value = 1;
        hdd.Cell(4, 2).Value = "AI Limit";
        hdd.Cell(4, 3).Value = 16;
        hdd.Cell(4, 4).Value = "Belum Download";
        hdd.Cell(5, 1).Value = 2;
        hdd.Cell(5, 2).Value = "Outlast 2";
        hdd.Cell(5, 3).Value = 23.4;
        hdd.Cell(5, 4).Value = "Belum Download";
        hdd.Cell(6, 1).Value = 3;
        hdd.Cell(6, 2).Value = "Mad Max";
        hdd.Cell(6, 3).Value = 32.2;
        hdd.Cell(6, 4).Value = "Belum Download";
        hdd.Cell(7, 2).Value = "TOTAL";
        hdd.Cell(7, 3).FormulaA1 = "SUM(C4:C6)";

        var laptop = workbook.Worksheets.Add("Laptop HDD 1 (Done)");
        laptop.Cell(1, 1).Value = "Laptop: Laptop HDD 1 (Done)";
        laptop.Cell(2, 1).Value = "  Total: ~942.7 GB  |  2 game";
        laptop.Cell(3, 1).Value = "No";
        laptop.Cell(3, 2).Value = "Nama Game";
        laptop.Cell(3, 3).Value = "Ukuran (GB)";
        laptop.Cell(3, 4).Value = "Keterangan";
        laptop.Cell(4, 1).Value = 1;
        laptop.Cell(4, 2).Value = "Palworld";
        laptop.Cell(4, 3).Value = 40;
        laptop.Cell(4, 4).Value = "Sudah Ada";
        laptop.Cell(5, 1).Value = 2;
        laptop.Cell(5, 2).Value = "SimCity";
        laptop.Cell(5, 3).Value = 2.3;
        laptop.Cell(5, 4).Value = "Sudah Ada";
        laptop.Cell(6, 2).Value = "TOTAL";
        laptop.Cell(6, 3).FormulaA1 = "SUM(C4:C5)";

        var dikecualikan = workbook.Worksheets.Add("Dikecualikan");
        dikecualikan.Cell(1, 1).Value = "Game Dikecualikan (Tidak Dihitung dalam HDD)";
        dikecualikan.Cell(2, 1).Value = "No";
        dikecualikan.Cell(2, 2).Value = "Nama Game";
        dikecualikan.Cell(2, 3).Value = "Alasan";
        dikecualikan.Cell(3, 1).Value = 1;
        dikecualikan.Cell(3, 2).Value = "Albion Online";
        dikecualikan.Cell(3, 3).Value = "Online Game / BlueStacks";

        var dieliminasi = workbook.Worksheets.Add("Game Dieliminasi");
        dieliminasi.Cell(1, 1).Value = "Game Dieliminasi (Dikeluarkan dari Rencana Download)";
        dieliminasi.Cell(2, 1).Value = "No";
        dieliminasi.Cell(2, 2).Value = "Nama Game";
        dieliminasi.Cell(2, 3).Value = "Ukuran (GB)";
        dieliminasi.Cell(2, 4).Value = "Asal HDD / Alasan";
        dieliminasi.Cell(3, 1).Value = 1;
        dieliminasi.Cell(3, 2).Value = "Mario Kart 8 (Cemu Included)";
        dieliminasi.Cell(3, 3).Value = 4;
        dieliminasi.Cell(3, 4).Value = "Laptop HDD 3 (Done)";

        var unknown = workbook.Worksheets.Add("Catatan Pribadi");
        unknown.Cell(1, 1).Value = "bukan sheet migrasi";

        var ringkasan = workbook.Worksheets.Add("Ringkasan");
        ringkasan.Cell(1, 1).Value = "HDD / Kategori";
        ringkasan.Cell(1, 2).Value = "Jumlah Game";
        ringkasan.Cell(1, 3).Value = "Total Ukuran (GB)";
        ringkasan.Cell(2, 1).Value = "HDD Eksternal 1 (Rencana Download)";
        ringkasan.Cell(2, 2).FormulaA1 = "COUNTA('HDD Eksternal 1'!B4:B6)";
        ringkasan.Cell(2, 3).FormulaA1 = "'HDD Eksternal 1'!C7";
        ringkasan.Cell(3, 1).Value = "Laptop HDD 1 (Sudah Di-download)";
        ringkasan.Cell(3, 2).FormulaA1 = "COUNTA('Laptop HDD 1 (Done)'!B4:B5)";
        ringkasan.Cell(3, 3).FormulaA1 = "'Laptop HDD 1 (Done)'!C6";

        workbook.SaveAs(path);
        return path;
    }

    [Fact]
    public void PreviewDetectsStoragesGamesAndSkippedSheets()
    {
        var path = CreateMiniWorkbook();
        var preview = new ExcelImporter().Preview(path);

        Assert.Equal(2, preview.TotalStorages);
        Assert.Equal(7, preview.TotalGames); // 3 + 2 + 1 excluded + 1 eliminated
        Assert.Contains("Catatan Pribadi", preview.SkippedSheets);
        Assert.Equal(1000, preview.Storages[0].Storage.KapasitasGb);
        Assert.Equal(900, preview.Storages[0].Storage.BatasGb);
        Assert.Equal(71.6, preview.Storages[0].TotalGb, precision: 1); // 16 + 23.4 + 32.2

        var dikecualikan = preview.Games.Single(g => g.Game.Status == GameStatus.Dikecualikan);
        Assert.Null(dikecualikan.Game.StorageId);
        Assert.Equal("Online Game / BlueStacks", dikecualikan.Game.Alasan);

        var eliminated = preview.Games.Single(g => g.Game.Status == GameStatus.Dieliminasi);
        Assert.Equal("Laptop HDD 3 (Done)", eliminated.Game.AsalStorage);
    }

    [Fact]
    public void PreviewComparesAgainstRingkasan()
    {
        var path = CreateMiniWorkbook();
        var preview = new ExcelImporter().Preview(path);

        Assert.NotNull(preview.Ringkasan);
        var hddRow = preview.Ringkasan!.Rows.Single(r => r.StorageSheet == "HDD Eksternal 1");
        Assert.Equal(3, hddRow.ActualCount);
        Assert.Equal(71.6, hddRow.ActualGb, precision: 1);
        var dumpLine = $"expected {hddRow.ExpectedCount}/{hddRow.ExpectedGb}, actual {hddRow.ActualCount}/{hddRow.ActualGb}";
        Assert.True(hddRow.Matches, dumpLine);
        var dump = string.Join("\n", preview.Ringkasan!.Rows.Select(r =>
            $"{r.StorageSheet ?? r.Label}: expected {r.ExpectedCount}/{r.ExpectedGb:0.0}GB, actual {r.ActualCount}/{r.ActualGb:0.0}GB"));
        Assert.True(preview.Ringkasan.AllMatch, dump);
    }

    [Fact]
    public async Task ImportGantiSemuaWritesAllData()
    {
        var path = CreateMiniWorkbook();
        var dbPath = Path.Combine(_tempDir, "import-ganti.db");
        var (service, gameRepo, storageRepo, factory) = CreateService(dbPath);

        var result = await service.ImportAsync(path, ImportMode.GantiSemua);

        Assert.Equal(2, result.StoragesAdded);
        Assert.Equal(7, result.GamesAdded);
        Assert.Equal(0, result.GamesSkippedAsDuplicate);
        Assert.NotNull(result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));

        var games = await gameRepo.GetAllAsync();
        Assert.Equal(7, games.Count);
        var storages = await storageRepo.GetAllAsync();
        Assert.Equal(2, storages.Count);

        SqliteCleanup(factory);
    }

    [Fact]
    public async Task ImportGabungSkipsDuplicatesOnSecondRun()
    {
        var path = CreateMiniWorkbook();
        var dbPath = Path.Combine(_tempDir, "import-gabung.db");
        var (service, gameRepo, _, factory) = CreateService(dbPath);

        await service.ImportAsync(path, ImportMode.Gabung);
        var second = await service.ImportAsync(path, ImportMode.Gabung);

        Assert.Equal(0, second.GamesAdded);
        Assert.Equal(7, second.GamesSkippedAsDuplicate);
        Assert.Equal(7, (await gameRepo.GetAllAsync()).Count);

        SqliteCleanup(factory);
    }

    [Fact]
    public void ImportCompletesUnderFiveSeconds()
    {
        var path = CreateMiniWorkbook();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        new ExcelImporter().Preview(path);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"Import took {sw.Elapsed}");
    }

    private static (ExcelImportService Service, GameRepository GameRepo, StorageRepository StorageRepo, SqliteConnectionFactory Factory) CreateService(string dbPath)
    {
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();
        var gameRepo = new GameRepository(factory);
        var storageRepo = new StorageRepository(factory);
        var backup = new DatabaseBackupService(factory, Path.Combine(Path.GetDirectoryName(dbPath)!, "backups"));
        var service = new ExcelImportService(new ExcelImporter(), factory, storageRepo, backup);
        return (service, gameRepo, storageRepo, factory);
    }

    private static void SqliteCleanup(SqliteConnectionFactory factory) =>
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

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
