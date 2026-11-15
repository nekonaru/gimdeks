using System.Windows;

namespace GimDeks.App.Services;

/// <summary>
/// Loads the requested language dictionary into the app resources (merged dictionaries),
/// replacing any existing GimDeks strings. Application accent colors are untouched.
/// </summary>
public sealed class LocalizationService
{
    private readonly AppSettingsService _settings;

    public LocalizationService(AppSettingsService settings) => _settings = settings;

    public string CurrentLanguage => _settings.Load().UiLanguage;

    public void Apply(string language)
    {
        var tag = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "id";

        var merged = Application.Current.Resources.MergedDictionaries;
        foreach (var dict in merged.ToArray())
        {
            var source = dict.Source?.OriginalString ?? "";
            if (source.Contains("Strings.id.xaml") || source.Contains("Strings.en.xaml"))
            {
                merged.Remove(dict);
            }
        }

        var source2 = new Uri($"/GimDeks;component/Resources/Strings.{tag}.xaml", UriKind.Relative);
        merged.Insert(0, new ResourceDictionary { Source = source2 });

        var culture = System.Globalization.CultureInfo.GetCultureInfo(tag == "en" ? "en-US" : "id-ID");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
        AppCulture.Set(culture);
        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher)
        {
            dispatcher.Invoke(() =>
            {
                var thread = System.Threading.Thread.CurrentThread;
                thread.CurrentCulture = culture;
                thread.CurrentUICulture = culture;
            });
        }
    }

    public string Toggle()
    {
        var settings = _settings.Load();
        var next = string.Equals(settings.UiLanguage, "id", StringComparison.OrdinalIgnoreCase) ? "en" : "id";
        settings.UiLanguage = next;
        _settings.Save(settings);
        Apply(next);
        return next;
    }

    /// <summary>Switch the language and rebuild the main window so every page shows the new language.</summary>
    public void ToggleLanguageAndReopen()
    {
        var next = Toggle();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var old = System.Windows.Application.Current.MainWindow;
            var window = new Views.MainWindow();
            System.Windows.Application.Current.MainWindow = window;
            window.Show();
            old?.Close();
        });
    }
}
