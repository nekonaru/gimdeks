using System.Windows.Controls;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class StoragePage : UserControl
{
    public StoragePage()
    {
        InitializeComponent();
    }

    private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not StorageViewModel viewModel || sender is not DataGrid grid)
        {
            return;
        }

        viewModel.SelectedGames.Clear();
        foreach (var item in grid.SelectedItems.OfType<GameRow>())
        {
            viewModel.SelectedGames.Add(item);
        }
    }
}
