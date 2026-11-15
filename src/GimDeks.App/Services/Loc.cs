using System.Globalization;
using System.Windows;

namespace GimDeks.App.Services;

/// <summary>
/// Reads localized strings from the active resource dictionary (default Bahasa Indonesia).
/// Language can be switched at runtime; DynamicResource references update automatically.
/// </summary>
public static class Loc
{
    public static string Get(string key) => Application.Current.Resources[key] as string ?? key;

    public static string Format(string key, params object[] args) =>
        string.Format(GimDeks.App.Services.AppCulture.Current, Get(key), args);
}
