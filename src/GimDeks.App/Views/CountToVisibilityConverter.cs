using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GimDeks.App.Views;

/// <summary>Visible when the bound count is zero (empty state message).</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
