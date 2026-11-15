using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public interface IRefreshable
{
    Task RefreshAsync();
}

public sealed class StorageCardItem
{
    public required Storage Storage { get; init; }
    public int GameCount { get; init; }
    public double TotalGb { get; init; }
    public double? RemainingGb { get; init; }
    public double? LimitGb { get; init; }
    public double ProgressFraction { get; init; }
    public bool IsOverLimit { get; init; }
    public bool HasLimit { get; init; }

    private static readonly CultureInfo IdId = CultureInfo.GetCultureInfo("id-ID");

    public string TotalText => $"{TotalGb.ToString("N1", IdId)} GB";
    public string GameCountText => $"{GameCount} game";
    public string CapacityText => Storage.KapasitasGb is { } cap
        ? $"dari {cap.ToString("N0", IdId)} GB"
        : "tanpa kapasitas";
    public string RemainingText
    {
        get
        {
            if (RemainingGb is not { } remaining)
            {
                return "Tanpa batas ruang";
            }

            if (IsOverLimit)
            {
                return $"Melebihi batas {Math.Abs(remaining).ToString("N1", IdId)} GB";
            }

            return $"Sisa ruang {remaining.ToString("N1", IdId)} GB";
        }
    }

    public string TipeLabel => Storage.Tipe switch
    {
        StorageType.HddEksternal => "HDD EKSTERNAL",
        StorageType.LaptopHdd => "LAPTOP HDD",
        StorageType.LaptopSsd => "LAPTOP SSD",
        StorageType.Virtual => "VIRTUAL",
        _ => Storage.Tipe.ToString().ToUpperInvariant(),
    };
}

public partial class DashboardViewModel : ObservableObject, IRefreshable
{
    private readonly IStorageRepository _storageRepository;
    private readonly IGameRepository _gameRepository;

    [ObservableProperty]
    private string _totalGamesText = "";

    [ObservableProperty]
    private string _totalSizeText = "";

    [ObservableProperty]
    private string _excludedText = "";

    [ObservableProperty]
    private string _statusText = "";

    public DashboardViewModel(IStorageRepository storageRepository, IGameRepository gameRepository)
    {
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
    }

    public event Action<long>? OpenStorageGamesRequested;

    public ObservableCollection<StorageCardItem> Storages { get; } = new();

    [RelayCommand]
    private void OpenStorage(StorageCardItem? item)
    {
        if (item is not null)
        {
            OpenStorageGamesRequested?.Invoke(item.Storage.Id);
        }
    }

    public Task RefreshAsync()
    {
        return ReloadAsync();
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        var storages = await _storageRepository.GetAllAsync();
        var games = await _gameRepository.GetAllAsync();
        var counted = games.Where(g => CapacityCalculator.CountsTowardTotal(g.Status)).ToList();

        Storages.Clear();
        foreach (var storage in storages)
        {
            var storageGames = counted.Where(g => g.StorageId == storage.Id).ToList();
            var total = storageGames.Sum(g => g.UkuranGb);
            var remaining = CapacityCalculator.RemainingGb(storage, total);
            var progress = CapacityCalculator.ProgressFraction(storage, total);

            Storages.Add(new StorageCardItem
            {
                Storage = storage,
                GameCount = storageGames.Count,
                TotalGb = total,
                RemainingGb = remaining,
                LimitGb = CapacityCalculator.EffectiveLimit(storage),
                ProgressFraction = progress ?? 1.0,
                IsOverLimit = CapacityCalculator.IsOverLimit(storage, total),
                HasLimit = progress.HasValue,
            });
        }

        var totalGames = counted.Count;
        var totalGb = counted.Sum(g => g.UkuranGb);
        TotalGamesText = $"{totalGames} game";
        TotalSizeText = $"{totalGb.ToString("N1", CultureInfo.GetCultureInfo("id-ID"))} GB";
        ExcludedText = $"{games.Count(g => g.Status == GameStatus.Dikecualikan)} dikecualikan | {games.Count(g => g.Status == GameStatus.Dieliminasi)} dieliminasi | {games.Count(g => g.SelesaiMain)} selesai";
        StatusText = $"{Storages.Count} storage";
    }
}
