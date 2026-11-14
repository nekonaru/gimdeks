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
    public string ExpectedText => Row.ExpectedCount is null ? "-" : $"{Row.ExpectedCount} game / {Row.ExpectedGb?.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) ?? "-"} GB";
    public string ActualText => $"{Row.ActualCount} game / {Row.ActualGb.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"))} GB";
    public string ResultText => Row.Matches ? "Cocok" : "Selisih";
    public bool Mismatch => !Row.Matches;
}

public partial class ImportExportViewModel : ObservableObject
{
    private readonly ExcelImportService _importService;
    private readonly ExcelExporter _exporter;
    private readonly IStorageRepository _storageRepository;
    private readonly IGameRepository _gameRepository;
    private readonly DatabaseBackupService _backup;

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _selectedMode = "Gabung";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Pilih file Excel, lihat pratinjau, lalu impor.";

    [ObservableProperty]
    private string _previewSummary = "";

    public ImportExportViewModel(
        ExcelImportService importService,
        IStorageRepository storageRepository,
        IGameRepository gameRepository,
        DatabaseBackupService backup)
    {
        _importService = importService;
        _exporter = new ExcelExporter();
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
        _backup = backup;
        ImportModes = new[] { "Gabung", "Ganti Semua" };
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
            Title = "Pilih Koleksi Game",
        };
        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
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
        StatusText = "Membaca file...";
        Warnings.Clear();
        RingkasanRows.Clear();
        SkippedSheets.Clear();
        try
        {
            var preview = await Task.Run(() => _importService.Preview(FilePath));
            PreviewSummary = $"{preview.TotalStorages} storage, {preview.TotalGames} game terdeteksi.";
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
                    ? "Pratinjau cocok dengan sheet Ringkasan."
                    : "Pratinjau selesai; ada selisih dengan sheet Ringkasan.";
            }
            else
            {
                StatusText = "Pratinjau selesai. Sheet Ringkasan tidak ditemukan; verifikasi dilewati.";
            }
        }
        catch (Exception exception)
        {
            StatusText = "Pratinjau gagal: " + exception.Message;
            DialogService.Error("Pratinjau Gagal", exception.Message);
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

        var mode = SelectedMode == "Ganti Semua" ? ImportMode.GantiSemua : ImportMode.Gabung;
        var warning = mode == ImportMode.GantiSemua
            ? "Mode Ganti Semua akan MENGOSONGKAN semua data yang ada dan menggantinya dengan isi file. Backup otomatis tetap dibuat. Lanjutkan?"
            : "Mode Gabung melewati game dengan nama dan storage yang sama. Lanjutkan?";

        if (!DialogService.Confirm("Import Excel", warning))
        {
            return;
        }

        IsBusy = true;
        StatusText = "Mengimpor...";
        try
        {
            var result = await _importService.ImportAsync(FilePath, mode);
            var skippedText = result.GamesSkippedAsDuplicate > 0 ? $", {result.GamesSkippedAsDuplicate} dilewati (duplikat)" : "";
            StatusText = $"Import selesai: {result.StoragesAdded} storage baru, {result.GamesAdded} game masuk{skippedText}.";
            DataChanged?.Invoke();
            await PreviewAsync(); // refresh the preview panel with the new state
        }
        catch (Exception exception)
        {
            StatusText = "Import gagal: " + exception.Message;
            DialogService.Error("Import Gagal", exception.Message);
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
            Title = "Ekspor ke Excel",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Mengekspor...";
        try
        {
            var storages = await _storageRepository.GetAllAsync();
            var games = await _gameRepository.GetAllAsync();
            await Task.Run(() => _exporter.Export(dialog.FileName, storages, games));
            StatusText = $"Diekspor ke {dialog.FileName}.";
        }
        catch (Exception exception)
        {
            StatusText = "Export gagal: " + exception.Message;
            DialogService.Error("Export Gagal", exception.Message);
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
            StatusText = $"Backup tersimpan di {destination}.";
        }
        catch (Exception exception)
        {
            StatusText = "Backup gagal: " + exception.Message;
            DialogService.Error("Backup Gagal", exception.Message);
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
            Title = "Pulihkan dari Backup",
            InitialDirectory = Directory.Exists(_backup.BackupsDirectory) ? _backup.BackupsDirectory : null,
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (!DialogService.Confirm("Pulihkan Backup", "Seluruh data sekarang akan diganti dengan isi file backup. Backup sebelum restore tetap dibuat. Lanjutkan?"))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _backup.RestoreAsync(dialog.FileName);
            StatusText = "Data dipulihkan dari backup.";
            DataChanged?.Invoke();
        }
        catch (Exception exception)
        {
            StatusText = "Pulihkan gagal: " + exception.Message;
            DialogService.Error("Pulihkan Gagal", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool EnsureFile()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            StatusText = "Pilih file Excel yang valid dulu.";
            DialogService.Error("File Belum Dipilih", "Pilih file .xlsx yang valid terlebih dahulu.");
            return false;
        }

        return true;
    }
}
