using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        ImportExportViewModel importExport)
    {
        Dashboard = dashboard;
        GameList = gameList;
        Storage = storage;
        Excluded = excluded;
        Eliminated = eliminated;
        ImportExport = importExport;
        NavItems = new[]
        {
            new NavItem(Page.Dashboard, "Dashboard", "ViewDashboard"),
            new NavItem(Page.GameList, "Daftar Game", "ControllerClassic"),
            new NavItem(Page.Storage, "Storage", "Harddisk"),
            new NavItem(Page.Dikecualikan, "Dikecualikan", "Block"),
            new NavItem(Page.Dieliminasi, "Dieliminasi", "DeleteSweep"),
            new NavItem(Page.ImportExport, "Import / Export", "ImportExport"),
        };
        CurrentViewModel = dashboard;
        dashboard.OpenStorageGamesRequested += storageId => NavigateTo(Page.GameList, storageId);
        importExport.DataChanged += RefreshAll;
    }

    public IReadOnlyList<NavItem> NavItems { get; }
    public DashboardViewModel Dashboard { get; }
    public GameListViewModel GameList { get; }
    public StorageViewModel Storage { get; }
    public ExcludedViewModel Excluded { get; }
    public EliminatedViewModel Eliminated { get; }
    public ImportExportViewModel ImportExport { get; }

    private void RefreshAll()
    {
        foreach (IRefreshable vm in new IRefreshable[] { Dashboard, GameList, Storage, Excluded, Eliminated })
        {
            _ = SafeRefresh(vm);
        }
    }

    public void NavigateTo(Page page, long? storageFilter = null)
    {
        if (page == Page.GameList && storageFilter is not null)
        {
            GameList.SetStorageFilter(storageFilter);
        }

        CurrentPage = page;
        CurrentViewModel = page switch
        {
            Page.Dashboard => Dashboard,
            Page.GameList => GameList,
            Page.Storage => Storage,
            Page.Dikecualikan => Excluded,
            Page.Dieliminasi => Eliminated,
            Page.ImportExport => ImportExport,
            _ => Dashboard,
        };

        if (CurrentViewModel is IRefreshable refreshable)
        {
            _ = SafeRefresh(refreshable);
        }
    }

    [RelayCommand]
    private void Navigate(Page page) => NavigateTo(page);

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
