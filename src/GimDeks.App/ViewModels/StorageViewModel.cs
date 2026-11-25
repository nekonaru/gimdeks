using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public sealed class StorageListItem
{
    public required Storage Storage { get; init; }
    public int GameCount { get; init; }
    public double TotalGb { get; init; }

    private static CultureInfo IdId => Services.AppCulture.Current;

    public string GameCountText => Services.Loc.Format("Ui_GameCount", GameCount);
    public string TotalText => $"{TotalGb.ToString("N1", IdId)} GB";
    public string TipeLabel => Storage.Tipe switch
    {
        StorageType.HddEksternal => Services.Loc.Get("Ui_StorageType_HddEksternal"),
        StorageType.LaptopHdd => Services.Loc.Get("Ui_StorageType_LaptopHdd"),
        StorageType.LaptopSsd => Services.Loc.Get("Ui_StorageType_LaptopSsd"),
        StorageType.Virtual => Services.Loc.Get("Ui_StorageType_Virtual"),
        _ => Services.Loc.Get("Ui_StorageType_Virtual"),
    };
}

public sealed class StorageTypeOption
{
    public StorageType Type { get; }
    private readonly string _labelKey;

    public StorageTypeOption(StorageType type, string labelKey)
    {
        Type = type;
        _labelKey = labelKey;
    }

    public string Label => Services.Loc.Get(_labelKey);

    public override string ToString() => Label;
}

public partial class StorageViewModel : ObservableObject, IRefreshable
{
    private readonly IStorageRepository _storageRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IHistoryRepository _historyRepository;
    private readonly IDriveLinkRepository _driveLinks;
    private readonly DriveService _driveService;

    [ObservableProperty]
    private StorageListItem? _selectedItem;

    [ObservableProperty]
    private string? _selectedDriveLetter;

    [ObservableProperty]
    private string _driveStatusText = "";

    [ObservableProperty]
    private string _editNama = "";

    [ObservableProperty]
    private StorageTypeOption _editTipe = new(StorageType.HddEksternal, "Ui_StorageType_HddEksternalLong");

    [ObservableProperty]
    private string _editKapasitas = "";

    [ObservableProperty]
    private string _editBatas = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _hasSelection;

