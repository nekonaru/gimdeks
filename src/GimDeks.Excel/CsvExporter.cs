using System.Globalization;
using System.IO;
using System.Text;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.Excel;

/// <summary>
/// Writes the whole catalog to a semicolon-separated UTF-8 CSV with a BOM,
/// so both locales of Excel open it directly. Same data shape as the Excel export.
/// </summary>
public sealed class CsvExporter
{
    private readonly IGameRepository _games;
    private readonly IStorageRepository _storages;
    private readonly ITagRepository _tags;

    public CsvExporter(IGameRepository games, IStorageRepository storages, ITagRepository tags)
    {
        _games = games;
        _storages = storages;
        _tags = tags;
    }

    public async Task<string> ExportAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var games = await _games.GetAllAsync();
        var storages = (await _storages.GetAllAsync()).ToDictionary(storage => storage.Id, storage => storage.Nama);
        var tagsByGame = await _tags.GetTagsForGamesAsync(games.Select(game => game.Id).ToList());

        await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(true));
        writer.WriteLine("Nama;Versi;Ukuran (GB);Status;Storage;Tag;Selesai");
        foreach (var game in games.OrderBy(g => g.Nama, StringComparer.OrdinalIgnoreCase))
        {
            var storageName = game.StorageId is { } id && storages.TryGetValue(id, out var nama) ? nama : "";
            var tagText = tagsByGame.TryGetValue(game.Id, out var tags)
                ? string.Join(", ", tags.Select(t => t.Nama))
                : "";
            var status = StatusText(game.Status);
            var selesai = game.SelesaiMain ? "1" : "0";
            var ukuran = game.UkuranGb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            writer.WriteLine(string.Join(";", new[]
            {
                Escape(game.Nama),
                Escape(game.Versi ?? ""),
                Escape(ukuran),
                Escape(status),
                Escape(storageName),
                Escape(tagText),
                Escape(selesai),
            }));
        }

        return filePath;
    }

    public async Task<string> ExportSelectedAsync(string filePath, IReadOnlyList<Game> games)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var storages = (await _storages.GetAllAsync()).ToDictionary(storage => storage.Id, storage => storage.Nama);
        var tagsByGame = games.Count == 0
            ? new Dictionary<long, IReadOnlyList<Tag>>()
            : (IReadOnlyDictionary<long, IReadOnlyList<Tag>>)await _tags.GetTagsForGamesAsync(games.Select(game => game.Id).ToList());

        await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(true));
        writer.WriteLine("Nama;Versi;Ukuran (GB);Status;Storage;Tag;Selesai");
        foreach (var game in games.OrderBy(g => g.Nama, StringComparer.OrdinalIgnoreCase))
        {
            var storageName = game.StorageId is { } id && storages.TryGetValue(id, out var nama) ? nama : "";
            var tagText = tagsByGame.TryGetValue(game.Id, out var tags)
                ? string.Join(", ", tags.Select(t => t.Nama))
                : "";
            writer.WriteLine(string.Join(";", new[]
            {
                Escape(game.Nama),
                Escape(game.Versi ?? ""),
                Escape(game.UkuranGb.ToString("0.0", CultureInfo.InvariantCulture)),
                Escape(StatusText(game.Status)),
                Escape(storageName),
                Escape(tagText),
                Escape(game.SelesaiMain ? "1" : "0"),
            }));
        }
        return filePath;
    }

    private static string StatusText(GameStatus status) => status switch
    {
        GameStatus.BelumDownload => "Belum Download",
        GameStatus.SudahAda => "Sudah Ada",
        GameStatus.Dikecualikan => "Dikecualikan",
        GameStatus.Dieliminasi => "Dieliminasi",
        _ => status.ToString(),
    };

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
