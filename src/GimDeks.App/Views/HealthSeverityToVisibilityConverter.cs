using System.Globalization;
using System.Windows;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

/// <summary>Shows the severity badge only for warnings. Info rows stay clean.</summary>
public sealed class HealthSeverityToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is HealthSeverity severity && severity == HealthSeverity.Warning
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
