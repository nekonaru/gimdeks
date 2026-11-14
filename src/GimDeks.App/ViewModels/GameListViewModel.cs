using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public sealed class GameRow
{
    private static readonly CultureInfo IdId = CultureInfo.GetCultureInfo("id-ID");

    public required Game Game { get; init; }
    public required string StorageName { get; init; }
    public string UkuranText => $"{Game.UkuranGb.ToString("N1", IdId)} GB";
    public string StatusText => Game.Status switch
    {
        GameStatus.BelumDownload => "Belum Download",
        GameStatus.SudahAda => "Sudah Ada",
        GameStatus.Dikecualikan => "Dikecualikan",
        GameStatus.Dieliminasi => "Dieliminasi",
        _ => Game.Status.ToString(),
    };
}

public sealed record FilterOption(string Value, string Label)
{
    public static readonly FilterOption All = new("", "Semua");
}

public partial class GameListViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _gameRepository;
    private readonly IStorageRepository _storageRepository;
    private List<Game> _allGames = new();
    private List<Storage> _allStorages = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private FilterOption _selectedStatus = FilterOption.All;

    [ObservableProperty]
    private FilterOption _selectedStorage = FilterOption.All;

    [ObservableProperty]
    private string _minSize = "";

    [ObservableProperty]
    private string _maxSize = "";

    [ObservableProperty]
    private string _summaryText = "";

    public GameListViewModel(IGameRepository gameRepository, IStorageRepository storageRepository)
    {
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        StatusOptions = new[]
        {
            FilterOption.All,
            new FilterOption(GameStatus.BelumDownload.ToDatabase(), "Belum Download"),
            new FilterOption(GameStatus.SudahAda.ToDatabase(), "Sudah Ada"),
            new FilterOption(GameStatus.Dikecualikan.ToDatabase(), "Dikecualikan"),
            new FilterOption(GameStatus.Dieliminasi.ToDatabase(), "Dieliminasi"),
        };
        StorageOptions = new ObservableCollection<FilterOption> { FilterOption.All };
        Rows = new ObservableCollection<GameRow>();
        SelectedRows = new ObservableCollection<GameRow>();
        SelectedRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSelection));
    }

    public ObservableCollection<GameRow> Rows { get; }
    public ObservableCollection<GameRow> SelectedRows { get; }
    public IReadOnlyList<FilterOption> StatusOptions { get; }
    public ObservableCollection<FilterOption> StorageOptions { get; }
    public bool HasSelection => SelectedRows.Count > 0;

    public void SetStorageFilter(long? storageId)
    {
        SelectedStorage = storageId is null
            ? FilterOption.All
            : StorageOptions.FirstOrDefault(o => o.Value == storageId.Value.ToString(CultureInfo.InvariantCulture)) ?? FilterOption.All;
    }

    public Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ReloadAsync()
    {
        _allGames = (await _gameRepository.GetAllAsync()).ToList();
        _allStorages = (await _storageRepository.GetAllAsync()).ToList();

        var current = SelectedStorage;
        StorageOptions.Clear();
        StorageOptions.Add(FilterOption.All);
        foreach (var storage in _allStorages)
        {
            StorageOptions.Add(new FilterOption(storage.Id.ToString(CultureInfo.InvariantCulture), storage.Nama));
        }

        StorageOptions.Add(new FilterOption("null", "Tanpa Storage"));
        SelectedStorage = StorageOptions.FirstOrDefault(o => o.Value == current.Value) ?? FilterOption.All;

        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusChanged(FilterOption value) => ApplyFilters();
    partial void OnSelectedStorageChanged(FilterOption value) => ApplyFilters();
    partial void OnMinSizeChanged(string value) => ApplyFilters();
    partial void OnMaxSizeChanged(string value) => ApplyFilters();

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        SelectedStatus = FilterOption.All;
        SelectedStorage = FilterOption.All;
        MinSize = "";
        MaxSize = "";
    }

    private void ApplyFilters()
    {
        var search = SearchText.Trim();
        var min = NumberParser.ParseGb(MinSize);
        var max = NumberParser.ParseGb(MaxSize);
        var storageById = _allStorages.ToDictionary(s => s.Id, s => s.Nama);

        IEnumerable<Game> filtered = _allGames;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(g =>
                g.Nama.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (g.Versi?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (SelectedStatus != FilterOption.All)
        {
            var dbValue = SelectedStatus.Value;
            filtered = filtered.Where(g => string.Equals(g.Status.ToDatabase(), dbValue, StringComparison.Ordinal));
        }

        if (SelectedStorage != FilterOption.All)
        {
            if (SelectedStorage.Value == "null")
            {
                filtered = filtered.Where(g => g.StorageId is null);
            }
            else if (long.TryParse(SelectedStorage.Value, CultureInfo.InvariantCulture, out var storageId))
            {
                filtered = filtered.Where(g => g.StorageId == storageId);
            }
        }

        if (min.HasValue)
        {
            filtered = filtered.Where(g => g.UkuranGb >= min.Value);
        }

        if (max.HasValue)
        {
            filtered = filtered.Where(g => g.UkuranGb <= max.Value);
        }

        var rows = filtered
            .OrderBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(g => g.Urutan)
            .Select(g => new GameRow
            {
                Game = g,
                StorageName = g.StorageId is { } id && storageById.TryGetValue(id, out var name) ? name : "Tanpa Storage",
            })
            .ToList();

        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        SummaryText = $"{rows.Count} game ditampilkan";
    }

    [RelayCommand]
    private async Task AddGameAsync()
    {
        var dialog = new Views.GameEditDialog(_allStorages, null);
        if (dialog.ShowDialog() != true || dialog.Result is not { } game)
        {
            return;
        }

        await _gameRepository.AddAsync(game);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task EditGameAsync()
    {
        if (SelectedRows.Count != 1)
        {
            return;
        }

        var original = SelectedRows[0].Game;
        var dialog = new Views.GameEditDialog(_allStorages, original);
        if (dialog.ShowDialog() != true || dialog.Result is not { } game)
        {
            return;
        }

        game.Id = original.Id;
        game.DibuatPada = original.DibuatPada;
        await _gameRepository.UpdateAsync(game);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteGamesAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        var message = SelectedRows.Count == 1
            ? $"Hapus \"{SelectedRows[0].Game.Nama}\"?"
            : $"Hapus {SelectedRows.Count} game yang dipilih?";
        if (!DialogService.Confirm("Hapus Game", message))
        {
            return;
        }

        foreach (var row in SelectedRows.ToList())
        {
            await _gameRepository.DeleteAsync(row.Game.Id);
        }

        await ReloadAsync();
    }

    [RelayCommand]
    private async Task MoveToStorageAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        var storages = await _storageRepository.GetAllAsync();
        var target = Views.StoragePickerDialog.Show(storages, "Pindahkan ke Storage");
        if (target is null)
        {
            return;
        }

        var ids = SelectedRows.Select(r => r.Game.Id).ToList();
        await _gameRepository.MoveToStorageAsync(ids, target.Id);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ChangeStatusAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        var current = SelectedRows[0].Game.Status;
        var result = Views.StatusChangeDialog.Show(current);
        if (result is not { } changed)
        {
            return;
        }

        foreach (var row in SelectedRows.ToList())
        {
            var game = row.Game;
            game.Status = changed.Status;
            game.Alasan = changed.Alasan;
            if (changed.Status is GameStatus.Dikecualikan or GameStatus.Dieliminasi)
            {
                // Excluded statuses belong to no storage by definition.
                game.StorageId = null;
            }

            await _gameRepository.UpdateAsync(game);
        }

        await ReloadAsync();
    }
}

