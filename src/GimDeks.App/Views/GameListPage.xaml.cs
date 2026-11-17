using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class GameListPage : UserControl
{
    public GameListPage()
    {
        InitializeComponent();
        FocusService.SearchRequested += OnSearchRequested;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        FocusService.SearchRequested -= OnSearchRequested;

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

    public void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not GameListViewModel viewModel)
        {
            return;
        }

        var element = e.OriginalSource as DependencyObject;
        while (element is not null && element is not DataGridRow)
        {
            element = VisualTreeHelper.GetParent(element);
        }

        if (element is DataGridRow row && row.Item is GameRow gameRow)
        {
            viewModel.SelectedRows.Clear();
            viewModel.SelectedRows.Add(gameRow);
            viewModel.EditGameCommand.Execute(null);
        }
    }
}
