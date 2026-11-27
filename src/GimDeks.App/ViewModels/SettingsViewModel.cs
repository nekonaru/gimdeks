using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public partial class SettingsViewModel : ObservableObject, IRefreshable
{
    private readonly AppSettingsService _settings;
    private readonly ThemingService _theming;
    private readonly LocalizationService _localization;

    public SettingsViewModel(AppSettingsService settings, ThemingService theming, LocalizationService localization)
    {
        _settings = settings;
        _theming = theming;
        _localization = localization;
    }

    [ObservableProperty]
    private string _lowSpaceThreshold = "";

    [ObservableProperty]
    private string _backupRetention = "";

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private string _statusText = "";

    public string LanguageText => string.Equals(_localization.CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase)
        ? Loc.Get("Ui_English") : Loc.Get("Ui_Indonesian");

    public Task RefreshAsync()
    {
        var settings = _settings.Load();
        LowSpaceThreshold = settings.LowSpaceThresholdGb?.ToString("0", AppCulture.Current) ?? "";
        BackupRetention = settings.BackupRetention.ToString(AppCulture.Current);
        IsLightTheme = settings.IsLightTheme;
        OnPropertyChanged(nameof(LanguageText));
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Save()
    {
        var settings = _settings.Load();
        var parsed = NumberParser.ParseGb(LowSpaceThreshold.Trim());
        if (parsed is < 0)
        {
            StatusText = Loc.Get("Ui_Settings_InvalidThreshold");
            return;
        }
        settings.LowSpaceThresholdGb = parsed;
        if (int.TryParse(BackupRetention.Trim(), out var retention) && retention is >= 1 and <= 200)
        {
            settings.BackupRetention = retention;
        }
        else
        {
            StatusText = Loc.Get("Ui_Settings_InvalidRetention");
            return;
        }
        settings.IsLightTheme = IsLightTheme;
        _settings.Save(settings);
        _theming.Apply(IsLightTheme);
        StatusText = Loc.Get("Ui_Settings_Saved");
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        _localization.ToggleLanguageAndReopen();
    }
}
