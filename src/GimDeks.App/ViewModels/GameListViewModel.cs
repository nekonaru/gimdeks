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
    private static CultureInfo IdId => Services.AppCulture.Current;

    public required Game Game { get; init; }
    public required string StorageName { get; init; }
    public string TagsText { get; init; } = "";
    public string UkuranText => $"{Game.UkuranGb.ToString("N1", IdId)} GB";
    public string StatusText => Services.Loc.Get(Game.Status switch
    {
        GameStatus.BelumDownload => "Ui_Status_BelumDownload",
        GameStatus.SudahAda => "Ui_Status_SudahAda",
        GameStatus.Dikecualikan => "Ui_Status_Dikecualikan",
        GameStatus.Dieliminasi => "Ui_Status_Dieliminasi",
        _ => "Ui_Status_BelumDownload",
    });
}

public sealed record FilterOption(string Value, string LabelKey)
{
    public string Label => Services.Loc.Get(LabelKey);

    public static readonly FilterOption All = new("", "Ui_All");
}

public partial class GameListViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _gameRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly ITagRepository _tagRepository;
    private readonly SemaphoreSlim _filtersLock = new(1, 1);
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
    private FilterOption _selectedTag = FilterOption.All;

    [ObservableProperty]
    private FilterOption _selectedSelesai = FilterOption.All;

    [ObservableProperty]
    private string _summaryText = "";

    [ObservableProperty]
    private string _statusText = "";

    public GameListViewModel(IGameRepository gameRepository, IStorageRepository storageRepository, ITagRepository tagRepository)
    {
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        _tagRepository = tagRepository;
        StatusOptions = new[]
        {
            FilterOption.All,
            new FilterOption(GameStatus.BelumDownload.ToDatabase(), "Ui_Status_BelumDownload"),
            new FilterOption(GameStatus.SudahAda.ToDatabase(), "Ui_Status_SudahAda"),
            new FilterOption(GameStatus.Dikecualikan.ToDatabase(), "Ui_Status_Dikecualikan"),
            new FilterOption(GameStatus.Dieliminasi.ToDatabase(), "Ui_Status_Dieliminasi"),
        };
        SelesaiOptions = new[]
        {
            FilterOption.All,
            new FilterOption("ya", "Ui_SelesaiYes"),
            new FilterOption("tidak", "Ui_SelesaiNo"),
        };
        StorageOptions = new ObservableCollection<FilterOption> { FilterOption.All };
        TagOptions = new ObservableCollection<FilterOption> { FilterOption.All };
        Rows = new ObservableCollection<GameRow>();
        SelectedRows = new ObservableCollection<GameRow>();
        SelectedRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSelection));
    }

    public ObservableCollection<GameRow> Rows { get; }
    public ObservableCollection<GameRow> SelectedRows { get; }
    public IReadOnlyList<FilterOption> StatusOptions { get; }
    public IReadOnlyList<FilterOption> SelesaiOptions { get; }
    public ObservableCollection<FilterOption> StorageOptions { get; }
    public ObservableCollection<FilterOption> TagOptions { get; }
    public bool HasSelection => SelectedRows.Count > 0;

    public IReadOnlyList<GameRow> SelectedRowsList => SelectedRows;

    /// <summary>Raised at the end of a rows rebuild; the page re-selects matching rows in the grid.</summary>
    public event Action? RowsReloaded;

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
        // Selection is restored by ApplyFiltersAsync at the end of the rebuild.
        var current = SelectedStorage;
        var currentTag = SelectedTag;
        StorageOptions.Clear();
        _allGames = (await _gameRepository.GetAllAsync()).ToList();
        _allStorages = (await _storageRepository.GetAllAsync()).ToList();

        StorageOptions.Add(FilterOption.All);
        foreach (var storage in _allStorages)
        {
            StorageOptions.Add(new FilterOption(storage.Id.ToString(CultureInfo.InvariantCulture), storage.Nama));
        }

        StorageOptions.Add(new FilterOption("null", "Ui_NoStorage"));
        SelectedStorage = StorageOptions.FirstOrDefault(o => o.Value == current.Value) ?? FilterOption.All;

        var allTags = await _tagRepository.GetAllAsync();
        TagOptions.Clear();
        TagOptions.Add(FilterOption.All);
        foreach (var tag in allTags)
        {
            TagOptions.Add(new FilterOption(tag.Id.ToString(CultureInfo.InvariantCulture), tag.Nama));
        }

        SelectedTag = TagOptions.FirstOrDefault(o => o.Value == currentTag.Value) ?? FilterOption.All;
        await ApplyFiltersAsync();
    }

    private async Task<Dictionary<long, string>> MapTagsByGameAsync(IReadOnlyList<long> gameIds)
    {
        var tags = await _tagRepository.GetTagsForGamesAsync(gameIds);
        return tags.ToDictionary(kv => kv.Key, kv => string.Join(", ", kv.Value.Select(t => t.Nama)));
    }

    partial void OnSearchTextChanged(string value) => _ = ApplyFiltersAsync();
    partial void OnSelectedStatusChanged(FilterOption value) => _ = ApplyFiltersAsync();
    partial void OnSelectedStorageChanged(FilterOption value) => _ = ApplyFiltersAsync();
    partial void OnSelectedTagChanged(FilterOption value) => _ = ApplyFiltersAsync();
    partial void OnSelectedSelesaiChanged(FilterOption value) => _ = ApplyFiltersAsync();
    partial void OnMinSizeChanged(string value) => _ = ApplyFiltersAsync();
    partial void OnMaxSizeChanged(string value) => _ = ApplyFiltersAsync();

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        SelectedStatus = FilterOption.All;
        SelectedStorage = FilterOption.All;
        SelectedTag = FilterOption.All;
        SelectedSelesai = FilterOption.All;
        MinSize = "";
        MaxSize = "";
    }

    private async Task ApplyFiltersAsync()
    {
        await _filtersLock.WaitAsync().ConfigureAwait(true);
        try
        {
            await ApplyFiltersCoreAsync();
        }
        finally
        {
            _filtersLock.Release();
        }
    }

    private async Task ApplyFiltersCoreAsync()
    {
        var search = SearchText.Trim();
        var min = NumberParser.ParseGb(MinSize);
        var max = NumberParser.ParseGb(MaxSize);
        var storageById = _allStorages.ToDictionary(s => s.Id, s => s.Nama);
        var tagsByGame = await MapTagsByGameAsync(_allGames.Select(g => g.Id).ToList());

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

        if (SelectedTag != FilterOption.All && long.TryParse(SelectedTag.Value, CultureInfo.InvariantCulture, out var tagId))
        {
            var memberIds = (await _tagRepository.GetGameIdsWithTagAsync(tagId)).ToHashSet();
            filtered = filtered.Where(g => memberIds.Contains(g.Id));
        }

        if (SelectedSelesai == FilterOption.All)
        {
            // no selesai filter
        }
        else if (SelectedSelesai.Value == "ya")
        {
            filtered = filtered.Where(g => g.SelesaiMain);
        }
        else if (SelectedSelesai.Value == "tidak")
        {
            filtered = filtered.Where(g => !g.SelesaiMain);
        }

        var rows = filtered
            .OrderBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(g => g.Urutan)
            .Select(g => new GameRow
            {
                Game = g,
                StorageName = g.StorageId is { } id && storageById.TryGetValue(id, out var name) ? name : Services.Loc.Get("Ui_NoStorage"),
                TagsText = tagsByGame.TryGetValue(g.Id, out var tagText) ? tagText : "",
            })
            .ToList();

        var preservedSelectionIds = SelectedRows.Select(r => r.Game.Id).ToHashSet();
        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        SelectedRows.Clear();
        foreach (var row in rows)
        {
            if (preservedSelectionIds.Contains(row.Game.Id))
            {
                SelectedRows.Add(row);
            }
        }

        var selesaiCount = filtered.Count(g => g.SelesaiMain);
        SummaryText = Services.Loc.Format("Ui_GameList_Summary", rows.Count, selesaiCount);
        RowsReloaded?.Invoke();
    }

    [RelayCommand]
    private async Task DownloadCoversAsync()
    {
        var rows = SelectedRows.ToList();
        if (rows.Count == 0)
        {
            StatusText = Services.Loc.Get("Ui_Button_SelectDownloadCoverHint");
            return;
        }

        if (!DialogService.Confirm(
                Services.Loc.Get("Ui_Cover_Title"),
                Services.Loc.Format("Ui_Cover_FetchConfirm", rows.Count)))
        {
            return;
        }

        var service = new Services.SteamCoverService();
        var ok = 0;
        foreach (var (row, index) in rows.Select((r, i) => (r, i)))
        {
            StatusText = Services.Loc.Format("Ui_Cover_Progress", index + 1, rows.Count) + " " + row.Game.Nama;
            try
            {
                var result = await service.FetchCoverAsync(row.Game.Nama, row.Game.Id);
                if (result is not null)
                {
                    ok++;
                }
            }
            catch
            {
                // Continue with the next one.
            }
        }

        StatusText = Services.Loc.Format("Ui_Cover_Progress", ok, rows.Count);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task AddGameAsync()
    {
        var dialog = new Views.GameEditDialog(_allStorages, null, null);
        if (dialog.ShowDialog() != true || dialog.Result is not { } game)
        {
            return;
        }

        var id = await _gameRepository.AddAsync(game);
        await SaveTagsAsync(id, dialog.ResultTagText);
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
        var existingTags = string.Join(", ",
            (await _tagRepository.GetTagsForGameAsync(original.Id)).Select(t => t.Nama));
        var dialog = new Views.GameEditDialog(_allStorages, original, existingTags);
        if (dialog.ShowDialog() != true || dialog.Result is not { } game)
        {
            return;
        }

        game.Id = original.Id;
        game.DibuatPada = original.DibuatPada;
        await _gameRepository.UpdateAsync(game);
        await SaveTagsAsync(game.Id, dialog.ResultTagText);
        await ReloadAsync();
    }

    private async Task SaveTagsAsync(long gameId, string tagText)
    {
        var names = tagText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var tagIds = new List<long>();
        foreach (var name in names)
        {
            tagIds.Add(await _tagRepository.EnsureAsync(name));
        }

        await _tagRepository.SetTagsForGameAsync(gameId, tagIds);
    }

    [RelayCommand]
    private async Task DeleteGamesAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        var message = SelectedRows.Count == 1
            ? Services.Loc.Format("Ui_GameList_DeleteConfirmOne", SelectedRows[0].Game.Nama)
            : Services.Loc.Format("Ui_GameList_DeleteConfirmMany", SelectedRows.Count);
        if (!DialogService.Confirm(Services.Loc.Get("Ui_GameList_DeleteTitle"), message))
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
        var target = Views.StoragePickerDialog.Show(storages, Services.Loc.Get("Ui_Move_To"));
        if (target is null)
        {
            return;
        }

        var ids = SelectedRows.Select(r => r.Game.Id).ToList();
        await _gameRepository.MoveToStorageAsync(ids, target.Id);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task SplitVersionsAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        var changed = 0;
        foreach (var row in SelectedRows.ToList())
        {
            var (name, versi) = GameNameVersionSplitter.Split(row.Game.Nama);
            if (versi is null || (name == row.Game.Nama && row.Game.Versi == versi))
            {
                continue;
            }

            var game = row.Game;
            game.Nama = name;
            game.Versi = versi;
            await _gameRepository.UpdateAsync(game);
            changed++;
        }

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

    [RelayCommand]
    private async Task ToggleSelesaiAsync()
    {
        var rows = SelectedRows.ToList();
        if (rows.Count == 0)
        {
            return;
        }

        // If every selected row is already played through, the action un-marks them all;
        // otherwise it marks them all.
        var set = !rows.All(row => row.Game.SelesaiMain);
        foreach (var row in rows)
        {
            row.Game.SelesaiMain = set;
            await _gameRepository.UpdateAsync(row.Game);
        }

        StatusText = Services.Loc.Format(set ? "Ui_GameList_SelesaiDone" : "Ui_GameList_SelesaiUndone", rows.Count);
        await ReloadAsync();
    }
}

