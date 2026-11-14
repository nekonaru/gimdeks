using System.Globalization;
using ClosedXML.Excel;
using GimDeks.Core.Models;

namespace GimDeks.Excel;

/// <summary>
/// Writes the collection back to an .xlsx that mirrors the original layout:
/// one sheet per storage with a live summary formula, plus Ringkasan,
/// Dikecualikan and Game Dieliminasi. Total formulas always match the
/// number of data rows at export time.
/// </summary>
public sealed class ExcelExporter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public void Export(string filePath, IReadOnlyList<Storage> storages, IReadOnlyList<Game> games)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var workbook = new XLWorkbook();

        var gamesByStorage = games
            .Where(g => g.StorageId is not null)
            .GroupBy(g => g.StorageId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Urutan).ThenBy(x => x.Nama, StringComparer.CurrentCultureIgnoreCase).ToList());

        var excluded = games.Where(g => g.Status == GameStatus.Dikecualikan)
            .OrderBy(g => g.Urutan).ThenBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase).ToList();
        var eliminated = games.Where(g => g.Status == GameStatus.Dieliminasi)
            .OrderBy(g => g.Urutan).ThenBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase).ToList();

        var orderedStorages = storages.OrderBy(s => s.Urutan).ThenBy(s => s.Nama, StringComparer.CurrentCultureIgnoreCase).ToList();
        foreach (var storage in orderedStorages)
        {
            gamesByStorage.TryGetValue(storage.Id, out var storageGames);
            storageGames ??= new List<Game>();
            WriteStorageSheet(workbook, storage, storageGames);
        }

        WriteExcludedSheet(workbook, excluded);
        WriteEliminatedSheet(workbook, eliminated);
        WriteRingkasanSheet(workbook, orderedStorages, gamesByStorage, excluded);

        workbook.SaveAs(filePath, new SaveOptions { EvaluateFormulasBeforeSaving = true });
    }

    private static string SheetName(string name) =>
        name.Length <= 31 ? name : name[..31];

    private static (int FirstDataRow, int TotalRow) WriteStorageSheet(XLWorkbook workbook, Storage storage, IReadOnlyList<Game> games)
    {
        var sheetName = SheetName(storage.Nama);
        var sheet = workbook.Worksheets.Add(sheetName);
        var isHdd = storage.Tipe == StorageType.HddEksternal;
        var totalRow = games.Count + 4;

        sheet.Cell(1, 1).Value = BuildTitle(storage);
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        if (isHdd && storage.BatasGb is { } batas)
        {
            sheet.Cell(2, 1).FormulaA1 =
                $"=\"  Total: \"&TEXT(C{totalRow},\"0.0\")&\" GB  |  \"&COUNTA(B4:B{totalRow - 1})&\" game  |  \"&IF(C{totalRow}<={batas.ToString(Invariant)},\"Dalam batas\",\"Melebihi batas {batas.ToString(Invariant)} GB\")";
        }
        else
        {
            sheet.Cell(2, 1).FormulaA1 =
                $"=\"  Total: ~\"&TEXT(C{totalRow},\"0.0\")&\" GB  |  \"&COUNTA(B4:B{totalRow - 1})&\" game\"";
        }

        sheet.Cell(2, 1).Style.Font.Italic = true;
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        var headerRow = 3;
        WriteHeaderRow(sheet, headerRow, TextForFourthColumn(storage));

        for (var index = 0; index < games.Count; index++)
        {
            var row = 4 + index;
            var game = games[index];
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = game.Nama;
            sheet.Cell(row, 3).Value = game.UkuranGb;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "0.0";
            sheet.Cell(row, 4).Value = TextForStatus(game.Status);
            if (index % 2 == 1)
            {
                sheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 245, 244);
            }
        }

        sheet.Cell(totalRow, 2).Value = "TOTAL";
        sheet.Cell(totalRow, 2).Style.Font.Bold = true;
        sheet.Cell(totalRow, 3).FormulaA1 = games.Count > 0 ? $"SUM(C4:C{totalRow - 1})" : "0";
        sheet.Cell(totalRow, 3).Style.Font.Bold = true;
        sheet.Cell(totalRow, 3).Style.NumberFormat.Format = "0.0";

        sheet.Column(1).Width = 5;
        sheet.Column(2).Width = 62;
        sheet.Column(3).Width = 14;
        sheet.Column(4).Width = 16;
        sheet.SheetView.FreezeRows(3);

        return (4, totalRow);
    }

    private static void WriteHeaderRow(IXLWorksheet sheet, int row, string fourthHeader)
    {
        var headers = new[] { "No", "Nama Game", "Ukuran (GB)", fourthHeader };
        for (var column = 1; column <= headers.Length; column++)
        {
            var cell = sheet.Cell(row, column);
            cell.Value = headers[column - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 18, 38);
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
    }

    private static string TextForFourthColumn(Storage storage) =>
        storage.Tipe == StorageType.HddEksternal ? "Status" : "Keterangan";

    private static string TextForStatus(GameStatus status) => status switch
    {
        GameStatus.BelumDownload => "Belum Download",
        GameStatus.SudahAda => "Sudah Ada",
        GameStatus.Dikecualikan => "Dikecualikan",
        GameStatus.Dieliminasi => "Dieliminasi",
        _ => status.ToString(),
    };

    private static string BuildTitle(Storage storage)
    {
        if (storage.Tipe == StorageType.HddEksternal)
        {
            var kapasitasTb = storage.KapasitasGb is { } cap ? cap / 1000.0 : 1.0;
            var batas = storage.BatasGb is { } b ? $"  (Maks <= {b.ToString(Invariant)} GB)" : "";
            return $"{storage.Nama}: {kapasitasTb.ToString("0.#", Invariant)} TB{batas}";
        }

        if (storage.Tipe is StorageType.LaptopHdd or StorageType.LaptopSsd or StorageType.Virtual)
        {
            return $"Laptop: {storage.Nama}";
        }

        return storage.Nama;
    }

    private static void WriteExcludedSheet(XLWorkbook workbook, IReadOnlyList<Game> excluded)
    {
        var sheet = workbook.Worksheets.Add("Dikecualikan");
        sheet.Cell(1, 1).Value = "Game Dikecualikan (Tidak Dihitung dalam HDD)";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        var headers = new[] { "No", "Nama Game", "Alasan" };
        for (var column = 1; column <= headers.Length; column++)
        {
            var cell = sheet.Cell(2, column);
            cell.Value = headers[column - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 18, 38);
        }

        for (var index = 0; index < excluded.Count; index++)
        {
            var row = 3 + index;
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = excluded[index].Nama;
            sheet.Cell(row, 3).Value = excluded[index].Alasan ?? "";
        }

        sheet.Column(1).Width = 5;
        sheet.Column(2).Width = 72;
        sheet.Column(3).Width = 48;
        sheet.SheetView.FreezeRows(2);
    }

    private static void WriteEliminatedSheet(XLWorkbook workbook, IReadOnlyList<Game> eliminated)
    {
        var sheet = workbook.Worksheets.Add("Game Dieliminasi");
        sheet.Cell(1, 1).Value = "Game Dieliminasi (Dikeluarkan dari Rencana Download)";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        var headers = new[] { "No", "Nama Game", "Ukuran (GB)", "Asal HDD / Alasan" };
        for (var column = 1; column <= headers.Length; column++)
        {
            var cell = sheet.Cell(2, column);
            cell.Value = headers[column - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 18, 38);
        }

        for (var index = 0; index < eliminated.Count; index++)
        {
            var row = 3 + index;
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = eliminated[index].Nama;
            sheet.Cell(row, 3).Value = eliminated[index].UkuranGb;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "0.0";
            sheet.Cell(row, 4).Value = eliminated[index].AsalStorage ?? eliminated[index].Alasan ?? "";
        }

        sheet.Column(1).Width = 5;
        sheet.Column(2).Width = 62;
        sheet.Column(3).Width = 14;
        sheet.Column(4).Width = 56;
        sheet.SheetView.FreezeRows(2);
    }

    private static void WriteRingkasanSheet(
        XLWorkbook workbook,
        IReadOnlyList<Storage> storages,
        IReadOnlyDictionary<long, List<Game>> gamesByStorage,
        IReadOnlyList<Game> excluded)
    {
        var sheet = workbook.Worksheets.Add("Ringkasan");
        sheet.Cell(1, 1).Value = "HDD / Kategori";
        sheet.Cell(1, 2).Value = "Jumlah Game";
        sheet.Cell(1, 3).Value = "Total Ukuran (GB)";
        for (var column = 1; column <= 3; column++)
        {
            var cell = sheet.Cell(1, column);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 18, 38);
        }

        var row = 2;
        foreach (var storage in storages)
        {
            var labelSuffix = storage.Tipe switch
            {
                StorageType.HddEksternal => "(Rencana Download)",
                StorageType.LaptopHdd => "(Sudah Di-download)",
                StorageType.LaptopSsd => "(Sudah Ada)",
                _ => "",
            };
            gamesByStorage.TryGetValue(storage.Id, out var list);
            list ??= new List<Game>();
            var count = list.Count;
            var dataEnd = count + 3;
            var totalRow = count + 4;
            var sheetRef = $"'{SheetName(storage.Nama)}'";

            sheet.Cell(row, 1).Value = $"{storage.Nama} {labelSuffix}".TrimEnd();
            sheet.Cell(row, 2).FormulaA1 = $"COUNTA({sheetRef}!B4:B{dataEnd})";
            sheet.Cell(row, 3).FormulaA1 = $"{sheetRef}!C{totalRow}";
            sheet.Cell(row, 2).Style.NumberFormat.Format = "0";
            sheet.Cell(row, 3).Style.NumberFormat.Format = "0.0";
            row++;
        }

        sheet.Cell(row, 1).Value = "Dikecualikan (Online/Mobile/Free)";
        sheet.Cell(row, 2).Value = excluded.Count;
        sheet.Cell(row, 3).Value = "-";

        sheet.Column(1).Width = 52;
        sheet.Column(2).Width = 16;
        sheet.Column(3).Width = 20;
        sheet.SheetView.FreezeRows(1);
    }

}
