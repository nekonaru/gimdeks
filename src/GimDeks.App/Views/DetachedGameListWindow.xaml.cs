using System.Windows;

namespace GimDeks.App.Views;

public partial class DetachedGameListWindow : Window
{
    public DetachedGameListWindow(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
