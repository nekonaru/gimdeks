using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public sealed class DuplicateMember
{
    public required Game Game { get; init; }
    public required string StorageName { get; init; }
    public required bool IsKeeper { get; init; }

    private static readonly CultureInfo IdId = CultureInfo.GetCultureInfo("id-ID");

    public string UkuranText => $"{Game.UkuranGb.ToString("N1", IdId)} GB";
    public string StatusText => Game.Status switch
    {
        GameStatus.BelumDownload => "Belum Download",
        GameStatus.SudahAda => "Sudah Ada",
        _ => Game.Status.ToString(),
    };
}

public sealed class DuplicateGroup
{
    public required string Key { get; init; }
    public required List<DuplicateMember> Members { get; init; }
}

public partial class DuplicatesViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _gameRepository;
    private readonly IStorageRepository _storageRepository;

    [ObservableProperty]
    private string _summaryText = "";

    [ObservableProperty]
    private string _statusText = "";

    public DuplicatesViewModel(IGameRepository gameRepository, IStorageRepository storageRepository)
    {
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        Groups = new ObservableCollection<DuplicateGroup>();
    }

    public ObservableCollection<DuplicateGroup> Groups { get; }

    public Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ReloadAsync()
    {
        var games = (await _gameRepository.GetAllAsync())
            .Where(g => g.Status is GameStatus.BelumDownload or GameStatus.SudahAda)
            .ToList();
        var storageNames = (await _storageRepository.GetAllAsync()).ToDictionary(s => s.Id, s => s.Nama);

        Groups.Clear();
        var groups = games
            .GroupBy(g => GameNameNormalizer.Normalize(g.Nama))
            .Where(g => g.Count() > 1 && g.Key.Length > 0)
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);

        foreach (var group in groups)
        {
            var members = group
                .OrderBy(g => g.Urutan)
                .ThenBy(g => g.Id)
                .Select((g, index) => new DuplicateMember
                {
                    Game = g,
                    StorageName = g.StorageId is { } id && storageNames.TryGetValue(id, out var name) ? name : "Tanpa Storage",
                    IsKeeper = index == 0,
                })
                .ToList();

            Groups.Add(new DuplicateGroup { Key = group.Key, Members = members });
        }

        SummaryText = Groups.Count == 0
            ? "Tidak ada duplikat terdeteksi."
            : $"{Groups.Count} grup duplikat ({Groups.Sum(g => g.Members.Count)} game)";
        StatusText = "";
    }

    [RelayCommand]
    private async Task DeleteDuplicatesAsync(DuplicateGroup? group)
    {
        if (group is null)
        {
            return;
        }

        var losers = group.Members.Where(m => !m.IsKeeper).ToList();
        if (!DialogService.Confirm(
                "Hapus Duplikat",
                $"Hapus {losers.Count} duplikat dari grup \"{group.Key}\"? Yang tersisa: {group.Members.First(m => m.IsKeeper).Game.Nama}."))
        {
            return;
        }

        foreach (var member in losers)
        {
            await _gameRepository.DeleteAsync(member.Game.Id);
        }

        StatusText = $"{losers.Count} duplikat dihapus.";
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task EliminateDuplicatesAsync(DuplicateGroup? group)
    {
        if (group is null)
        {
            return;
        }

        var losers = group.Members.Where(m => !m.IsKeeper).ToList();
        if (!DialogService.Confirm(
                "Eliminasi Duplikat",
                $"Tandai {losers.Count} duplikat dari grup \"{group.Key}\" sebagai Dieliminasi?"))
        {
            return;
        }

        foreach (var member in losers)
        {
            var game = member.Game;
            game.Status = GameStatus.Dieliminasi;
            game.StorageId = null;
            game.Alasan = $"Duplikat dari {group.Members.First(m => m.IsKeeper).Game.Nama}";
            await _gameRepository.UpdateAsync(game);
        }

        StatusText = $"{losers.Count} duplikat ditandai Dieliminasi.";
        await ReloadAsync();
    }
}
