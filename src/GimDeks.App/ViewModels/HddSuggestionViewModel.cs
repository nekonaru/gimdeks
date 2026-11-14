using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public sealed class HddSuggestionSlot
{
    public required Storage Storage { get; init; }
    public required List<Game> Games { get; init; }
    public double TotalGb { get; init; }
    public double? LimitGb { get; init; }

    private static readonly CultureInfo IdId = CultureInfo.GetCultureInfo("id-ID");

    public string TotalText => $"{TotalGb.ToString("N1", IdId)} GB";
    public string LimitText => LimitGb is { } limit ? $"dari batas {limit.ToString("N0", IdId)} GB" : "tanpa batas";
    public double FillFraction => LimitGb is { } limit && limit > 0 ? Math.Clamp(TotalGb / limit, 0.0, 1.0) : 1.0;
    public string GameCountText => $"{Games.Count} game";
}

public partial class HddSuggestionViewModel : ObservableObject, IRefreshable
{
    private readonly IStorageRepository _storageRepository;
    private readonly IGameRepository _gameRepository;

    [ObservableProperty]
    private string _summaryText = "";

    public HddSuggestionViewModel(IStorageRepository storageRepository, IGameRepository gameRepository)
    {
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
        Slots = new ObservableCollection<HddSuggestionSlot>();
        UnassignedGames = new ObservableCollection<string>();
        Warnings = new ObservableCollection<string>();
    }

    public ObservableCollection<HddSuggestionSlot> Slots { get; }
    public ObservableCollection<string> UnassignedGames { get; }
    public ObservableCollection<string> Warnings { get; }

    public Task RefreshAsync() => RecomputeAsync();

    [RelayCommand]
    private async Task RecomputeAsync()
    {
        var storages = (await _storageRepository.GetAllAsync())
            .Where(s => s.Tipe == StorageType.HddEksternal)
            .OrderBy(s => s.Urutan)
            .ToList();
        var games = (await _gameRepository.GetAllAsync())
            .Where(g => g.Status == GameStatus.BelumDownload)
            .OrderByDescending(g => g.UkuranGb)
            .ToList();

        var limits = storages.Select(s => (double?)(s.BatasGb ?? s.KapasitasGb)).ToList();
        var loads = new double[storages.Count];
        var assignment = storages.Select(_ => new List<Game>()).ToList();
        var unassigned = new List<Game>();

        foreach (var game in games)
        {
            var placed = false;
            for (var index = 0; index < storages.Count; index++)
            {
                var limit = limits[index];
                if (loads[index] + game.UkuranGb <= (limit ?? double.MaxValue))
                {
                    assignment[index].Add(game);
                    loads[index] += game.UkuranGb;
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                unassigned.Add(game);
            }
        }

        Slots.Clear();
        for (var index = 0; index < storages.Count; index++)
        {
            Slots.Add(new HddSuggestionSlot
            {
                Storage = storages[index],
                Games = assignment[index],
                TotalGb = loads[index],
                LimitGb = limits[index],
            });
        }

        Warnings.Clear();
        foreach (var slot in Slots)
        {
            Warnings.Add($"{slot.Storage.Nama}: {slot.TotalText} / {slot.LimitText}");
        }

        UnassignedGames.Clear();
        foreach (var game in unassigned)
        {
            UnassignedGames.Add($"{game.Nama} ({game.UkuranGb.ToString("N1", CultureInfo.GetCultureInfo("id-ID"))} GB)");
        }

        if (storages.Count == 0)
        {
            SummaryText = "Belum ada storage HDD Eksternal.";
        }
        else if (games.Count == 0)
        {
            SummaryText = "Tidak ada game berstatus Belum Download.";
        }
        else if (unassigned.Count == 0)
        {
            SummaryText = $"Semua {games.Count} game muat di {storages.Count} HDD Eksternal.";
        }
        else
        {
            SummaryText = $"{games.Count - unassigned.Count} dari {games.Count} game muat; {unassigned.Count} tidak ada tempatnya.";
        }
    }
}