    public StorageViewModel(
        IStorageRepository storageRepository,
        IGameRepository gameRepository,
        IHistoryRepository historyRepository,
        IDriveLinkRepository driveLinks,
        DriveService driveService)
    {
        _storageRepository = storageRepository;
        _gameRepository = gameRepository;
        _historyRepository = historyRepository;
        _driveLinks = driveLinks;
        _driveService = driveService;
        Storages = new ObservableCollection<StorageListItem>();
        Games = new ObservableCollection<GameRow>();
        SelectedGames = new ObservableCollection<GameRow>();
        SelectedGames.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasGameSelection));
        Riwayat = new ObservableCollection<string>();
        TipeOptions = new[]
        {
            new StorageTypeOption(StorageType.HddEksternal, "Ui_StorageType_HddEksternalLong"),
            new StorageTypeOption(StorageType.LaptopHdd, "Ui_StorageType_LaptopHddLong"),
            new StorageTypeOption(StorageType.LaptopSsd, "Ui_StorageType_LaptopSsdLong"),
            new StorageTypeOption(StorageType.Virtual, "Ui_StorageType_VirtualLong"),
        };
    }

    public ObservableCollection<StorageListItem> Storages { get; }
    public ObservableCollection<GameRow> Games { get; }
    public ObservableCollection<GameRow> SelectedGames { get; }
    public ObservableCollection<string> Riwayat { get; }
    public ObservableCollection<DriveOption> DriveOptions { get; } = new();
    public IReadOnlyList<StorageTypeOption> TipeOptions { get; }
    public bool HasGameSelection => SelectedGames.Count > 0;
    public sealed record DriveOption(string Letter, string DisplayText)
    {
        public override string ToString() => DisplayText;
    }

    partial void OnSelectedItemChanged(StorageListItem? value)
    {
        HasSelection = value is not null;
        if (value is null)
        {
            EditNama = "";
            EditKapasitas = "";
            EditBatas = "";
            DriveOptions.Clear();
            SelectedDriveLetter = null;
            DriveStatusText = "";
            _ = ReloadGamesAsync(null);
            return;
        }

        EditNama = value.Storage.Nama;
        EditTipe = TipeOptions.FirstOrDefault(o => o.Type == value.Storage.Tipe) ?? TipeOptions[0];
        EditKapasitas = value.Storage.KapasitasGb?.ToString("0.#", GimDeks.App.Services.AppCulture.Current) ?? "";
        EditBatas = value.Storage.BatasGb?.ToString("0.#", GimDeks.App.Services.AppCulture.Current) ?? "";
        _ = LoadDriveOptionsAsync(value.Storage.Id);
        _ = ReloadGamesAsync(value.Storage.Id);
        _ = ReloadRiwayatAsync(value.Storage.Id);
    }

    public Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ReloadAsync()
    {
        var storages = await _storageRepository.GetAllAsync();
        var games = await _gameRepository.GetAllAsync();
        var counted = games.Where(g => CapacityCalculator.CountsTowardTotal(g.Status)).ToList();

        var previousId = SelectedItem?.Storage.Id;
        Storages.Clear();
        foreach (var storage in storages)
        {
            var storageGames = counted.Where(g => g.StorageId == storage.Id).ToList();
            Storages.Add(new StorageListItem
            {
                Storage = storage,
                GameCount = storageGames.Count,
                TotalGb = storageGames.Sum(g => g.UkuranGb),
            });
        }

        SelectedItem = Storages.FirstOrDefault(s => s.Storage.Id == previousId)
            ?? Storages.FirstOrDefault();
    }

    private async Task LoadDriveOptionsAsync(long storageId)
    {
        _loadingDrives = true;
        try
        {
            DriveOptions.Clear();
            DriveOptions.Add(new DriveOption("", Services.Loc.Get("Ui_Storage_NotConnected")));
            foreach (var drive in _driveService.GetReadyDrives())
            {
                DriveOptions.Add(new DriveOption(drive.Letter, $"{drive.Letter} ({drive.Label}) - {drive.FreeText}"));
            }

            var links = await _driveLinks.GetAllAsync();
            SelectedDriveLetter = links.TryGetValue(storageId, out var letter) ? letter : "";
            UpdateDriveStatus(letter: SelectedDriveLetter);
        }
        finally
        {
            _loadingDrives = false;
        }
    }

    private bool _loadingDrives;

    private void UpdateDriveStatus(string? letter)
    {
        if (string.IsNullOrEmpty(letter))
        {
            DriveStatusText = Services.Loc.Get("Ui_Storage_NotConnected");
            return;
        }

        var drive = _driveService.FindDrive(letter);
        DriveStatusText = drive is null ? Services.Loc.Format("Ui_Storage_DriveUnavailable", letter) : $"{drive.Letter} ({drive.Label}) - {drive.FreeText}";
    }

    partial void OnSelectedDriveLetterChanged(string? value)
    {
        if (SelectedItem is null || _loadingDrives)
        {
            return;
        }

        var letter = string.IsNullOrEmpty(value) ? null : value;
        _ = _driveLinks.SetAsync(SelectedItem.Storage.Id, letter);
        UpdateDriveStatus(letter);
    }

    private async Task ReloadGamesAsync(long? storageId)
    {
        var games = await _gameRepository.GetByStorageAsync(storageId);
        var storageName = SelectedItem?.Storage.Nama ?? "";
        Games.Clear();
        foreach (var game in games.OrderBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase))
        {
            Games.Add(new GameRow { Game = game, StorageName = storageName });
        }
    }

    private async Task ReloadRiwayatAsync(long storageId)
    {
        var entries = await _historyRepository.GetByStorageAsync(storageId);
        var gameNames = (await _gameRepository.GetAllAsync()).ToDictionary(g => g.Id, g => g.Nama);
        var storageNames = (await _storageRepository.GetAllAsync()).ToDictionary(s => s.Id, s => s.Nama);
        string NameOf(long? id) =>
            id is { } i && storageNames.TryGetValue(i, out var name) ? name : Services.Loc.Get("Ui_NoStorage");

        Riwayat.Clear();
        foreach (var entry in entries.Take(20))
        {
            var game = gameNames.TryGetValue(entry.GameId, out var n) ? n : $"#{entry.GameId}";
            Riwayat.Add($"{entry.Waktu.ToLocalTime():dd MMM yyyy HH:mm} | {game}: {NameOf(entry.DariStorageId)} -> {NameOf(entry.KeStorageId)}");
        }

        if (Riwayat.Count == 0)
        {
            Riwayat.Add(Services.Loc.Get("Ui_History_Empty"));
        }
    }

    /// <summary>Select a storage by id after the list reloads (deep link from health checks).</summary>
    public async Task SelectStorageByIdAsync(long id)
    {
        await ReloadAsync();
        SelectedItem = Storages.FirstOrDefault(s => s.Storage.Id == id);
    }

    [RelayCommand]
    private async Task AddStorageAsync()
    {
        var storages = await _storageRepository.GetAllAsync();
        var order = storages.Count == 0 ? 0 : storages.Max(s => s.Urutan) + 1;
        var nama = Services.Loc.Get("Ui_Storage_NewName");
        var counter = 2;
        while (await _storageRepository.ExistsByNameAsync(nama))
        {
            nama = Services.Loc.Format("Ui_Storage_NewNameCount", counter++);
        }

        var id = await _storageRepository.AddAsync(new Storage { Nama = nama, Tipe = StorageType.Virtual, KapasitasGb = null, BatasGb = null, Urutan = order });
        await ReloadAsync();
        SelectedItem = Storages.FirstOrDefault(s => s.Storage.Id == id);
        StatusText = Services.Loc.Get("Ui_Storage_Created");
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var nama = EditNama.Trim();
        if (string.IsNullOrWhiteSpace(nama))
        {
            DialogService.Error("Storage", Services.Loc.Get("Ui_Storage_NameRequired"));
            return;
        }

        if (await _storageRepository.ExistsByNameAsync(nama, SelectedItem.Storage.Id))
        {
            DialogService.Error("Storage", Services.Loc.Format("Ui_Storage_NameTaken", nama));
            return;
        }

        var kapasitas = ParseNullableGb(EditKapasitas);
        var batas = ParseNullableGb(EditBatas);
        if (!kapasitas.Ok)
        {
            DialogService.Error("Storage", Services.Loc.Get("Ui_Storage_CapacityInvalid"));
            return;
        }

        if (!batas.Ok)
        {
            DialogService.Error("Storage", Services.Loc.Get("Ui_Storage_LimitInvalid"));
            return;
        }

        var updated = SelectedItem.Storage;
        updated.Nama = nama;
        updated.Tipe = EditTipe.Type;
        updated.KapasitasGb = kapasitas.Value;
        updated.BatasGb = batas.Value;
        await _storageRepository.UpdateAsync(updated);
        StatusText = Services.Loc.Format("Ui_Storage_Saved", nama);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteStorageAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var count = await _gameRepository.CountByStorageAsync(SelectedItem.Storage.Id);
        if (count > 0)
        {
            DialogService.Error("Storage", Services.Loc.Format("Ui_Storage_InUse", count));
            return;
        }

        if (!DialogService.Confirm(Services.Loc.Get("Ui_Storage_DeleteTitle"), Services.Loc.Format("Ui_Storage_DeleteConfirm", SelectedItem.Storage.Nama)))
        {
            return;
        }

        await _storageRepository.DeleteAsync(SelectedItem.Storage.Id);
        SelectedItem = null;
        StatusText = Services.Loc.Get("Ui_Storage_Deleted");
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task MoveSelectedGamesAsync()
    {
        if (SelectedGames.Count == 0)
        {
            return;
        }

        var storages = await _storageRepository.GetAllAsync();
        var target = Views.StoragePickerDialog.Show(storages, Services.Loc.Get("Ui_Move_To"));
        if (target is null)
        {
            return;
        }

        var ids = SelectedGames.Select(g => g.Game.Id).ToList();
        await _gameRepository.MoveToStorageAsync(ids, target.Id);
        StatusText = Services.Loc.Format("Ui_Storage_Moved", ids.Count, target.Nama);
        await ReloadAsync();
    }

    private static (bool Ok, double? Value) ParseNullableGb(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (true, null);
        }

        return NumberParser.ParseGb(text) is { } value && value >= 0
            ? (true, value)
            : (false, null);
    }
}
