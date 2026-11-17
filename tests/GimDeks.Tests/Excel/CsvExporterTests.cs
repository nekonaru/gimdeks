using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using GimDeks.Excel;
using Xunit;

namespace GimDeks.Tests.Excel;

public sealed class CsvExporterTests : IDisposable
{
    private readonly string _tempDir;

    public CsvExporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gimdeks-csv-export-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private async Task SeedAsync(string dbPath)
    {
        var factory = new SqliteConnectionFactory(dbPath);
        new DatabaseInitializer(factory).EnsureCreated();

        var source = Path.Combine(_tempDir, "source-" + Guid.NewGuid().ToString("N") + ".xlsx");
        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
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
            hdd.Cell(5, 2).Value = "Gamma; With Semi";
            hdd.Cell(5, 3).Value = 50.5;
            hdd.Cell(5, 4).Value = "Belum Download";
            workbook.SaveAs(source);
        }

        var backupDir = Path.Combine(_tempDir, "backups-" + Guid.NewGuid().ToString("N"));
        var service = new ExcelImportService(
            new ExcelImporter(),
            factory,
            new StorageRepository(factory),
            new DatabaseBackupService(factory, backupDir));
        await service.ImportAsync(source, ImportMode.GantiSemua);
    }

    [Fact]
    public async Task ExportWritesBomHeaderAndRows()
    {
        var dbPath = Path.Combine(_tempDir, "csv-seed.db");
        await SeedAsync(dbPath);

        var csvPath = Path.Combine(_tempDir, "out.csv");
        var factory = new SqliteConnectionFactory(dbPath);
        var exporter = new CsvExporter(
            new GameRepository(factory),
            new StorageRepository(factory),
            new TagRepository(factory));

        await exporter.ExportAsync(csvPath);

        Assert.True(File.Exists(csvPath));
        var lines = await File.ReadAllLinesAsync(csvPath);
        Assert.StartsWith("Nama;Versi;Ukuran (GB);Status;Storage;Tag;Selesai", lines[0]);
        Assert.Equal(3, lines.Length);
        Assert.Contains(lines, line => line.StartsWith("Alpha;", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("\"Gamma; With Semi\"", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("HDD Eksternal 1", StringComparison.Ordinal));
        var bytes = await File.ReadAllBytesAsync(csvPath);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
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
