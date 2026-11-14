using System.Windows.Controls;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class StatusListPage : UserControl
{
    public StatusListPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is StatusListViewModelBase viewModel)
            {
                AlasanColumn.Header = viewModel.AlasanHeader;
            }
        };
    }

    private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not StatusListViewModelBase viewModel || sender is not DataGrid grid)
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
