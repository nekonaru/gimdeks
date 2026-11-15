using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GimDeks.App.Views;

/// <summary>Cover file for a game id; null when there is none.</summary>
public sealed class GameIdToCoverConverter : IValueConverter
{
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
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
