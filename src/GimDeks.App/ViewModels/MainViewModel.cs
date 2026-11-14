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
        GameListViewModel gameList)
    {
        Dashboard = dashboard;
        GameList = gameList;
        NavItems = new[]
        {
            new NavItem(Page.Dashboard, "Dashboard", "ViewDashboard"),
            new NavItem(Page.GameList, "Daftar Game", "ControllerClassic"),
        };
        CurrentViewModel = dashboard;
        dashboard.OpenStorageGamesRequested += storageId => NavigateTo(Page.GameList, storageId);
    }

    public IReadOnlyList<NavItem> NavItems { get; }
    public DashboardViewModel Dashboard { get; }
    public GameListViewModel GameList { get; }

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
            _ => Dashboard,
        };

        if (CurrentViewModel is IRefreshable refreshable)
        {
            _ = SafeRefresh(refreshable);
        }
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

    [RelayCommand]
    private void Navigate(Page page) => NavigateTo(page);
}
