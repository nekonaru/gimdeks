using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;
using GimDeks.Data;

namespace GimDeks.App.ViewModels;

public enum HealthSeverity
{
    Warning,
    Info,
}

public sealed class HealthIssue
{
    public required HealthSeverity Severity { get; init; }
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public Page? TargetPage { get; init; }
    public long? TargetId { get; init; }
    public string SeverityText => Severity switch
    {
        HealthSeverity.Warning => Services.Loc.Get("Ui_Health_Warning"),
        _ => Services.Loc.Get("Ui_Health_Info"),
    };
}

public partial class HealthViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _games;
    private readonly IStorageRepository _storages;
    private readonly IDriveLinkRepository _links;
    private readonly Services.DriveService _driveService;
    private readonly DatabaseBackupService _backup;
    private readonly Services.AppSettingsService _settings;

    public HealthViewModel(
        IGameRepository games,
        IStorageRepository storages,
        IDriveLinkRepository links,
        Services.DriveService driveService,
        DatabaseBackupService backup,
        Services.AppSettingsService settings)
    {
        _games = games;
        _storages = storages;
        _links = links;
        _driveService = driveService;
        _backup = backup;
        _settings = settings;
    }

    [ObservableProperty]
    private string _summaryText = "";

    public ObservableCollection<HealthIssue> Issues { get; } = new();

    public event Action<HealthIssue>? OpenIssueRequested;

    public Task RefreshAsync() => RefreshInternalAsync();

    [RelayCommand]
    private async Task RefreshInternalAsync()
    {
        var games = await _games.GetAllAsync();
        var storages = await _storages.GetAllAsync();
        var driveLinks = await _links.GetAllAsync();
        var readyDrives = _driveService.GetReadyDriveLetters();

        Issues.Clear();

        var byStorage = games.Where(g => g.StorageId is not null)
            .GroupBy(g => g.StorageId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var storage in storages)
        {
            byStorage.TryGetValue(storage.Id, out var storageGames);
            var total = CapacityCalculator.CountedTotalGb(storageGames ?? new List<Game>());
            if (CapacityCalculator.IsOverLimit(storage, total))
            {
                var over = total - storage.BatasGb!.Value;
                Issues.Add(new HealthIssue
                {
                    Severity = HealthSeverity.Warning,
                    Title = storage.Nama,
                    Detail = Services.Loc.Format("Ui_Health_OverLimit",
                        over.ToString("N1", Services.AppCulture.Current)),
                    TargetPage = Page.Storage,
                    TargetId = storage.Id,
                });
            }
        }

        foreach (var (storageId, letter) in driveLinks)
        {
            var linkedStorage = storages.FirstOrDefault(s => s.Id == storageId);
            if (!string.IsNullOrWhiteSpace(letter) && !readyDrives.Contains(letter))
            {
                Issues.Add(new HealthIssue
                {
                    Severity = HealthSeverity.Warning,
                    Title = linkedStorage?.Nama ?? letter,
                    Detail = Services.Loc.Format("Ui_Health_DriveMissing", letter),
                    TargetPage = Page.Storage,
                    TargetId = storageId,
                });
            }

            if (!string.IsNullOrWhiteSpace(letter))
            {
                var drive = _driveService.FindDrive(letter);
                var threshold = _settings.Load().LowSpaceThresholdGb;
                if (drive is not null && threshold is { } limit)
                {
                    var freeGb = drive.FreeBytes / 1024.0 / 1024.0 / 1024.0;
                    if (freeGb < limit)
                    {
                        Issues.Add(new HealthIssue
                        {
                            Severity = HealthSeverity.Warning,
                            Title = linkedStorage?.Nama ?? letter,
                            Detail = Services.Loc.Format("Ui_Health_LowSpace", drive.FreeText),
                            TargetPage = Page.Storage,
                            TargetId = storageId,
                        });
                    }
                }
            }
        }

        var duplicates = games
            .Where(g => g.Status is GameStatus.BelumDownload or GameStatus.SudahAda)
            .GroupBy(g => GameNameNormalizer.Normalize(g.Nama))
            .Where(g => g.Key.Length > 0 && g.Count() > 1)
            .ToList();
        if (duplicates.Count > 0)
        {
            Issues.Add(new HealthIssue
            {
                Severity = HealthSeverity.Info,
                Title = Services.Loc.Get("Ui_Nav_Duplicates"),
                Detail = Services.Loc.Format("Ui_Health_DuplicateGroups", duplicates.Count),
                TargetPage = Page.Duplicates,
            });
        }

        var invalid = games.Where(g => g.UkuranGb <= 0).ToList();
        foreach (var game in invalid)
        {
            Issues.Add(new HealthIssue
            {
                Severity = HealthSeverity.Warning,
                Title = game.Nama,
                Detail = Services.Loc.Get("Ui_Health_ZeroSize"),
                TargetPage = Page.GameList,
                TargetId = game.Id,
            });
        }

        var misplaced = games.Where(g =>
            (g.Status is GameStatus.Dikecualikan or GameStatus.Dieliminasi) && g.StorageId is not null).ToList();
        foreach (var game in misplaced)
        {
            Issues.Add(new HealthIssue
            {
                Severity = HealthSeverity.Warning,
                Title = game.Nama,
                Detail = Services.Loc.Get("Ui_Health_MisplacedStatus"),
                TargetPage = Page.GameList,
                TargetId = game.Id,
            });
        }

        var backups = _backup.ListBackups();
        if (backups.Count == 0)
        {
            Issues.Add(new HealthIssue
            {
                Severity = HealthSeverity.Warning,
                Title = Services.Loc.Get("Ui_Import_BackupNow"),
                Detail = Services.Loc.Get("Ui_Health_NoBackup"),
                TargetPage = Page.ImportExport,
            });
        }
        else
        {
            var last = backups.Max(File.GetCreationTimeUtc);
            var age = DateTime.UtcNow - last;
            if (age > TimeSpan.FromDays(7))
            {
                Issues.Add(new HealthIssue
                {
                    Severity = HealthSeverity.Warning,
                    Title = Services.Loc.Get("Ui_Import_BackupNow"),
                    Detail = Services.Loc.Format("Ui_Health_StaleBackup", (int)age.TotalDays),
                    TargetPage = Page.ImportExport,
                });
            }
        }

        var source = _settings.Load().LastImportedPath;
        if (!string.IsNullOrWhiteSpace(source) && !File.Exists(source))
        {
            Issues.Add(new HealthIssue
            {
                Severity = HealthSeverity.Warning,
                Title = Services.Loc.Get("Ui_Import_Section"),
                Detail = Services.Loc.Get("Ui_Health_SourceMissing"),
                TargetPage = Page.ImportExport,
            });
        }

        var warnings = Issues.Count(i => i.Severity == HealthSeverity.Warning);
        SummaryText = Issues.Count == 0
            ? Services.Loc.Get("Ui_Health_AllGood")
            : Services.Loc.Format("Ui_Health_Summary", warnings, Issues.Count - warnings);
    }

    [RelayCommand]
    private void OpenIssue(HealthIssue? issue)
    {
        if (issue is not null)
        {
            OpenIssueRequested?.Invoke(issue);
        }
    }
}
