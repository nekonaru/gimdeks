using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.App.ViewModels;

public sealed class MiniGameRow
{
    public required Game Game { get; init; }
    public string Detail => Game.JumlahMain == 0
        ? Services.Loc.Get("Ui_Col_NeverPlayed")
        : Services.Loc.Format("Ui_Col_PlayedTimes", Game.JumlahMain);
}

public partial class MiniPlayerViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _games;
    private readonly Services.SessionTrackerService _tracker;

    public MiniPlayerViewModel(IGameRepository games, Services.SessionTrackerService tracker)
    {
        _games = games;
        _tracker = tracker;
    }

    public ObservableCollection<MiniGameRow> Favorites { get; } = new();

    [ObservableProperty]
    private string _statusText = "";

    public Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        Favorites.Clear();
        var games = await Task.Run(() => _games.GetAllAsync());
        foreach (var game in games.Where(g => g.Favorit).Take(12))
        {
            Favorites.Add(new MiniGameRow { Game = game });
        }
    }

    [RelayCommand]
    private async Task LaunchAsync(MiniGameRow? row)
    {
        if (row is null)
        {
            return;
        }
        if (!Services.GameLauncherService.IsValidExecutable(row.Game.PathExe))
        {
            StatusText = Services.Loc.Get("Ui_GameList_NoExe");
            return;
        }
        if (await _tracker.LaunchAsync(row.Game))
        {
            StatusText = Services.Loc.Format("Ui_GameList_Launched", row.Game.Nama);
        }
        else
        {
            StatusText = Services.Loc.Get("Ui_GameList_LaunchFailed");
        }
    }

    [RelayCommand]
    private async Task PickRandomAsync()
    {
        var candidates = Favorites.Where(r => Services.GameLauncherService.IsValidExecutable(r.Game.PathExe)).ToList();
        if (candidates.Count == 0)
        {
            StatusText = Services.Loc.Get("Ui_Mini_NoCandidates");
            return;
        }
        var row = candidates[Random.Shared.Next(candidates.Count)];
        await LaunchAsync(row);
    }
}
