using System.Windows;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class MiniPlayerWindow : Window
{
    public MiniPlayerWindow(MiniPlayerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.RefreshAsync();
    }
}
