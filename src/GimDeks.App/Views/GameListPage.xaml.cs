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
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(new Action(OnRowsReloaded));

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
            Dispatcher.BeginInvoke(new Action(OnRowsReloaded));
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

            if (vm.SelectedRows.Count > 0)
            {
                GamesGrid.ScrollIntoView(vm.SelectedRows[0], null);
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

    private void DataGrid_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedImage(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DataGrid_OnDrop(object sender, DragEventArgs e)
    {
        if (FindRowFrom(e) is not { Item: GameRow row })
        {
            return;
        }

        var image = GetDroppedImage(e);
        if (image is null || DataContext is not GameListViewModel viewModel)
        {
            return;
        }

        try
        {
            new Services.CoverFileService().SaveCover(row.Game.Id, image);
            Views.GameIdToCoverConverter.Invalidate(row.Game.Id);
            viewModel.StatusText = Services.Loc.Format("Ui_Cover_DroppedSave", row.Game.Nama);
            await viewModel.RefreshAsync();
        }
        catch (Exception exception)
        {
            viewModel.StatusText = Services.Loc.Format("Ui_Cover_DroppedFailed", exception.Message);
        }
    }

    private static DataGridRow? FindRowFrom(DragEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        while (element is not null && element is not DataGridRow)
        {
            element = VisualTreeHelper.GetParent(element);
        }

        return element as DataGridRow;
    }

    private static string? GetDroppedImage(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return null;
        }

        return files.FirstOrDefault(Services.CoverFileService.IsCoverImage);
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
