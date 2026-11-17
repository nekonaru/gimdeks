using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class GameListPage : UserControl
{
    private bool _syncingSelection;

    public GameListPage()
    {
        InitializeComponent();
        FocusService.SearchRequested += OnSearchRequested;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is GameListViewModel oldVm)
        {
            oldVm.RowsReloaded -= OnRowsReloaded;
        }

        if (e.NewValue is GameListViewModel vm)
        {
            vm.RowsReloaded -= OnRowsReloaded;
            vm.RowsReloaded += OnRowsReloaded;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        FocusService.SearchRequested -= OnSearchRequested;
        if (DataContext is GameListViewModel vm)
        {
            vm.RowsReloaded -= OnRowsReloaded;
        }
    }

    private void OnRowsReloaded()
    {
        if (DataContext is not GameListViewModel vm || GamesGrid is null)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            GamesGrid.SelectedItems.Clear();
            foreach (var row in vm.SelectedRows)
            {
                GamesGrid.SelectedItems.Add(row);
            }
        }
        finally
        {
            _syncingSelection = false;
        }
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
        if (_syncingSelection)
        {
            return;
        }

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
