using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.Excel;

/// <summary>
/// Reads the Koleksi_Game.xlsx layout by content patterns, not fixed cell positions.
/// Never writes to the workbook; parsing is pure and side effect free.
/// </summary>
public sealed partial class ExcelImporter
{
    public ImportPreview Preview(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The Excel file was not found.", filePath);
        }

        const long maxBytes = 50L * 1024 * 1024; // 50 MB is far above any sane collection file
        var fileSize = new FileInfo(filePath).Length;
        if (fileSize > maxBytes)
        {
            throw new InvalidDataException($"File Excel terlalu besar ({fileSize / (1024 * 1024)} MB). Batas maksimal 50 MB.");
        }

        using var workbook = new XLWorkbook(filePath);

        var games = new List<GamePreview>();
        var storages = new List<StoragePreview>();
        var skipped = new List<string>();
        var warnings = new List<string>();
        RingkasanComparison? ringkasan = null;

        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetName = worksheet.Name.Trim();

            if (IsRingkasan(sheetName))
            {
                continue; // counted formulas only feed the verification pass
            }

            var headerRow = FindHeaderRow(worksheet);
            if (headerRow is null)
            {
                skipped.Add(sheetName);
                continue;
            }

            var (namaCol, ukuranCol, thirdCol, headerHasAlasan, headerHasAsal) = AnalyzeHeader(worksheet, headerRow.Value);

            if (TryGetStorageClassification(sheetName, out var classification))
            {
                var storage = BuildStorage(worksheet, sheetName, classification, storages.Count, warnings);
                var storageGames = ReadRows(workbook, worksheet, headerRow.Value, namaCol, ukuranCol, thirdCol, classification.Status, null, null, sheetName, warnings);

                storages.Add(new StoragePreview
                {
                    SheetName = sheetName,
                    Storage = storage,
                    GameCount = storageGames.Count,
                    TotalGb = storageGames.Sum(g => g.Game.UkuranGb),
                });
                games.AddRange(storageGames);
                continue;
            }

            if (IsDikecualikan(sheetName, headerHasAlasan))
            {
                var rows = ReadRows(workbook, worksheet, headerRow.Value, namaCol, ukuranCol, thirdCol, GameStatus.Dikecualikan, alasanCol: thirdCol, asalStorageCol: null, sheetName, warnings);
                games.AddRange(rows);
                continue;
            }

            if (IsDieliminasi(sheetName, headerHasAsal))
            {
                var rows = ReadRows(workbook, worksheet, headerRow.Value, namaCol, ukuranCol, thirdCol, GameStatus.Dieliminasi, alasanCol: thirdCol, asalStorageCol: thirdCol, sheetName, warnings);
                games.AddRange(rows);
                continue;
            }

