using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Excel;
using Microsoft.Win32;

namespace GimDeks.App.ViewModels;

public sealed class RingkasanRowView
{
    public required RingkasanRow Row { get; init; }

    public string Label => Row.Label;
    public string ExpectedText => Row.ExpectedCount is null ? "-" : Services.Loc.Format("Ui_Import_Expected", Row.ExpectedCount, Row.ExpectedGb?.ToString("0.0", GimDeks.App.Services.AppCulture.Current) ?? "-");
    public string ActualText => Services.Loc.Format("Ui_Import_Actual", Row.ActualCount, Row.ActualGb.ToString("0.0", GimDeks.App.Services.AppCulture.Current));
    public string ResultText => Row.Matches ? Services.Loc.Get("Ui_Import_ResultMatch") : Services.Loc.Get("Ui_Import_ResultMismatch");
    public bool Mismatch => !Row.Matches;
}

public sealed class ImportModeOption
{
    private readonly string _labelKey;

    public ImportModeOption(ImportMode value, string labelKey)
    {
        Value = value;
        _labelKey = labelKey;
    }

    public ImportMode Value { get; }
    public string Label => Services.Loc.Get(_labelKey);
    public override string ToString() => Label;
}

public partial class ImportExportViewModel : ObservableObject
{
    private readonly ExcelImportService _importService;
    private readonly ExcelExporter _exporter;
    private readonly CsvExporter _csvExporter;
    private readonly IStorageRepository _storageRepository;
    private readonly IGameRepository _gameRepository;
    private readonly DatabaseBackupService _backup;
    private readonly Services.AppSettingsService _settings;
    private readonly Services.PortableModeService _portable;
    private readonly IImportHistoryRepository _history;
    private readonly CsvImportService _csvImport;
    private readonly CollectionBundleService _bundle;

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private ImportModeOption _selectedMode;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = Services.Loc.Get("Ui_Import_PickFile");

    [ObservableProperty]
    private string _previewSummary = "";

    [ObservableProperty]
    private string _diffText = "";

    public bool CanRun => !IsBusy;

    partial void OnIsBusyChanged(bool value) => NotifyCanExecuteChangedForAll();

    private void NotifyCanExecuteChangedForAll()
    {
        BrowseCommand.NotifyCanExecuteChanged();
        PreviewCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
        PreviewDiffCommand.NotifyCanExecuteChanged();
        ImportIncrementalCommand.NotifyCanExecuteChanged();
        ImportCsvCommand.NotifyCanExecuteChanged();
        CreateTemplateCommand.NotifyCanExecuteChanged();
        ExportBundleCommand.NotifyCanExecuteChanged();
        ImportBundleCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        ExportCsvCommand.NotifyCanExecuteChanged();
        BackupCommand.NotifyCanExecuteChanged();
        RestoreCommand.NotifyCanExecuteChanged();
        EnablePortableCommand.NotifyCanExecuteChanged();
        DisablePortableCommand.NotifyCanExecuteChanged();
    }

    public bool IsPortable => _portable.IsPortable;

    public string PortableStatusText => IsPortable
        ? Services.Loc.Get("Ui_Import_PortableActive")
        : Services.Loc.Get("Ui_Import_PortableInactive");

    public ImportExportViewModel(
        ExcelImportService importService,
        CsvExporter csvExporter,
        IStorageRepository storageRepository,
        IGameRepository gameRepository,
        DatabaseBackupService backup,
        Services.AppSettingsService settings,
        Services.PortableModeService portable,
        IImportHistoryRepository history,
        CsvImportService csvImport,
        CollectionBundleService bundle)
    {
        _importService = importService;
        _exporter = new ExcelExporter();
        _csvExporter = csvExporter;
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
        _backup = backup;
        _settings = settings;
        _portable = portable;
        _history = history;
        _csvImport = csvImport;
        _bundle = bundle;
        ImportModes = new[]
        {
            new ImportModeOption(ImportMode.Gabung, "Ui_Import_ModeGabung"),
            new ImportModeOption(ImportMode.GantiSemua, "Ui_Import_ModeGantiSemua"),
        };
        _selectedMode = ImportModes[0];
        Warnings = new ObservableCollection<string>();
        RingkasanRows = new ObservableCollection<RingkasanRowView>();
        SkippedSheets = new ObservableCollection<string>();
        _ = LoadHistoryAsync();
    }

