using System.Windows;
using System.Windows.Controls;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class GameListPage : UserControl
{
    public GameListPage()
    {
        InitializeComponent();
        FocusService.SearchRequested += OnSearchRequested;
    }

    private void OnSearchRequested()
    {
        if (SearchBox is null)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        });
    }

    public void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not GameListViewModel viewModel || sender is not DataGrid grid)
        {
            return;
        }

        viewModel.SelectedRows.Clear();
        foreach (var item in grid.SelectedItems.OfType<GameRow>())
        {
            viewModel.SelectedRows.Add(item);
        }
    }
}
