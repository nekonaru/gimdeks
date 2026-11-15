using System.ComponentModel;
using System.Windows;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GimDeks.App.Views;

public partial class MainWindow : Window
{
    private readonly AppSettingsService _settings;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _settings = App.Services.GetRequiredService<AppSettingsService>();
        var loaded = _settings.Load();
        if (loaded.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            Width = loaded.WindowWidth;
            Height = loaded.WindowHeight;
        }

        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
        App.Services.GetRequiredService<Services.ThemingService>().Apply(_viewModel.IsLightTheme);
        _viewModel.NavigateTo(_viewModel.CurrentPage);
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        var settings = new AppSettings
        {
            IsMaximized = WindowState == WindowState.Maximized,
            WindowWidth = RestoreBounds.Width,
            WindowHeight = RestoreBounds.Height,
        };
        _settings.Save(settings);
    }
}















