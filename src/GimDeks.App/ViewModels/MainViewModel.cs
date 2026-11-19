using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace GimDeks.App.ViewModels;

public sealed record NavItem(Page Page, string LabelKey, string IconKind);

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private Page _currentPage = Page.Dashboard;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private bool _showExcelChangedBanner;

    [ObservableProperty]
    private string _statusBarText = "";

    [ObservableProperty]
    private string _shortcutHintText = "";

    [ObservableProperty]
    private string _titleText = "GimDeks";

    [ObservableProperty]
    private string _appVersionText = $"GimDeks {Services.AppVersion.Current}";

    [ObservableProperty]
    private bool _isDropTargetActive;

    [ObservableProperty]
    private string _languageLabel = "ID";

    public MainViewModel(
        DashboardViewModel dashboard,
        GameListViewModel gameList,
        StorageViewModel storage,
        ExcludedViewModel excluded,
        EliminatedViewModel eliminated,
        ImportExportViewModel importExport,
        DuplicatesViewModel duplicates,
        HddSuggestionViewModel hddSuggestion,
        StatisticsViewModel statistics,
        Services.ThemingService theming,
        Services.LocalizationService localization,
        Services.AppSettingsService settings)
    {
        _theming = theming;
        _localization = localization;
        _settings = settings;
        Dashboard = dashboard;
        GameList = gameList;
        Storage = storage;
        Excluded = excluded;
        Eliminated = eliminated;
        ImportExport = importExport;
        Duplicates = duplicates;
        HddSuggestion = hddSuggestion;
        Statistics = statistics;
        IsLightTheme = theming.IsLightTheme;
        LanguageLabel = GetLanguageLabel(localization.CurrentLanguage);
        NavItems = new[]
        {
            new NavItem(Page.Dashboard, "Ui_Nav_Dashboard", "ViewDashboard"),
            new NavItem(Page.GameList, "Ui_Nav_GameList", "ControllerClassic"),
            new NavItem(Page.Storage, "Ui_Nav_Storage", "Harddisk"),
            new NavItem(Page.Dikecualikan, "Ui_Nav_Dikecualikan", "Block"),
            new NavItem(Page.Dieliminasi, "Ui_Nav_Dieliminasi", "DeleteSweep"),
            new NavItem(Page.Duplicates, "Ui_Nav_Duplicates", "ContentDuplicate"),
            new NavItem(Page.SaranHdd, "Ui_Nav_SaranHdd", "ChartBar"),
            new NavItem(Page.Statistics, "Ui_Nav_Statistics", "ChartLine"),
            new NavItem(Page.ImportExport, "Ui_Nav_ImportExport", "ImportExport"),
        };
        CurrentViewModel = dashboard;
        UpdateNavigationTexts();
        dashboard.OpenStorageGamesRequested += storageId => NavigateTo(Page.GameList, storageId);
        importExport.DataChanged += RefreshAll;
        RegisterWatcher();
    }

    private readonly Services.ThemingService _theming;
    private readonly Services.LocalizationService _localization;
    private readonly Services.AppSettingsService _settings;
    private FileSystemWatcher? _watcher;
    private string? _watcherDirectory;

    private void UpdateNavigationTexts()
    {
        StatusBarText = $"GimDeks v{Services.AppVersion.Current} | {GetNavLabel()}";
        ShortcutHintText = System.Windows.Application.Current.TryFindResource("Ui_ShortcutHint") as string ?? "";
        TitleText = Services.Loc.Format("Ui_TitleFormat", GetNavLabel());
    }

    private string GetNavLabel()
    {
        var key = CurrentPage switch
        {
            Page.Dashboard => "Ui_Nav_Dashboard",
            Page.GameList => "Ui_Nav_GameList",
            Page.Storage => "Ui_Nav_Storage",
            Page.Dikecualikan => "Ui_Nav_Dikecualikan",
            Page.Dieliminasi => "Ui_Nav_Dieliminasi",
            Page.Duplicates => "Ui_Nav_Duplicates",
            Page.SaranHdd => "Ui_Nav_SaranHdd",
            Page.Statistics => "Ui_Nav_Statistics",
            Page.ImportExport => "Ui_Nav_ImportExport",
            _ => "Ui_Nav_Dashboard",
        };
        return Services.Loc.Get(key);
    }

    private static string GetLanguageLabel(string lang) =>
        string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "EN" : "ID";

    public IReadOnlyList<NavItem> NavItems { get; }
    public DashboardViewModel Dashboard { get; }
    public GameListViewModel GameList { get; }
    public StorageViewModel Storage { get; }
    public ExcludedViewModel Excluded { get; }
    public EliminatedViewModel Eliminated { get; }
    public ImportExportViewModel ImportExport { get; }
    public DuplicatesViewModel Duplicates { get; }
    public HddSuggestionViewModel HddSuggestion { get; }
    public StatisticsViewModel Statistics { get; }

    private void RefreshAll()
    {
        foreach (IRefreshable vm in new IRefreshable[] { Dashboard, GameList, Storage, Excluded, Eliminated, Duplicates, HddSuggestion, Statistics })
        {
            _ = SafeRefresh(vm);
        }
    }

    private void RegisterWatcher()
    {
        var path = _settings.Load().LastImportedPath;
        if (!string.IsNullOrWhiteSpace(_watcherDirectory) && string.Equals(_watcherDirectory, path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _watcher?.Dispose();
        _watcher = null;
        _watcherDirectory = path;
        ShowExcelChangedBanner = false;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path)!;
        _watcher = new FileSystemWatcher(directory, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnWatchedFileChanged;
        _watcher.Created += OnWatchedFileChanged;
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (System.Windows.Application.Current?.Dispatcher is { } dispatcher)
            {
                dispatcher.BeginInvoke(() =>
                {
                    if (!ShowExcelChangedBanner)
                    {
                        ShowExcelChangedBanner = true;
                    }
                });
            }
        }
        catch
        {
            // The watcher must never crash the process; the banner is best effort.
        }
    }

    /// <summary>Stop watching the imported file; called when the window closes.</summary>
    public void StopWatcher()
    {
        _watcher?.Dispose();
        _watcher = null;
        _watcherDirectory = null;
    }

    public void NavigateTo(Page page, long? storageFilter = null)
    {
        if (page == Page.GameList && storageFilter is not null)
        {
            GameList.SetStorageFilter(storageFilter);
        }

        RegisterWatcher();
        CurrentPage = page;
        UpdateNavigationTexts();
        CurrentViewModel = page switch
        {
            Page.Dashboard => Dashboard,
            Page.GameList => GameList,
            Page.Storage => Storage,
            Page.Dikecualikan => Excluded,
            Page.Dieliminasi => Eliminated,
            Page.ImportExport => ImportExport,
            Page.Duplicates => Duplicates,
            Page.SaranHdd => HddSuggestion,
            Page.Statistics => Statistics,
            _ => Dashboard,
        };

        if (CurrentViewModel is IRefreshable refreshable)
        {
            _ = SafeRefresh(refreshable);
        }
    }

    [RelayCommand]
    private void Navigate(Page page) => NavigateTo(page);

    [RelayCommand]
    private void FocusSearch()
    {
        NavigateTo(Page.GameList);
        Services.FocusService.RaiseSearchRequested();
    }

    [RelayCommand]
    private void AddGame() => GameList.AddGameCommand.Execute(null);

    [RelayCommand]
    private void ToggleSelesai() => GameList.ToggleSelesaiCommand.Execute(null);

    [RelayCommand]
    private void Export() => ImportExport.ExportCommand.Execute(null);

    [RelayCommand]
    private void DismissExcelChanged() => ShowExcelChangedBanner = false;

    [RelayCommand]
    private void ImportNow()
    {
        ShowExcelChangedBanner = false;
        NavigateTo(Page.ImportExport);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsLightTheme = _theming.Toggle();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        _localization.ToggleLanguageAndReopen();
        LanguageLabel = GetLanguageLabel(_localization.CurrentLanguage);
        UpdateNavigationTexts();
    }

    public void SetDropTargetActive(bool value) => IsDropTargetActive = value;

    private async Task SafeRefresh(IRefreshable refreshable)
    {
        try
        {
            await refreshable.RefreshAsync();
        }
        catch (Exception exception)
        {
            Services.CrashLog.Write(exception);
            StatusBarText = Services.Loc.Get("Ui_RefreshFailed");
        }
    }
}