    /// <summary>Raised after any operation that changes stored data.</summary>
    public event Action? DataChanged;

    public ObservableCollection<ImportHistory> History { get; } = new();

    public async Task LoadHistoryAsync()
    {
        History.Clear();
        foreach (var entry in await _history.GetAllAsync())
        {
            History.Add(entry);
        }
    }

    public IReadOnlyList<ImportModeOption> ImportModes { get; }
    public ObservableCollection<string> Warnings { get; }
    public ObservableCollection<RingkasanRowView> RingkasanRows { get; }
    public ObservableCollection<string> SkippedSheets { get; }

    public IReadOnlyList<string> FileFilters => new[] { "File Excel (*.xlsx)|*.xlsx" };

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Browse()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "File Excel (*.xlsx)|*.xlsx",
            Title = Services.Loc.Get("Ui_Import_OpenTitle"),
        };
        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
            _settings.SaveLastImportedPath(FilePath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PreviewAsync()
    {
        if (!EnsureFile())
        {
            return;
        }

        IsBusy = true;
        StatusText = Services.Loc.Get("Ui_Import_ReadingFile");
        Warnings.Clear();
        RingkasanRows.Clear();
        SkippedSheets.Clear();
        try
        {
            var preview = await Task.Run(() => _importService.Preview(FilePath));
            PreviewSummary = Services.Loc.Format("Ui_Import_Detected", preview.TotalStorages, preview.TotalGames);
            foreach (var warning in preview.Warnings)
            {
                Warnings.Add(warning);
            }

            foreach (var skipped in preview.SkippedSheets)
            {
                SkippedSheets.Add(skipped);
            }

            if (preview.Ringkasan is not null)
            {
                foreach (var row in preview.Ringkasan.Rows)
                {
                    RingkasanRows.Add(new RingkasanRowView { Row = row });
                }

                StatusText = preview.Ringkasan.AllMatch
                    ? Services.Loc.Get("Ui_Import_RingkasanMatch")
                    : Services.Loc.Get("Ui_Import_RingkasanMismatch");
            }
            else
            {
                StatusText = Services.Loc.Get("Ui_Import_NoRingkasan");
            }
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_PreviewFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_ImportPreview"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PreviewDiffAsync()
    {
        if (!EnsureFile())
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (newCount, changedCount) = await _importService.DiffAsync(FilePath);
            DiffText = Services.Loc.Format("Ui_Import_Diff", newCount, changedCount);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ImportIncrementalAsync()
    {
        if (!EnsureFile())
        {
            return;
        }

        if (!DialogService.Confirm(Services.Loc.Get("Ui_Import_Title"), Services.Loc.Get("Ui_Import_IncrementalConfirm")))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _importService.ImportIncrementalAsync(FilePath);
            _settings.SaveLastImportedPath(FilePath);
            await _history.AddAsync(FilePath, "Inkremental", System.Text.Json.JsonSerializer.Serialize(new
            {
                result.GamesAdded,
                result.GamesSkippedAsDuplicate,
            }));
            await LoadHistoryAsync();
            StatusText = Services.Loc.Format("Ui_Import_IncrementalDone", result.GamesAdded, result.GamesSkippedAsDuplicate);
            DiffText = "";
            DataChanged?.Invoke();
            await PreviewAsync();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ImportFailed", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ImportAsync()
    {
        if (!EnsureFile())
        {
            return;
        }

        var mode = SelectedMode?.Value ?? ImportMode.Gabung;
        var warning = mode == ImportMode.GantiSemua
            ? Services.Loc.Get("Ui_Import_GantiSemuaConfirm")
            : Services.Loc.Get("Ui_Import_GabungConfirm");

        if (!DialogService.Confirm(Services.Loc.Get("Ui_Dialog_ImportTitle"), warning))
        {
            return;
        }

        IsBusy = true;
        StatusText = Services.Loc.Get("Ui_Import_Importing");
        try
        {
            var result = await _importService.ImportAsync(FilePath, mode);
            _settings.SaveLastImportedPath(FilePath);
            await _history.AddAsync(FilePath, mode.ToString(), System.Text.Json.JsonSerializer.Serialize(new
            {
                result.StoragesAdded,
                result.GamesAdded,
                result.GamesSkippedAsDuplicate,
                AllMatch = result.Ringkasan?.AllMatch ?? true,
            }));
            await LoadHistoryAsync();
            var skippedText = result.GamesSkippedAsDuplicate > 0 ? Services.Loc.Format("Ui_Import_SkippedDup", result.GamesSkippedAsDuplicate) : "";
            StatusText = Services.Loc.Format("Ui_Import_Done", result.StoragesAdded, result.GamesAdded, skippedText);
            DataChanged?.Invoke();
            await PreviewAsync(); // refresh the preview panel with the new state
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ImportFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_ImportFailed"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportBundleAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Collection bundle (*.zip)|*.zip",
            FileName = "gimdeks-collection.zip",
            Title = Services.Loc.Get("Ui_Bundle_ExportTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        IsBusy = true;
        try
        {
            var path = dialog.FileName;
            await Task.Run(() => _bundle.Export(path));
            StatusText = Services.Loc.Get("Ui_Bundle_Exported");
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ImportFailed", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ImportBundleAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Collection bundle (*.zip)|*.zip",
            Title = Services.Loc.Get("Ui_Bundle_ImportTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        IsBusy = true;
        try
        {
            var path = dialog.FileName;
            var verification = await Task.Run(() => _bundle.Verify(path));
            if (!verification.Ok)
            {
                StatusText = Services.Loc.Format("Ui_Bundle_Failed", verification.Message);
                return;
            }
            if (!DialogService.Confirm(Services.Loc.Get("Ui_Bundle_ImportTitle"), Services.Loc.Get("Ui_Bundle_Confirm")))
            {
                return;
            }
            await Task.Run(() => _bundle.Import(path));
            StatusText = Services.Loc.Get("Ui_Bundle_Imported");
            DataChanged?.Invoke();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Bundle_Failed", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CreateTemplateAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Koleksi Game Template.xlsx",
            Title = Services.Loc.Get("Ui_Import_TemplateTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var path = dialog.FileName;
            await Task.Run(() => ExcelTemplateService.Create(path));
            StatusText = Services.Loc.Get("Ui_Import_TemplateCreated");
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ImportFailed", exception.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ImportCsvAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            Title = Services.Loc.Get("Ui_Import_CsvTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => _csvImport.Preview(dialog.FileName));
            if (preview.Rows.Count == 0)
            {
                StatusText = Services.Loc.Get("Ui_Import_CsvEmpty");
                return;
            }

            if (!DialogService.Confirm(Services.Loc.Get("Ui_Import_CsvTitle"),
                    Services.Loc.Format("Ui_Import_CsvConfirm", preview.Rows.Count)))
            {
                return;
            }

            var result = await _csvImport.ImportAsync(preview, dialog.FileName);
            _settings.SaveLastImportedPath(dialog.FileName);
            await _history.AddAsync(dialog.FileName, "CSV", System.Text.Json.JsonSerializer.Serialize(new
            {
                result.Added,
                result.Skipped,
            }));
            await LoadHistoryAsync();
            StatusText = Services.Loc.Format("Ui_Import_CsvDone", result.Added, result.Skipped);
            DataChanged?.Invoke();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ImportFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_ImportFailed"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "File Excel (*.xlsx)|*.xlsx",
            FileName = "Koleksi_Game.xlsx",
            Title = Services.Loc.Get("Ui_Import_ExportTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        StatusText = Services.Loc.Get("Ui_Import_Exporting");
        try
        {
            var storages = await _storageRepository.GetAllAsync();
            var games = await _gameRepository.GetAllAsync();
            await Task.Run(() => _exporter.Export(dialog.FileName, storages, games));
            StatusText = Services.Loc.Format("Ui_Import_ExportDone", dialog.FileName);
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ExportFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_ExportFailed"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "File CSV (*.csv)|*.csv",
            FileName = "Koleksi_Game.csv",
            Title = Services.Loc.Get("Ui_Import_ExportCsvTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        StatusText = Services.Loc.Get("Ui_Import_Exporting");
        try
        {
            await _csvExporter.ExportAsync(dialog.FileName);
            StatusText = Services.Loc.Format("Ui_Import_ExportDone", dialog.FileName);
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_ExportCsvFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_ExportFailed"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task BackupAsync()
    {
        IsBusy = true;
        try
        {
            var destination = await _backup.BackupAsync();
            StatusText = Services.Loc.Format("Ui_Import_BackupDone", destination);
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_BackupFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_BackupFailed"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RestoreAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "GimDeks backup (*.db)|*.db",
            Title = Services.Loc.Get("Ui_Import_RestoreTitle"),
            InitialDirectory = Directory.Exists(_backup.BackupsDirectory) ? _backup.BackupsDirectory : null,
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (!DialogService.Confirm(Services.Loc.Get("Ui_Import_RestoreTitle"), Services.Loc.Get("Ui_Import_RestoreConfirm")))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _backup.RestoreAsync(dialog.FileName);
            StatusText = Services.Loc.Get("Ui_Import_RestoreDone");
            DataChanged?.Invoke();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Import_RestoreFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_RestoreFailed"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task EnablePortableAsync()
    {
        if (IsPortable)
        {
            return;
        }

        if (!DialogService.Confirm(
                Services.Loc.Get("Ui_Portable_EnableTitle"),
                Services.Loc.Get("Ui_Portable_EnableConfirm")))
        {
            return;
        }

        try
        {
            var target = await _portable.EnableAsync();
            DialogService.Info(Services.Loc.Get("Ui_Dialog_PortableMode"), Services.Loc.Format("Ui_Portable_EnableDone", target));
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Portable_EnableFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_PortableFailed"), exception.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task DisablePortableAsync()
    {
        if (!IsPortable)
        {
            return;
        }

        if (!DialogService.Confirm(
                Services.Loc.Get("Ui_Dialog_NormalMode"),
                Services.Loc.Get("Ui_Portable_DisableConfirm")))
        {
            return;
        }

        try
        {
            var target = await _portable.DisableAsync();
            DialogService.Info(Services.Loc.Get("Ui_Dialog_NormalMode"), Services.Loc.Format("Ui_Portable_EnableDone", target));
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Portable_DisableFailed", exception.Message);
            DialogService.Error(Services.Loc.Get("Ui_Dialog_PortableFailed"), exception.Message);
        }
    }

    /// <summary>Set the import file path programmatically (e.g. from drag and drop) and run the preview.</summary>
    public void BeginImportWithFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            DialogService.Error(Services.Loc.Get("Ui_Import_FileTitle"), Services.Loc.Get("Ui_Import_SelectFile"));
            return;
        }

        FilePath = path;
        _settings.SaveLastImportedPath(path);
        _ = PreviewAsync();
    }

    private bool EnsureFile()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            StatusText = Services.Loc.Get("Ui_Import_SelectFile");
            DialogService.Error(Services.Loc.Get("Ui_Import_FileTitle"), Services.Loc.Get("Ui_Import_SelectFile"));
            return false;
        }

        return true;
    }
}

