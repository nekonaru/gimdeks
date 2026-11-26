using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public sealed partial class ScanMatchRow : ObservableObject
{
    public required Game Game { get; init; }
    public required ScannedExecutable Executable { get; init; }
    public bool IsExact { get; init; }

    [ObservableProperty]
    private bool _isSelected = true;

    public string MatchText => IsExact
        ? Services.Loc.Get("Ui_Scan_Exact")
        : Services.Loc.Get("Ui_Scan_Partial");
}

public sealed class DriveChoice
{
    public required string Letter { get; init; }
    public required string DisplayText { get; init; }
    public string Path => Letter + @":\";
}

/// <summary>
/// Scans a local drive for executables and suggests links to games that have no
/// launch path. Scanning only reads files; applying only stores a local path.
/// </summary>
public partial class ScanGamesViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _games;
    private readonly Services.DriveService _driveService;
    private CancellationTokenSource? _scanCts;

    public ScanGamesViewModel(IGameRepository games, Services.DriveService driveService)
    {
        _games = games;
        _driveService = driveService;
    }

    [ObservableProperty]
    private string _rootPath = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private DriveChoice? _selectedDrive;

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanScan));
    }

    partial void OnSelectedDriveChanged(DriveChoice? value)
    {
        if (value is not null)
        {
            RootPath = value.Path;
        }
    }

    public bool CanScan => !IsScanning;

    public ObservableCollection<DriveChoice> DriveChoices { get; } = new();
    public ObservableCollection<ScanMatchRow> Matches { get; } = new();
    public bool HasMatches => Matches.Count > 0;
    public int SelectedCount => Matches.Count(m => m.IsSelected);
    public string SelectedCountText => Services.Loc.Format("Ui_Scan_Selected", SelectedCount);

    /// <summary>Raised after paths are written so other pages can refresh.</summary>
    public event Action? DataChanged;

    public Task RefreshAsync() => LoadDrivesAsync();

    [RelayCommand]
    private async Task LoadDrivesAsync()
    {
        DriveChoices.Clear();
        foreach (var drive in _driveService.GetReadyDrives())
        {
            DriveChoices.Add(new DriveChoice
            {
                Letter = drive.Letter,
                DisplayText = drive.DisplayText,
            });
        }

        if (SelectedDrive is null && DriveChoices.Count > 0)
        {
            SelectedDrive = DriveChoices[0];
        }
        if (string.IsNullOrWhiteSpace(RootPath) && DriveChoices.Count > 0)
        {
            RootPath = DriveChoices[0].Path;
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (!Services.InstalledGameScanner.IsValidRoot(RootPath))
        {
            StatusText = Services.Loc.Get("Ui_Scan_InvalidPath");
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;
        IsScanning = true;
        Matches.Clear();
        OnPropertyChanged(nameof(HasMatches));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedCountText));
        StatusText = Services.Loc.Get("Ui_Scan_Scanning");

        try
        {
            var executables = await Services.InstalledGameScanner.ScanAsync(RootPath, token);
            var games = await _games.GetAllAsync(token);
            var matches = GameExecutableMatcher.Match(games, executables);
            foreach (var match in matches)
            {
                var row = new ScanMatchRow
                {
                    Game = match.Game,
                    Executable = match.Executable,
                    IsExact = match.IsExact,
                };
                row.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ScanMatchRow.IsSelected))
                    {
                        OnPropertyChanged(nameof(SelectedCount));
                        OnPropertyChanged(nameof(SelectedCountText));
                    }
                };
                Matches.Add(row);
            }

            StatusText = Services.Loc.Format("Ui_Scan_Found", Matches.Count, executables.Count);
        }
        catch (OperationCanceledException)
        {
            StatusText = Services.Loc.Get("Ui_Scan_Cancelled");
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Scan_Failed", exception.Message);
            Services.CrashLog.Write(exception);
        }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(nameof(HasMatches));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var selected = Matches.Where(m => m.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = Services.Loc.Get("Ui_Scan_NothingSelected");
            return;
        }

        foreach (var row in selected)
        {
            row.Game.PathExe = row.Executable.Path;
            await _games.UpdateAsync(row.Game);
        }

        StatusText = Services.Loc.Format("Ui_Scan_Applied", selected.Count);
        DataChanged?.Invoke();
        await LoadDrivesAsync();
    }
}
