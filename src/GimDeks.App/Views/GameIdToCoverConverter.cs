using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GimDeks.App.Views;

/// <summary>Cover file for a game id, cached in memory; null when there is none.</summary>
public sealed class GameIdToCoverConverter : IValueConverter
{
    private const int MaxEntries = 256;
    private static readonly ConcurrentDictionary<string, Lazy<BitmapImage?>> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long gameId)
        {
            return null;
        }

        var path = Services.SteamCoverService.FindCoverPath(gameId);
        if (path is null)
        {
            return null;
        }

        try
        {
            var stamp = System.IO.File.GetLastWriteTimeUtc(path).Ticks;
            var key = path + "|" + stamp;
            var lazy = Cache.GetOrAdd(key, k => new Lazy<BitmapImage?>(() => Load(k)));
            if (Cache.Count > MaxEntries)
            {
                Trim();
            }

            return lazy.Value;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage? Load(string key)
    {
        var path = key.Substring(0, key.LastIndexOf('|'));
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static void Trim()
    {
        foreach (var key in Cache.Keys.Take(MaxEntries / 2))
        {
            Cache.TryRemove(key, out _);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
