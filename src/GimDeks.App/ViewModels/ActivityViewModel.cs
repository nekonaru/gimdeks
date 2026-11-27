using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;

namespace GimDeks.App.ViewModels;

public sealed class ActivityRow
{
    public required ActivityEntry Entry { get; init; }
    public string TimeText => Entry.Waktu.Length >= 16 ? Entry.Waktu[..16].Replace('T', ' ') : Entry.Waktu;
    public string ActionText => Services.Loc.Get("Ui_Activity_" + Entry.Aksi.Replace("-", "_"));
}

public partial class ActivityViewModel : ObservableObject, IRefreshable
{
    private readonly IActivityRepository _activity;

    public ActivityViewModel(IActivityRepository activity) => _activity = activity;

    public ObservableCollection<ActivityRow> Rows { get; } = new();

    [ObservableProperty]
    private string _statusText = "";

    public Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        Rows.Clear();
        var entries = await Task.Run(() => _activity.GetRecentAsync(200));
        foreach (var entry in entries)
        {
            Rows.Add(new ActivityRow { Entry = entry });
        }
        StatusText = Services.Loc.Format("Ui_Activity_Count", Rows.Count);
    }
}
