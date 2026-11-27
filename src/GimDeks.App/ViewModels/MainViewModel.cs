using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GimDeks.App.ViewModels;

public sealed record NavItem(Page Page, string LabelKey, string IconKind, string SectionKey);

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
        Services.AppSettingsService settings,
        HealthViewModel health,
        ScanGamesViewModel scanGames,
        BackupManagerViewModel backups,
        SettingsViewModel settingsVm,
        ActivityViewModel activity)
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
        Health = health;
        ScanGames = scanGames;
        Backups = backups;
        Settings = settingsVm;
        Activity = activity;
        IsLightTheme = theming.IsLightTheme;
        LanguageLabel = GetLanguageLabel(localization.CurrentLanguage);
        NavItems = new[]
        {
            new NavItem(Page.Dashboard, "Ui_Nav_Dashboard", "ViewDashboard", "Ui_NavSection_Collection"),
            new NavItem(Page.GameList, "Ui_Nav_GameList", "ControllerClassic", "Ui_NavSection_Collection"),
            new NavItem(Page.Storage, "Ui_Nav_Storage", "Harddisk", "Ui_NavSection_Collection"),
            new NavItem(Page.Dikecualikan, "Ui_Nav_Dikecualikan", "Block", "Ui_NavSection_Collection"),
            new NavItem(Page.Dieliminasi, "Ui_Nav_Dieliminasi", "DeleteSweep", "Ui_NavSection_Collection"),
            new NavItem(Page.Duplicates, "Ui_Nav_Duplicates", "ContentDuplicate", "Ui_NavSection_Collection"),
            new NavItem(Page.SaranHdd, "Ui_Nav_SaranHdd", "ChartBar", "Ui_NavSection_Play"),
            new NavItem(Page.Statistics, "Ui_Nav_Statistics", "ChartLine", "Ui_NavSection_Play"),
            new NavItem(Page.Health, "Ui_Nav_Health", "Medical", "Ui_NavSection_Maintain"),
            new NavItem(Page.ScanGames, "Ui_Nav_ScanGames", "FolderSearch", "Ui_NavSection_Maintain"),
            new NavItem(Page.Backups, "Ui_Nav_Backups", "Backup", "Ui_NavSection_Maintain"),
            new NavItem(Page.ImportExport, "Ui_Nav_ImportExport", "ImportExport", "Ui_NavSection_Maintain"),
            new NavItem(Page.Activity, "Ui_Nav_Activity", "History", "Ui_NavSection_System"),
            new NavItem(Page.Settings, "Ui_Nav_Settings", "Settings", "Ui_NavSection_System"),
        };
        NavItemsView = CollectionViewSource.GetDefaultView(NavItems);
        NavItemsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavItem.SectionKey)));
        CurrentViewModel = dashboard;
        UpdateNavigationTexts();
        dashboard.OpenStorageGamesRequested += storageId => NavigateTo(Page.GameList, storageId);
        dashboard.OpenRandomGameRequested += gameId =>
        {
            GameList.SetPendingGame(gameId);
            NavigateTo(Page.GameList);
        };
        dashboard.JourneyActionRequested += page => NavigateTo(page);
        importExport.DataChanged += RefreshAll;
        health.OpenIssueRequested += OpenHealthIssue;
        scanGames.DataChanged += RefreshAll;
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
            Page.Health => "Ui_Nav_Health",
            Page.ScanGames => "Ui_Nav_ScanGames",
            Page.Backups => "Ui_Nav_Backups",
            Page.Activity => "Ui_Nav_Activity",
            Page.Settings => "Ui_Nav_Settings",
            Page.ImportExport => "Ui_Nav_ImportExport",
            _ => "Ui_Nav_Dashboard",
        };
        return Services.Loc.Get(key);
    }

    private static string GetLanguageLabel(string lang) =>
        string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "EN" : "ID";

    public IReadOnlyList<NavItem> NavItems { get; }
    public ICollectionView NavItemsView { get; }
    public DashboardViewModel Dashboard { get; }
    public GameListViewModel GameList { get; }
    public StorageViewModel Storage { get; }
    public ExcludedViewModel Excluded { get; }
    public EliminatedViewModel Eliminated { get; }
    public ImportExportViewModel ImportExport { get; }
    public DuplicatesViewModel Duplicates { get; }
    public HddSuggestionViewModel HddSuggestion { get; }
    public StatisticsViewModel Statistics { get; }
    public HealthViewModel Health { get; }
    public ScanGamesViewModel ScanGames { get; }
    public BackupManagerViewModel Backups { get; }
    public SettingsViewModel Settings { get; }
    public ActivityViewModel Activity { get; }

    private void OpenHealthIssue(HealthIssue issue)
    {
        if (issue.TargetPage is not { } page)
        {
            return;
        }

        if (issue.TargetId is { } id)
        {
            if (page == Page.GameList)
            {
                GameList.SetPendingGame(id);
            }
            else if (page == Page.Storage)
            {
                Storage.SetPendingStorage(id);
            }
        }

        NavigateTo(page);
    }

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private int _refreshRunning;
    private bool _refreshAgain;

    private void RefreshAll()
    {
        if (Interlocked.CompareExchange(ref _refreshRunning, 1, 0) != 0)
        {
            Volatile.Write(ref _refreshAgain, true);
            return;
        }
        _ = RunRefreshAllAsync();
    }

    private async Task RunRefreshAllAsync()
    {
        try
        {
            do
            {
                Volatile.Write(ref _refreshAgain, false);
                await _refreshLock.WaitAsync().ConfigureAwait(true);
                try
                {
                    foreach (IRefreshable vm in new IRefreshable[] { Dashboard, GameList, Storage, Excluded, Eliminated, Duplicates, HddSuggestion, Statistics, Health, ScanGames, Backups, Settings, Activity })
                    {
                        await SafeRefresh(vm);
                    }
                }
                finally
                {
                    _refreshLock.Release();
                }
            }
            while (Volatile.Read(ref _refreshAgain));
        }
        finally
        {
            Interlocked.Exchange(ref _refreshRunning, 0);
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
            Page.Health => Health,
            Page.ScanGames => ScanGames,
            Page.Backups => Backups,
            Page.Settings => Settings,
            Page.Activity => Activity,
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
    private void PickRandomGame() => Dashboard.PickRandomGameCommand.Execute(null);

    [RelayCommand]
    private void ToggleSelesai() => GameList.ToggleSelesaiCommand.Execute(null);

    [RelayCommand]
    private void Export() => ImportExport.ExportCommand.Execute(null);

    [RelayCommand]
    private void OpenPalette()
    {
        new Views.CommandPaletteWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

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

    public void RefreshTexts()
    {
        LanguageLabel = GetLanguageLabel(_localization.CurrentLanguage);
        UpdateNavigationTexts();
    }

    [RelayCommand]
    private void Undo()
    {
        App.Services.GetRequiredService<UndoRedoService>().Undo();
        GameList.RefreshAsync().GetAwaiter().GetResult();
    }

    [RelayCommand]
    private void Redo()
    {
        App.Services.GetRequiredService<UndoRedoService>().Redo();
        GameList.RefreshAsync().GetAwaiter().GetResult();
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

