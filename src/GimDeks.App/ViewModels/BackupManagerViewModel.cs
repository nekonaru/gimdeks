using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Data;
using Microsoft.Data.Sqlite;

namespace GimDeks.App.ViewModels;

public sealed class BackupItem
{
    public required string Path { get; init; }
    public string Name => System.IO.Path.GetFileName(Path);
    public string DateText
    {
        get
        {
            var name = Name.Replace("gimdeks-backup-", "").Replace(".db", "");
            return name.Length >= 15 ? name[..15] : name;
        }
    }
    public string SizeText
    {
        get
        {
            try
            {
                var bytes = new System.IO.FileInfo(Path).Length;
                return $"{bytes / 1024.0 / 1024.0:0.0} MB";
            }
            catch
            {
                return "-";
            }
        }
    }
}

public partial class BackupManagerViewModel : ObservableObject, IRefreshable
{
    private readonly DatabaseBackupService _backup;

    public BackupManagerViewModel(DatabaseBackupService backup)
    {
        _backup = backup;
    }

    public ObservableCollection<BackupItem> Backups { get; } = new();

    [ObservableProperty]
    private BackupItem? _selectedBackup;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _previewText = "";

    [ObservableProperty]
    private bool _isBusy;

    public event Action? DataChanged;

    public Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Backups.Clear();
            foreach (var path in _backup.ListBackups())
            {
                Backups.Add(new BackupItem { Path = path });
            }
            SelectedBackup = Backups.FirstOrDefault();
            StatusText = Services.Loc.Format("Ui_Backup_Count", Backups.Count);
            await UpdatePreviewAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedBackupChanged(BackupItem? value) => _ = UpdatePreviewAsync();

    private async Task UpdatePreviewAsync()
    {
        if (SelectedBackup is null)
        {
            PreviewText = "";
            return;
        }

        try
        {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = SelectedBackup.Path,
                Mode = SqliteOpenMode.ReadOnly,
            }.ToString());
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT (SELECT COUNT(1) FROM storage), (SELECT COUNT(1) FROM game);";
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                PreviewText = Services.Loc.Format("Ui_Backup_Preview", reader.GetInt32(0), reader.GetInt32(1));
            }
        }
        catch (Exception exception)
        {
            PreviewText = Services.Loc.Format("Ui_Backup_PreviewFailed", exception.Message);
        }
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        IsBusy = true;
        try
        {
            await _backup.BackupAsync();
            StatusText = Services.Loc.Get("Ui_Backup_Created");
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (SelectedBackup is null)
        {
            StatusText = Services.Loc.Get("Ui_Backup_SelectFirst");
            return;
        }

        if (!Services.DialogService.Confirm(Services.Loc.Get("Ui_Import_Restore"),
                Services.Loc.Format("Ui_Backup_RestoreConfirm", SelectedBackup.Name)))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _backup.RestoreAsync(SelectedBackup.Path);
            StatusText = Services.Loc.Get("Ui_Backup_Restored");
            DataChanged?.Invoke();
            await LoadAsync();
        }
        catch (Exception exception)
        {
            StatusText = Services.Loc.Format("Ui_Backup_RestoreFailed", exception.Message);
            Services.DialogService.Error(Services.Loc.Get("Ui_Import_Restore"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "\"" + _backup.BackupsDirectory + "\"",
                UseShellExecute = false,
            });
        }
        catch (Exception exception)
        {
            Services.CrashLog.Write(exception);
        }
    }
}
