using System.Globalization;
using System.Windows.Data;

namespace GimDeks.App.Views;

/// <summary>Maps a localization resource key to its localized string (literal pass-through otherwise).</summary>
public sealed class LocKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key)
        {
            return value;
        }

        return GimDeks.App.Services.Loc.Get(key);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
