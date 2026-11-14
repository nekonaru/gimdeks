using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using GimDeks.Excel;
using Xunit;

namespace GimDeks.Tests.Excel;

public sealed class ExcelExporterTests : IDisposable
{
    private readonly string _tempDir;

    public ExcelExporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-export-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private async Task SeedAsync(string dbPath)
    {
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();
        var source = CreateSourceWorkbook();
        var backupDir = Path.Combine(_tempDir, "backups-" + Guid.NewGuid().ToString("N"));
        var service = new ExcelImportService(
            new ExcelImporter(),
            factory,
            new StorageRepository(factory),
            new DatabaseBackupService(factory, backupDir));
        await service.ImportAsync(source, ImportMode.GantiSemua);
    }

    private string CreateSourceWorkbook()
    {
        var path = Path.Combine(_tempDir, "source-" + Guid.NewGuid().ToString("N") + ".xlsx");
        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var hdd = workbook.Worksheets.Add("HDD Eksternal 1");
        hdd.Cell(1, 1).Value = "HDD Eksternal 1: 1 TB  (Maks <= 900 GB)";
        hdd.Cell(3, 1).Value = "No";
        hdd.Cell(3, 2).Value = "Nama Game";
        hdd.Cell(3, 3).Value = "Ukuran (GB)";
        hdd.Cell(3, 4).Value = "Status";
        hdd.Cell(4, 1).Value = 1;
        hdd.Cell(4, 2).Value = "Alpha";
        hdd.Cell(4, 3).Value = 100;
        hdd.Cell(4, 4).Value = "Belum Download";
        hdd.Cell(5, 1).Value = 2;
        hdd.Cell(5, 2).Value = "Beta";
        hdd.Cell(5, 3).Value = 50.5;
        hdd.Cell(5, 4).Value = "Belum Download";

        var dikecualikan = workbook.Worksheets.Add("Dikecualikan");
        dikecualikan.Cell(2, 1).Value = "No";
        dikecualikan.Cell(2, 2).Value = "Nama Game";
        dikecualikan.Cell(2, 3).Value = "Alasan";
        dikecualikan.Cell(3, 1).Value = 1;
        dikecualikan.Cell(3, 2).Value = "Albion Online";
        dikecualikan.Cell(3, 3).Value = "Online";

        var dieliminasi = workbook.Worksheets.Add("Game Dieliminasi");
        dieliminasi.Cell(2, 1).Value = "No";
        dieliminasi.Cell(2, 2).Value = "Nama Game";
        dieliminasi.Cell(2, 3).Value = "Ukuran (GB)";
        dieliminasi.Cell(2, 4).Value = "Asal HDD / Alasan";
        dieliminasi.Cell(3, 1).Value = 1;
        dieliminasi.Cell(3, 2).Value = "Deleted Game";
        dieliminasi.Cell(3, 3).Value = 10;
        dieliminasi.Cell(3, 4).Value = "HDD Eksternal 1";

        workbook.SaveAs(path);
        return path;
    }

    [Fact]
    public async Task ExportThenImportYieldsSameData()
    {
        var dbPath = Path.Combine(_tempDir, "roundtrip.db");
        await SeedAsync(dbPath);

        var exportPath = Path.Combine(_tempDir, "export.xlsx");
        var factory = new SqliteConnectionFactory(dbPath);
        var storages = await new StorageRepository(factory).GetAllAsync();
        var games = await new GameRepository(factory).GetAllAsync();

        new ExcelExporter().Export(exportPath, storages, games);
        Assert.True(File.Exists(exportPath));

        var reimportedPath = Path.Combine(_tempDir, "reimport.db");
        var reFactory = new SqliteConnectionFactory(reimportedPath);
        new DatabaseInitializer(reFactory).EnsureCreated();
        var backupDir = Path.Combine(_tempDir, "backups-roundtrip");
        var service = new ExcelImportService(
            new ExcelImporter(),
            reFactory,
            new StorageRepository(reFactory),
            new DatabaseBackupService(reFactory, backupDir));

        var preview = service.Preview(exportPath);
        Assert.True(preview.Ringkasan?.AllMatch ?? false, "Ringkasan selisih setelah export");

        await service.ImportAsync(exportPath, ImportMode.GantiSemua);

        var reimportedGames = await new GameRepository(reFactory).GetAllAsync();
        var original = games.OrderBy(g => g.Nama).Select(g => (g.Nama, g.UkuranGb, g.Status)).ToList();
        var restored = reimportedGames.OrderBy(g => g.Nama).Select(g => (g.Nama, g.UkuranGb, g.Status)).ToList();

        Assert.Equal(original.Count, restored.Count);
        foreach (var pair in original.Zip(restored))
        {
            Assert.Equal(pair.First.Nama, pair.Second.Nama, ignoreCase: true);
            Assert.Equal(pair.First.UkuranGb, pair.Second.UkuranGb, precision: 1);
            Assert.Equal(pair.First.Status, pair.Second.Status);
        }

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
