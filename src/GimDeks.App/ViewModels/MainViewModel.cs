using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace GimDeks.App.ViewModels;

public sealed record NavItem(Page Page, string Label, string IconKind);

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private Page _currentPage = Page.Dashboard;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    public MainViewModel(
        DashboardViewModel dashboard,
        GameListViewModel gameList,
        StorageViewModel storage,
        ExcludedViewModel excluded,
        EliminatedViewModel eliminated,
        ImportExportViewModel importExport,
        DuplicatesViewModel duplicates,
        HddSuggestionViewModel hddSuggestion,
        Services.ThemingService theming,
        Services.AppSettingsService settings)
    {
        _theming = theming;
        _settings = settings;
        Dashboard = dashboard;
        GameList = gameList;
        Storage = storage;
        Excluded = excluded;
        Eliminated = eliminated;
        ImportExport = importExport;
        Duplicates = duplicates;
        HddSuggestion = hddSuggestion;
        _theming = theming;
        IsLightTheme = theming.IsLightTheme;
        NavItems = new[]
        {
            new NavItem(Page.Dashboard, "Dashboard", "ViewDashboard"),
            new NavItem(Page.GameList, "Daftar Game", "ControllerClassic"),
            new NavItem(Page.Storage, "Storage", "Harddisk"),
            new NavItem(Page.Dikecualikan, "Dikecualikan", "Block"),
            new NavItem(Page.Dieliminasi, "Dieliminasi", "DeleteSweep"),
            new NavItem(Page.Duplicates, "Duplikat", "ContentDuplicate"),
            new NavItem(Page.SaranHdd, "Saran HDD", "ChartBar"),
            new NavItem(Page.ImportExport, "Import / Export", "ImportExport"),
        };
        CurrentViewModel = dashboard;
        dashboard.OpenStorageGamesRequested += storageId => NavigateTo(Page.GameList, storageId);
        importExport.DataChanged += RefreshAll;
        RegisterWatcher();
    }

    private readonly Services.ThemingService _theming;
    private readonly Services.AppSettingsService _settings;
    private FileSystemWatcher? _watcher;
    private string? _watcherDirectory;

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private bool _showExcelChangedBanner;

    [RelayCommand]
    private void FocusSearch()
    {
        NavigateTo(Page.GameList);
        Services.FocusService.RaiseSearchRequested();
    }

    [RelayCommand]
    private void AddGame() => GameList.AddGameCommand.Execute(null);

    [RelayCommand]
    private void Export() => ImportExport.ExportCommand.Execute(null);

    public IReadOnlyList<NavItem> NavItems { get; }
    public DashboardViewModel Dashboard { get; }
    public GameListViewModel GameList { get; }
    public StorageViewModel Storage { get; }
    public ExcludedViewModel Excluded { get; }
    public EliminatedViewModel Eliminated { get; }
    public ImportExportViewModel ImportExport { get; }
    public DuplicatesViewModel Duplicates { get; }
    public HddSuggestionViewModel HddSuggestion { get; }

    private void RefreshAll()
    {
        foreach (IRefreshable vm in new IRefreshable[] { Dashboard, GameList, Storage, Excluded, Eliminated, Duplicates, HddSuggestion })
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
        System.Windows.Application.Current.Dispatcher.Invoke(() => ShowExcelChangedBanner = true);
    }

    public void NavigateTo(Page page, long? storageFilter = null)
    {
        if (page == Page.GameList && storageFilter is not null)
        {
            GameList.SetStorageFilter(storageFilter);
        }

        RegisterWatcher();

        CurrentPage = page;
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

    private static async Task SafeRefresh(IRefreshable refreshable)
    {
        try
        {
            await refreshable.RefreshAsync();
        }
        catch
        {
            // Refresh failures surface as the last known-good page content; the next action retries.
        }
    }
}
