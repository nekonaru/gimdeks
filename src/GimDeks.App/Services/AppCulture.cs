using System.Globalization;

namespace GimDeks.App.Services;

/// <summary>
/// Holds the culture used to format numbers and dates in the UI,
/// set by <see cref="LocalizationService"/> when the language changes.
/// </summary>
public static class AppCulture
{
    public static CultureInfo Current { get; private set; } = CultureInfo.GetCultureInfo("id-ID");

    public static void Set(CultureInfo culture) => Current = culture;
}