            skipped.Add(sheetName);
        }

        var ringkasanSheet = workbook.Worksheets.FirstOrDefault(w => IsRingkasan(w.Name.Trim()));
        if (ringkasanSheet is not null)
        {
            ringkasan = BuildRingkasanComparison(workbook, ringkasanSheet, storages, games);
        }

        return new ImportPreview
        {
            Storages = storages,
            Games = games,
            SkippedSheets = skipped,
            Warnings = warnings,
            Ringkasan = ringkasan,
        };
    }

    private static bool IsRingkasan(string sheetName) =>
        string.Equals(sheetName, "Ringkasan", StringComparison.OrdinalIgnoreCase);

    private static bool IsDikecualikan(string sheetName, bool headerHasAlasan) =>
        string.Equals(sheetName, "Dikecualikan", StringComparison.OrdinalIgnoreCase) || headerHasAlasan;

    private static bool IsDieliminasi(string sheetName, bool headerHasAsal) =>
        string.Equals(sheetName, "Game Dieliminasi", StringComparison.OrdinalIgnoreCase) || headerHasAsal;

    private sealed record StorageClassification(StorageType Tipe, GameStatus Status, double? KapasitasGb, double? BatasGb);

    private static bool TryGetStorageClassification(string sheetName, out StorageClassification classification)
    {
        var normalized = sheetName.Trim();
        if (HddEksternalSheet().IsMatch(normalized))
        {
            classification = new StorageClassification(StorageType.HddEksternal, GameStatus.BelumDownload, 1000, 900);
            return true;
        }

        if (LaptopHddSheet().IsMatch(normalized))
        {
            classification = new StorageClassification(StorageType.LaptopHdd, GameStatus.SudahAda, null, null);
            return true;
        }

        if (LaptopSsdSheet().IsMatch(normalized))
        {
            classification = new StorageClassification(StorageType.LaptopSsd, GameStatus.SudahAda, null, null);
            return true;
        }

        classification = null!;
        return false;
    }

    private static int? FindHeaderRow(IXLWorksheet worksheet)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var limit = Math.Min(lastRow, 15);
        for (var row = 1; row <= limit; row++)
        {
            foreach (var cell in worksheet.Row(row).CellsUsed())
            {
                var text = cell.GetString().Trim();
                if (string.Equals(text, "Nama Game", StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }
        }

        return null;
    }

    private static (int NamaCol, int? UkuranCol, int? ThirdCol, bool HeaderHasAlasan, bool HeaderHasAsal) AnalyzeHeader(
        IXLWorksheet worksheet, int headerRow)
    {
        int namaCol = 0;
        int? ukuranCol = null;
        int? thirdCol = null;
        var headerHasAlasan = false;
        var headerHasAsal = false;

        foreach (var cell in worksheet.Row(headerRow).CellsUsed())
        {
            var text = cell.GetString().Trim();
            var column = cell.Address.ColumnNumber;
            if (string.Equals(text, "Nama Game", StringComparison.OrdinalIgnoreCase))
            {
                namaCol = column;
                continue;
            }

            if (text.StartsWith("Ukuran", StringComparison.OrdinalIgnoreCase))
            {
                ukuranCol = column;
                continue;
            }

            if (text.StartsWith("No", StringComparison.OrdinalIgnoreCase))
            {
                continue; // row number column; unused
            }

            thirdCol = column;
            if (text.StartsWith("Alasan", StringComparison.OrdinalIgnoreCase))
            {
                headerHasAlasan = true;
            }

            if (text.StartsWith("Asal", StringComparison.OrdinalIgnoreCase))
            {
                headerHasAsal = true;
            }
        }

        return (namaCol, ukuranCol, thirdCol, headerHasAlasan, headerHasAsal);
    }

    private static Storage BuildStorage(IXLWorksheet worksheet, string sheetName, StorageClassification classification, int order, List<string> warnings)
    {
        double? kapasitas = classification.KapasitasGb;
        double? batas = classification.BatasGb;

        // Titles like "1 TB  (Maks <= 900 GB)" override the type defaults.
        var title = worksheet.Cell(1, 1).GetString();
        var tbMatch = TerabyteTitle().Match(title);
        if (tbMatch.Success && NumberParser.ParseGb(tbMatch.Groups["tb"].Value) is { } tb)
        {
            kapasitas = tb * 1000;
        }

        var batasMatch = BatasTitle().Match(title);
        if (batasMatch.Success && NumberParser.ParseGb(batasMatch.Groups["gb"].Value) is { } gb)
        {
            batas = gb;
        }

        if (classification.Tipe == StorageType.HddEksternal && (!kapasitas.HasValue || !batas.HasValue))
        {
            warnings.Add($"Sheet {sheetName}: storage title does not include capacity or limit; defaulting to 1000 GB / 900 GB.");
            kapasitas ??= 1000;
            batas ??= 900;
        }

        return new Storage
        {
            Nama = sheetName,
            Tipe = classification.Tipe,
            KapasitasGb = kapasitas,
            BatasGb = batas,
            Urutan = order,
        };
    }

    private static List<GamePreview> ReadRows(
        XLWorkbook workbook,
        IXLWorksheet worksheet,
        int headerRow,
        int namaCol,
        int? ukuranCol,
        int? thirdCol,
        GameStatus status,
        int? alasanCol,
        int? asalStorageCol,
        string sheetName,
        List<string> warnings)
    {
        var games = new List<GamePreview>();
        if (namaCol == 0)
        {
            return games;
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var namaCell = worksheet.Cell(row, namaCol);
            var nama = namaCell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(nama))
            {
                continue;
            }

            if (string.Equals(nama, "TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                break; // nothing but totals below the data block
            }

            double uk = 0;
            if (ukuranCol is { } ukCol)
            {
                var raw = ReadNumber(workbook, worksheet.Cell(row, ukCol));
                if (raw is null && !worksheet.Cell(row, ukCol).IsEmpty())
                {
                    warnings.Add($"Sheet {sheetName} row {row}: size \"{worksheet.Cell(row, ukCol).GetString().Trim()}\" could not be read; treated as 0.");
                }

                uk = raw ?? 0;
            }

            string? alasan = alasanCol is { } aCol ? EmptyToNull(worksheet.Cell(row, aCol).GetString()) : null;
            string? asal = asalStorageCol is { } sCol ? EmptyToNull(worksheet.Cell(row, sCol).GetString()) : null;
            games.Add(new GamePreview
            {
                SheetName = sheetName,
                Game = new Game
                {
                    Nama = nama,
                    UkuranGb = uk,
                    Status = status,
                    Alasan = alasan,
                    AsalStorage = asal,
                    Urutan = games.Count + 1,
                },
            });
        }

        return games;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static double? ReadNumber(XLWorkbook workbook, IXLCell cell)
    {
        try
        {
            if (cell.IsEmpty())
            {
                return null;
            }

            if (cell.HasFormula)
            {
                var cached = ToDouble(cell.CachedValue);
                if (cached.HasValue)
                {
                    return cached;
                }

                workbook.RecalculateAllFormulas();
                return ToDouble(cell.CachedValue) ?? ToDouble(cell.Value);
            }

            return ToDouble(cell.Value);
        }
        catch
        {
            return NumberParser.ParseGb(cell.GetString());
        }
    }

    private static double? ToDouble(object? value)
    {
        if (value is XLCellValue cellValue)
        {
            try
            {
                if (cellValue.IsNumber)
                {
                    return cellValue.GetNumber();
                }

                if (cellValue.IsText)
                {
                    return NumberParser.ParseGb(cellValue.GetText());
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        return value switch
        {
            null => null,
            bool b => b ? 1 : 0,
            double d => d,
            float f => f,
            decimal m => (double)m,
            int i => i,
            long l => l,
            short s => s,
            string str => NumberParser.ParseGb(str),
            DateTime => null,
            _ => null,
        };
    }

    private static RingkasanComparison BuildRingkasanComparison(
        XLWorkbook wb,
        IXLWorksheet ringkasan,
        List<StoragePreview> storages,
        List<GamePreview> games)
    {
        var actualsBySheet = storages.ToDictionary(
            s => NormalizeSheetKey(s.SheetName),
            s => (Count: s.GameCount, Gb: s.TotalGb));

        // ClosedXML does not store cached results for formulas it writes, and older
        // files may carry stale caches; recalculate once so the comparison is accurate.
        try
        {
            wb.RecalculateAllFormulas();
        }
        catch
        {
            // Best effort; uncached cells fall back to whatever the file carries.
        }

        var rows = new List<RingkasanRow>();
        var lastRow = ringkasan.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 2; row <= lastRow; row++)
        {
            var label = ringkasan.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var key = NormalizeSheetKey(label);
            if (key.StartsWith("dikecualikan", StringComparison.Ordinal))
            {
                rows.Add(new RingkasanRow
                {
                    Label = label,
                    StorageSheet = null,
                    ExpectedCount = ToInt(ToDouble(ringkasan.Cell(row, 2).Value)),
                    ExpectedGb = null, // Ringkasan shows a dash here, never a GB value
                    ActualCount = games.Count(g => g.Game.Status == GameStatus.Dikecualikan),
                    ActualGb = 0,
                });
                continue;
            }

            var match = actualsBySheet.Keys.FirstOrDefault(k => key.StartsWith(k, StringComparison.Ordinal));
            var actual = match is null ? ((int Count, double Gb)?)null : actualsBySheet[match];
            var sheetName = match is null ? null : storages.First(s => NormalizeSheetKey(s.SheetName) == match).SheetName;

            rows.Add(new RingkasanRow
            {
                Label = label,
                StorageSheet = sheetName,
                ExpectedCount = ToInt(ToDouble(ringkasan.Cell(row, 2).Value)),
                ExpectedGb = ToDouble(ringkasan.Cell(row, 3).Value),
                ActualCount = actual?.Count ?? 0,
                ActualGb = actual?.Gb ?? 0,
            });
        }

        return new RingkasanComparison { Rows = rows };
    }

    private static string NormalizeSheetKey(string value)
    {
        var stripped = Parentheses().Replace(value, " ");
        var collapsed = Whitespace().Replace(stripped, " ").Trim().ToLowerInvariant();
        return collapsed;
    }

    private static int? ToInt(double? value) =>
        value.HasValue ? (int)Math.Round(value.Value, MidpointRounding.AwayFromZero) : null;

    [GeneratedRegex(@"^HDD Eksternal \d+$", RegexOptions.CultureInvariant)]
    private static partial Regex HddEksternalSheet();

    [GeneratedRegex(@"^Laptop HDD \d+", RegexOptions.CultureInvariant)]
    private static partial Regex LaptopHddSheet();

    [GeneratedRegex(@"^Laptop SSD Internal$", RegexOptions.CultureInvariant)]
    private static partial Regex LaptopSsdSheet();

    [GeneratedRegex(@"(?<tb>\d+([.,]\d+)?)\s*TB", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TerabyteTitle();

    [GeneratedRegex(@"Maks[^0-9]*(?<gb>\d+([.,]\d+)?)\s*GB", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BatasTitle();

    [GeneratedRegex(@"\([^)]*\)", RegexOptions.CultureInvariant)]
    private static partial Regex Parentheses();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
