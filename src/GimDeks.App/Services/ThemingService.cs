using MaterialDesignThemes.Wpf;

namespace GimDeks.App.Services;

/// <summary>Switches the MaterialDesign light/dark theme at runtime and persists it.</summary>
public sealed class ThemingService
{
    private readonly AppSettingsService _settings;

    public ThemingService(AppSettingsService settings) => _settings = settings;

    public bool IsLightTheme => _settings.Load().IsLightTheme;

    public void Apply(bool isLight)
    {
        var palette = new PaletteHelper();
        var theme = palette.GetTheme();
        theme.SetBaseTheme(isLight ? BaseTheme.Light : BaseTheme.Dark);
        palette.SetTheme(theme);
    }

    public bool Toggle()
    {
        var settings = _settings.Load();
        settings.IsLightTheme = !settings.IsLightTheme;
        _settings.Save(settings);
        Apply(settings.IsLightTheme);
        return settings.IsLightTheme;
    }
}
