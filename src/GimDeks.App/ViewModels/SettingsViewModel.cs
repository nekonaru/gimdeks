using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public partial class SettingsViewModel : ObservableObject, IRefreshable
{
    private readonly AppSettingsService _settings;
    private readonly ThemingService _theming;
    private readonly LocalizationService _localization;
    private readonly ICustomFieldRepository _fields;

    public SettingsViewModel(AppSettingsService settings, ThemingService theming, LocalizationService localization, ICustomFieldRepository fields)
    {
        _settings = settings;
        _theming = theming;
        _localization = localization;
        _fields = fields;
    }

    [ObservableProperty]
    private string _lowSpaceThreshold = "";

    [ObservableProperty]
    private string _backupRetention = "";

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _newFieldName = "";

    [ObservableProperty]
    private string _newFieldType = "text";

    public IReadOnlyList<CustomField> Fields { get; private set; } = Array.Empty<CustomField>();
    public IReadOnlyList<string> FieldTypes { get; } = new[] { "text", "number", "date", "choice" };

    public string LanguageText => string.Equals(_localization.CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase)
        ? Loc.Get("Ui_English") : Loc.Get("Ui_Indonesian");

    public async Task RefreshAsync()
    {
        var settings = _settings.Load();
        LowSpaceThreshold = settings.LowSpaceThresholdGb?.ToString("0", AppCulture.Current) ?? "";
        BackupRetention = settings.BackupRetention.ToString(AppCulture.Current);
        IsLightTheme = settings.IsLightTheme;
        Fields = await Task.Run(() => _fields.GetAllAsync());
        OnPropertyChanged(nameof(LanguageText));
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
    private async Task AddFieldAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFieldName))
        {
            return;
        }
        await _fields.AddAsync(NewFieldName, NewFieldType);
        NewFieldName = "";
        await RefreshAsync();
        StatusText = Loc.Get("Ui_Settings_FieldAdded");
    }

    [RelayCommand]
    private async Task DeleteFieldAsync(CustomField? field)
    {
        if (field is null)
        {
            return;
        }
        await _fields.DeleteAsync(field.Id);
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        _localization.ToggleLanguageAndReopen();
    }
}
