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
    public bool DriveDisconnected { get; init; }
    public string DriveDisconnectedText { get; init; } = "";

    private static CultureInfo IdId => Services.AppCulture.Current;



    public string TotalText => $"{TotalGb.ToString("N1", IdId)} GB";
    public string GameCountText => Services.Loc.Format("Ui_GameCount", GameCount);
    public string CapacityText => Storage.KapasitasGb is { } cap
        ? Services.Loc.Format("Ui_CapacityFrom", cap.ToString("N0", IdId))
        : Services.Loc.Get("Ui_CapacityNone");
    public string RemainingText
    {
        get
        {
            if (RemainingGb is not { } remaining)
            {
                return Services.Loc.Get("Ui_NoLimit");
            }

            if (IsOverLimit)
            {
                return Services.Loc.Format("Ui_OverBy", Math.Abs(remaining).ToString("N1", IdId));
            }

            return Services.Loc.Format("Ui_Remaining", remaining.ToString("N1", IdId));
        }
    }

    public string TipeLabel => Storage.Tipe switch
    {
        StorageType.HddEksternal => Services.Loc.Get("Ui_StorageType_HddEksternal"),
        StorageType.LaptopHdd => Services.Loc.Get("Ui_StorageType_LaptopHdd"),
        StorageType.LaptopSsd => Services.Loc.Get("Ui_StorageType_LaptopSsd"),
        StorageType.Virtual => Services.Loc.Get("Ui_StorageType_Virtual"),
        _ => Services.Loc.Get("Ui_StorageType_Virtual"),
    };
}

public partial class DashboardViewModel : ObservableObject, IRefreshable
{
    private readonly IStorageRepository _storageRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IDriveLinkRepository _driveLinks;
    private readonly Services.DriveService _driveService;

    [ObservableProperty]
    private string _totalGamesText = "";

    [ObservableProperty]
    private string _totalSizeText = "";

    [ObservableProperty]
    private string _excludedText = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _hasAnyGames;

    public DashboardViewModel(IStorageRepository storageRepository, IGameRepository gameRepository, IDriveLinkRepository driveLinks, Services.DriveService driveService)
    {
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
        _driveLinks = driveLinks;
        _driveService = driveService;
    }

    public event Action<long>? OpenStorageGamesRequested;
    public event Action<long>? OpenRandomGameRequested;

    public ObservableCollection<StorageCardItem> Storages { get; } = new();

    [RelayCommand]
    private void OpenStorage(StorageCardItem? item)
    {
        if (item is not null)
        {
            OpenStorageGamesRequested?.Invoke(item.Storage.Id);
        }
    }

    [RelayCommand]
    private async Task PickRandomGameAsync()
    {
        var games = await _gameRepository.GetAllAsync();
        var candidates = games
            .Where(g => g.Status == GameStatus.SudahAda && !g.SelesaiMain)
            .ToList();

        if (candidates.Count == 0)
        {
            StatusText = Services.Loc.Get("Ui_Dashboard_NoPickCandidates");
            return;
        }

        var pick = candidates[Random.Shared.Next(candidates.Count)];
        StatusText = Services.Loc.Format("Ui_Dashboard_Picked", pick.Nama);
        OpenRandomGameRequested?.Invoke(pick.Id);
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
        var driveLinks = await _driveLinks.GetAllAsync();
        var gamesByStorage = counted.Where(g => g.StorageId is not null)
            .GroupBy(g => g.StorageId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var readyDrives = _driveService.GetReadyDriveLetters();
        foreach (var storage in storages)
        {
            gamesByStorage.TryGetValue(storage.Id, out var storageGames);
            storageGames ??= new List<Game>();
            var total = storageGames.Sum(g => g.UkuranGb);
            var remaining = CapacityCalculator.RemainingGb(storage, total);
            var progress = CapacityCalculator.ProgressFraction(storage, total);
            var driveDisconnected = driveLinks.TryGetValue(storage.Id, out var letter)
                && (string.IsNullOrWhiteSpace(letter) || !readyDrives.Contains(letter));

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
                DriveDisconnected = driveDisconnected,
                DriveDisconnectedText = driveDisconnected ? Services.Loc.Format("Ui_Storage_DriveNotConnected", driveLinks[storage.Id]) : "",
            });
        }

        var totalGames = counted.Count;
        var totalGb = counted.Sum(g => g.UkuranGb);
        TotalGamesText = Services.Loc.Format("Ui_GameCount", totalGames);
        TotalSizeText = $"{totalGb.ToString("N1", GimDeks.App.Services.AppCulture.Current)} GB";
        ExcludedText = Services.Loc.Format("Ui_ExcludedSummary",
            games.Count(g => g.Status == GameStatus.Dikecualikan),
            games.Count(g => g.Status == GameStatus.Dieliminasi),
            games.Count(g => g.SelesaiMain));
        HasAnyGames = games.Count > 0;
        StatusText = $"{Storages.Count} storage";
    }
}
