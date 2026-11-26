using System.Windows;
using GimDeks.App.ViewModels;

namespace GimDeks.App.Views;

public partial class GameDetailWindow : Window
{
    public GameDetailWindow(GameDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private void Thumbnail_OnClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Image { DataContext: ImageItem item } && DataContext is GameDetailViewModel vm)
        {
            vm.SelectedImagePath = item.Path;
        }
    }
}
