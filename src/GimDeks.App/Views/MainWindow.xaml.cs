using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;
using AppPage = GimDeks.App.ViewModels.Page;
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
            RestoreWindowPosition(loaded);
        }

        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
        App.Services.GetRequiredService<Services.ThemingService>().Apply(_viewModel.IsLightTheme);
        var target = IsValidPage(loaded.LastPage) ? (AppPage)loaded.LastPage : AppPage.Dashboard;
        _viewModel.NavigateTo(target);
        Closing += OnClosing;
    }

    private static bool IsValidPage(int value) =>
        Enum.IsDefined(typeof(AppPage), value);

    private void RestoreWindowPosition(AppSettings loaded)
    {
        if (loaded.WindowLeft is not { } left || loaded.WindowTop is not { } top)
        {
            return;
        }

        var workLeft = SystemParameters.VirtualScreenLeft;
        var workTop = SystemParameters.VirtualScreenTop;
        var workRight = workLeft + SystemParameters.VirtualScreenWidth;
        var workBottom = workTop + SystemParameters.VirtualScreenHeight;
        if (left + 120 <= workLeft || top + 80 <= workTop || left >= workRight - 120 || top >= workBottom - 80)
        {
            return; // The saved position is off screen (monitor disconnected); stay centered.
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = Math.Clamp(left, workLeft, workRight - 120);
        Top = Math.Clamp(top, workTop, workBottom - 80);
    }

    private void NavList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: NavItem item } && _viewModel.CurrentPage != item.Page)
        {
            _viewModel.NavigateTo(item.Page);
        }
    }

    private void Window_OnDragEnter(object sender, DragEventArgs e) => UpdateDropTarget(e);

    private void Overlay_OnDragOver(object sender, DragEventArgs e) => UpdateDropTarget(e);

    private void Window_OnDragLeave(object sender, DragEventArgs e)
    {
        _viewModel.SetDropTargetActive(false);
        e.Handled = true;
    }

    private void UpdateDropTarget(DragEventArgs e)
    {
        var file = GetDroppedExcelFile(e);
        e.Effects = file is null ? DragDropEffects.None : DragDropEffects.Copy;
        _viewModel.SetDropTargetActive(file is not null);
        e.Handled = true;
    }

    private void Overlay_OnDragLeave(object sender, DragEventArgs e)
    {
        _viewModel.SetDropTargetActive(false);
        e.Handled = true;
    }

    private void Window_OnDrop(object sender, DragEventArgs e)
    {
        var file = GetDroppedExcelFile(e);
        _viewModel.SetDropTargetActive(false);
        e.Handled = true;
        if (file is null)
        {
            return;
        }

        _viewModel.ImportExport.BeginImportWithFile(file);
        _viewModel.NavigateTo(AppPage.ImportExport);
    }

    private static string? GetDroppedExcelFile(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length != 1)
        {
            return null;
        }

        var file = files[0];
        if (!file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
        {
            return null;
        }

        return file;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _viewModel.StopWatcher();
        try
        {
            App.Services.GetRequiredService<SessionTrackerService>().CloseOpenSessionsAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            CrashLog.Write(exception);
        }
        var settings = _settings.Load();
        settings.IsMaximized = WindowState == WindowState.Maximized;
        if (WindowState != WindowState.Maximized)
        {
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
        }
        else
        {
            settings.WindowWidth = RestoreBounds.Width;
            settings.WindowHeight = RestoreBounds.Height;
        }

        settings.LastPage = (int)_viewModel.CurrentPage;
        _settings.Save(settings);
    }
}
