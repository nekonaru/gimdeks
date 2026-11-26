using System.Globalization;
using System.IO;
using System.Text;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;
using GimDeks.Data;

namespace GimDeks.Excel;

public sealed record CsvGameRow(
    string Nama,
    string? Versi,
    double UkuranGb,
    GameStatus Status,
    string? StorageName,
    IReadOnlyList<string> Tags,
    bool Selesai);

public sealed record CsvImportPreview(IReadOnlyList<CsvGameRow> Rows, IReadOnlyList<string> Warnings);

/// <summary>
/// Reads the semicolon CSV written by CsvExporter. Local-only, no network.
/// </summary>
public sealed class CsvImportService
{
    private readonly IGameRepository _games;
    private readonly IStorageRepository _storages;
    private readonly ITagRepository _tags;
    private readonly DatabaseBackupService _backup;

    public CsvImportService(IGameRepository games, IStorageRepository storages, ITagRepository tags, DatabaseBackupService backup)
    {
        _games = games;
        _storages = storages;
        _tags = tags;
        _backup = backup;
    }

    public CsvImportPreview Preview(string filePath)
    {
        var warnings = new List<string>();
        var rows = new List<CsvGameRow>();
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        if (lines.Length == 0)
        {
            return new CsvImportPreview(rows, warnings);
        }

        var header = Split(lines[0]);
        if (header.Count < 4 || !header[0].Contains("Nama", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Header does not match the GimDeks CSV layout.");
            return new CsvImportPreview(rows, warnings);
        }

        for (var index = 1; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                continue;
            }
            var cells = Split(lines[index]);
            var name = Cell(cells, 0);
            if (name.Length == 0)
            {
                continue;
            }
            var sizeText = Cell(cells, 2);
            var size = NumberParser.ParseGb(sizeText) ?? 0;
            var status = ParseStatus(Cell(cells, 3));
            var tags = Cell(cells, 5).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            rows.Add(new CsvGameRow(
                name,
                EmptyToNull(Cell(cells, 1)),
                size,
                status,
                EmptyToNull(Cell(cells, 4)),
                tags,
                Cell(cells, 6) == "1"));
        }

        return new CsvImportPreview(rows, warnings);
    }

    public async Task<(int Added, int Skipped, string BackupPath)> ImportAsync(string filePath, CancellationToken ct = default)
    {
        var preview = Preview(filePath);
        var backupPath = await _backup.BackupAsync(ct);
        var storages = (await _storages.GetAllAsync(ct)).ToDictionary(s => s.Nama, s => s.Id, StringComparer.OrdinalIgnoreCase);
        var existing = (await _games.GetAllAsync(ct))
            .Select(g => (GameNameNormalizer.Normalize(g.Nama), g.StorageId))
            .ToHashSet();

        var added = 0;
        var skipped = 0;
        foreach (var row in preview.Rows)
        {
            long? storageId = row.StorageName is not null && storages.TryGetValue(row.StorageName, out var id) ? id : null;
            var key = (GameNameNormalizer.Normalize(row.Nama), storageId);
            if (!existing.Add(key))
            {
                skipped++;
                continue;
            }

            var game = new Game
            {
                Nama = row.Nama,
                Versi = row.Versi,
                UkuranGb = row.UkuranGb,
                Status = row.Status,
                StorageId = storageId,
                SelesaiMain = row.Selesai,
            };
            var gameId = await _games.AddAsync(game, ct);
            foreach (var tagName in row.Tags)
            {
                var tagId = await _tags.EnsureAsync(tagName, ct);
                await _tags.SetTagsForGameAsync(gameId, new[] { tagId }, ct);
            }
            added++;
        }

        return (added, skipped, backupPath);
    }

    private static GameStatus ParseStatus(string text) => text.ToLowerInvariant() switch
    {
        "sudah ada" or "sudahada" or "downloaded" => GameStatus.SudahAda,
        "dikecualikan" or "excluded" => GameStatus.Dikecualikan,
        "dieliminasi" or "eliminated" => GameStatus.Dieliminasi,
        _ => GameStatus.BelumDownload,
    };

    private static List<string> Split(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                quoted = !quoted;
            }
            else if (ch == ';' && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    private static string Cell(IReadOnlyList<string> cells, int index) =>
        index < cells.Count ? cells[index].Trim().Trim('"') : "";

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
