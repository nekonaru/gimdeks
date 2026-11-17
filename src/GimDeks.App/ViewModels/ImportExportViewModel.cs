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

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _selectedMode = Services.Loc.Get("Ui_Import_ModeGabung");

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = Services.Loc.Get("Ui_Import_PickFile");

    [ObservableProperty]
    private string _previewSummary = "";

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
        Services.PortableModeService portable)
    {
        _importService = importService;
        _exporter = new ExcelExporter();
        _csvExporter = csvExporter;
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
        _backup = backup;
        _settings = settings;
        _portable = portable;
        ImportModes = new[] { Services.Loc.Get("Ui_Import_ModeGabung"), Services.Loc.Get("Ui_Import_ModeGantiSemua") };
        Warnings = new ObservableCollection<string>();
        RingkasanRows = new ObservableCollection<RingkasanRowView>();
        SkippedSheets = new ObservableCollection<string>();
    }

    /// <summary>Raised after any operation that changes stored data.</summary>
    public event Action? DataChanged;

    public IReadOnlyList<string> ImportModes { get; }
    public ObservableCollection<string> Warnings { get; }
    public ObservableCollection<RingkasanRowView> RingkasanRows { get; }
    public ObservableCollection<string> SkippedSheets { get; }

    public IReadOnlyList<string> FileFilters => new[] { "File Excel (*.xlsx)|*.xlsx" };

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (!EnsureFile())
        {
            return;
        }

        var mode = SelectedMode == Services.Loc.Get("Ui_Import_ModeGantiSemua") ? ImportMode.GantiSemua : ImportMode.Gabung;
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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
