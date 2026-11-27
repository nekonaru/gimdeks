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
            throw new InvalidDataException($"The Excel file is too large ({fileSize / (1024 * 1024)} MB). Maximum size is 50 MB.");
        }

        CheckDecompressionBudget(filePath);

        using var workbook = new XLWorkbook(filePath);
        var warnings = new List<string>();
        try
        {
            workbook.RecalculateAllFormulas();
        }
        catch
        {
            warnings.Add("Workbook formulas could not be recalculated; cached values were used.");
        }

        var games = new List<GamePreview>();
        var storages = new List<StoragePreview>();
        var skipped = new List<string>();
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

            var (namaCol, ukuranCol, thirdCol, alasanCol, asalCol, headerHasAlasan, headerHasAsal) = AnalyzeHeader(worksheet, headerRow.Value);

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

            if (IsDikecualikan(sheetName, headerHasAlasan, headerHasAsal))
            {
                var rows = ReadRows(workbook, worksheet, headerRow.Value, namaCol, ukuranCol, thirdCol, GameStatus.Dikecualikan, alasanCol: alasanCol ?? thirdCol, asalStorageCol: null, sheetName, warnings);
                games.AddRange(rows);
                continue;
            }

            if (IsDieliminasi(sheetName, headerHasAsal))
            {
                var rows = ReadRows(workbook, worksheet, headerRow.Value, namaCol, ukuranCol, thirdCol, GameStatus.Dieliminasi, alasanCol: alasanCol, asalStorageCol: asalCol ?? (alasanCol is null ? thirdCol : null), sheetName, warnings);
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

    /// <summary>
    /// An .xlsx is a zip. Reject it up front if the decompressed payload would be
    /// unreasonably large (decompression bomb) before ClosedXML expands it.
    /// </summary>
    private static void CheckDecompressionBudget(string filePath)
    {
        const long maxUncompressedBytes = 256L * 1024 * 1024; // 256 MB decompressed
        using var zip = System.IO.Compression.ZipFile.OpenRead(filePath);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            total += entry.Length;
            if (total > maxUncompressedBytes)
            {
                throw new InvalidDataException("The Excel file expands to too much data when decompressed. Maximum is 256 MB.");
            }
        }
    }

    private static bool IsRingkasan(string sheetName) =>
        string.Equals(sheetName, "Ringkasan", StringComparison.OrdinalIgnoreCase);

    private static bool IsDikecualikan(string sheetName, bool headerHasAlasan, bool headerHasAsal) =>
        string.Equals(sheetName, "Dikecualikan", StringComparison.OrdinalIgnoreCase) || (headerHasAlasan && !headerHasAsal);

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

    private static (int NamaCol, int? UkuranCol, int? ThirdCol, int? AlasanCol, int? AsalCol, bool HeaderHasAlasan, bool HeaderHasAsal) AnalyzeHeader(
        IXLWorksheet worksheet, int headerRow)
    {
        int namaCol = 0;
        int? ukuranCol = null;
        int? thirdCol = null;
        int? alasanCol = null;
        int? asalCol = null;
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

            var isAlasan = text.StartsWith("Alasan", StringComparison.OrdinalIgnoreCase);
            var mentionsAlasan = text.IndexOf("alasan", StringComparison.OrdinalIgnoreCase) >= 0;
            var isAsal = text.StartsWith("Asal", StringComparison.OrdinalIgnoreCase);
            if (text.IndexOf("Asal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                headerHasAsal = true;
            }

            if (isAlasan && !isAsal)
            {
                alasanCol = column;
                headerHasAlasan = true;
            }
            if (isAsal && !mentionsAlasan)
            {
                asalCol = column;
            }
            if (text.IndexOf("Alasan", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                headerHasAlasan = true;
            }

            thirdCol = column;
        }

        return (namaCol, ukuranCol, thirdCol, alasanCol, asalCol, headerHasAlasan, headerHasAsal);
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

            // Only a formula-bearing TOTAL row is the footer. A real game named
            // TOTAL must not truncate the rest of the sheet.
            if (string.Equals(nama, "TOTAL", StringComparison.OrdinalIgnoreCase)
                && ukuranCol is { } totalCol
                && worksheet.Cell(row, totalCol).HasFormula)
            {
                break;
            }

            double uk = 0;
            if (ukuranCol is { } ukCol)
            {
                var ukCell = worksheet.Cell(row, ukCol);
                var raw = ReadNumber(workbook, ukCell);
                if (raw is null && !ukCell.IsEmpty())
                {
                    warnings.Add($"Sheet {sheetName} row {row}: size \"{ukCell.GetString().Trim()}\" could not be read; treated as 0.");
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

                // Formulas were recalculated once at preview start; never per cell.
                return ToDouble(cell.Value);
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
        // Two sheets can normalize to the same key (for example a "Done" suffix).
        // Group instead of ToDictionary so preview never throws, and aggregate totals.
        var actuals = storages
            .GroupBy(s => NormalizeSheetKey(s.SheetName), StringComparer.OrdinalIgnoreCase)
            .Select(g => (
                Key: g.Key,
                SheetName: g.First().SheetName,
                Count: g.Sum(s => s.GameCount),
                Gb: g.Sum(s => s.TotalGb)))
            .Where(x => x.Key.Length > 0)
            .ToList();

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

            // Prefer the longest matching key: "HDD Eksternal 10" must not attach
            // the totals of "HDD Eksternal 1".
            var match = actuals
                .Where(a => key.StartsWith(a.Key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.Key.Length)
                .FirstOrDefault();
            var hasMatch = match.Key is not null;
            var sheetName = hasMatch ? match.SheetName : null;

            rows.Add(new RingkasanRow
            {
                Label = label,
                StorageSheet = sheetName,
                ExpectedCount = ToInt(ToDouble(ringkasan.Cell(row, 2).Value)),
                ExpectedGb = ToDouble(ringkasan.Cell(row, 3).Value),
                ActualCount = hasMatch ? match.Count : 0,
                ActualGb = hasMatch ? match.Gb : 0,
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

    private static int? ToInt(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return null;
        }
        if (value.Value >= int.MaxValue)
        {
            return int.MaxValue;
        }
        if (value.Value <= int.MinValue)
        {
            return int.MinValue;
        }
        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    [GeneratedRegex(@"^HDD Eksternal \d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HddEksternalSheet();

    [GeneratedRegex(@"^Laptop HDD \d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LaptopHddSheet();

    [GeneratedRegex(@"^Laptop SSD", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
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

