using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace GimDeks.App.Services;

/// <summary>
/// Fetches cover art from the Steam storefront. Only ever called when the
/// user clicks "Ambil Cover". The only data sent is the game title.
/// Online, opt-in, never called automatically.
/// </summary>
public sealed class SteamCoverService
{
    private const long MaxBytes = 20 * 1024 * 1024; // 20 MB
    private static readonly HttpClient Http = CreateClient();

    private const long MaxJsonBytes = 5L * 1024 * 1024; // 5 MB is far above any search response

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { UseCookies = false };
        var client = new HttpClient(handler) { MaxResponseContentBufferSize = MaxBytes };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GimDeks/3.0");
        client.Timeout = TimeSpan.FromSeconds(15);
        return client;
    }

    public sealed class CoverResult
    {
        public required string Path { get; init; }
        public required string MatchedName { get; init; }
    }

    /// <summary>Search Steam for the game by title, download its header art, save it as the cover for gameId. Returns null when no match is found.</summary>
    public async Task<CoverResult?> FetchCoverAsync(string gameTitle, long gameId, CancellationToken ct = default)
    {
        var appId = await FindAppIdAsync(gameTitle, ct);
        if (appId is null)
        {
            return null;
        }

        var bytes = await DownloadBytesAsync($"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId.Value}/header.jpg", ct);
        if (bytes is null || !LooksLikeImage(bytes))
        {
            return null;
        }

        Directory.CreateDirectory(CoverDirectory());
        var extension = DetectExtension(bytes);
        var path = Path.Combine(CoverDirectory(), gameId + extension);
        await File.WriteAllBytesAsync(path, bytes, ct);
        RemoveStaleCovers(gameId, extension);
        CoverPathCache.Refresh(gameId);
        return new CoverResult { Path = path, MatchedName = gameTitle };
    }

    public static string? FindCoverPath(long gameId) => CoverPathCache.GetPath(gameId);

    internal static bool IsCoverExtension(string file) =>
        ".jpg".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)
        || ".jpeg".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)
        || ".png".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)
        || ".webp".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase);

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, long maxBytes, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new InvalidDataException("The Steam search response is too large.");
            }

            await ms.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        return ms.ToArray();
    }

    private async Task<long?> FindAppIdAsync(string term, CancellationToken ct)
    {
        var url = $"https://store.steampowered.com/api/storesearch?term={Uri.EscapeDataString(term)}&l=en&cc=us";
        using var response = await Http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.Content.Headers.ContentLength is { } length && length > MaxJsonBytes)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        var json = await ReadBoundedAsync(stream, MaxJsonBytes, ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            return null;
        }

        var first = items[0];
        return first.TryGetProperty("id", out var id) && id.TryGetInt64(out var appId) ? appId : null;
    }

    private async Task<byte[]?> DownloadBytesAsync(string url, CancellationToken ct)
    {
        using var response = await Http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.Content.Headers.ContentLength is { } len && len > MaxBytes)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > MaxBytes)
            {
                return null;
            }

            await ms.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        return ms.ToArray();
    }

    private static bool LooksLikeImage(byte[] bytes) =>
        (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) || // JPEG
        (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) || // PNG
        (bytes.Length >= 12 && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P'); // WebP

    private static string DetectExtension(byte[] bytes) =>
        bytes[0] == 0xFF ? ".jpg"
        : bytes[0] == 0x89 ? ".png"
        : ".webp";

    private static string CoverDirectory() =>
        Path.Combine(GimDeks.Data.DatabasePaths.AppDataDirectory, "covers");

    private static void RemoveStaleCovers(long gameId, string keepExtension)
    {
        foreach (var file in Directory.EnumerateFiles(CoverDirectory(), gameId + ".*"))
        {
            if (!Path.GetExtension(file).Equals(keepExtension, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(file);
            }
        }
    }
}















