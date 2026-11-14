using System.Windows.Controls;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class GameListPage : UserControl
{
    public GameListPage()
    {
        InitializeComponent();
    }

    private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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
