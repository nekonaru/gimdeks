using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

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
    public string PlayText => Game.JumlahMain == 0
        ? Services.Loc.Get("Ui_Col_NeverPlayed")
        : Services.Loc.Format("Ui_Col_PlayedTimes", Game.JumlahMain);
    public string RatingText => Game.Rating <= 0 ? "" : new string('*', Game.Rating);
    public string TimeText
    {
        get
        {
            var seconds = Game.TotalDetik;
            if (seconds <= 0)
            {
                return "-";
            }
            var hours = seconds / 3600;
            var minutes = (seconds % 3600) / 60;
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }
    }
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
    private readonly Services.SessionTrackerService _sessionTracker;
    private readonly IFilterPresetRepository _presets;
    private readonly IGameNoteRepository _notes;
    private Dictionary<long, string> _notesByGame = new();
    private string _noteQuery = "";
    private bool _regexSearch;
    private readonly SemaphoreSlim _filtersLock = new(1, 1);
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private readonly SemaphoreSlim _launchLock = new(1, 1);
    private bool _suppressFilterChanges;
    private List<Game> _allGames = new();
    private List<Storage> _allStorages = new();
    private Dictionary<long, string> _tagsByGame = new();

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
    private FilterOption _selectedSort = FilterOption.All;

    [ObservableProperty]
    private string _summaryText = "";

    [ObservableProperty]
    private FilterPreset? _selectedPreset;

    partial void OnSelectedPresetChanged(FilterPreset? value) => _ = ApplyPresetAsync(value);

    [ObservableProperty]
    private string _statusText = "";

    public GameListViewModel(IGameRepository gameRepository, IStorageRepository storageRepository, ITagRepository tagRepository, Services.SessionTrackerService sessionTracker, IFilterPresetRepository presets, IGameNoteRepository notes)
    {
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        _tagRepository = tagRepository;
        _sessionTracker = sessionTracker;
        _presets = presets;
        _notes = notes;
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
        SortOptions = new[]
        {
            new FilterOption("name", "Ui_Sort_Name"),
            new FilterOption("size", "Ui_Sort_SizeDesc"),
            new FilterOption("played", "Ui_Sort_PlayedDesc"),
            new FilterOption("recent", "Ui_Sort_RecentDesc"),
            new FilterOption("status", "Ui_Sort_Status"),
            new FilterOption("favorite", "Ui_Sort_Favorite"),
            new FilterOption("rating", "Ui_Sort_Rating"),
            new FilterOption("priority", "Ui_Sort_Priority"),
            new FilterOption("time", "Ui_Sort_Time"),
        };
        _selectedSort = SortOptions[0];
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
    public IReadOnlyList<FilterOption> SortOptions { get; }
    public ObservableCollection<FilterOption> StorageOptions { get; }
    public ObservableCollection<FilterOption> TagOptions { get; }
    public bool HasSelection => SelectedRows.Count > 0;

    public IReadOnlyList<GameRow> SelectedRowsList => SelectedRows;

    /// <summary>Raised at the end of a rows rebuild; the page re-selects matching rows in the grid.</summary>
    public event Action? RowsReloaded;

    public long? PendingSelectedGameId { get; private set; }
    public long? PendingStorageFilterId { get; private set; }
    public long? PendingGameId { get; private set; }

    public void SetStorageFilter(long? storageId) => PendingStorageFilterId = storageId;

    public void SetPendingGame(long gameId) => PendingGameId = gameId;

    /// <summary>Reset filters and select one game (used by the dashboard random pick).</summary>
    public async Task OpenGameByIdAsync(long gameId)
    {
        PendingGameId = gameId;
        await ReloadAsync();
    }

    public Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ReloadAsync()
    {
        await _reloadLock.WaitAsync().ConfigureAwait(true);
        try
        {
            await ReloadCoreAsync();
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private async Task ReloadCoreAsync()
    {
        // Selection is restored by ApplyFiltersAsync at the end of the rebuild.
        var current = SelectedStorage;
        var currentTag = SelectedTag;
        StorageOptions.Clear();
        _allGames = (await _gameRepository.GetAllAsync()).ToList();
        _allStorages = (await _storageRepository.GetAllAsync()).ToList();
        _tagsByGame = await MapTagsByGameAsync(_allGames.Select(g => g.Id).ToList());
        _notesByGame = await _notes.GetAllTextByGameAsync();

        StorageOptions.Add(FilterOption.All);
        foreach (var storage in _allStorages)
        {
            StorageOptions.Add(new FilterOption(storage.Id.ToString(CultureInfo.InvariantCulture), storage.Nama));
        }

        StorageOptions.Add(new FilterOption("null", "Ui_NoStorage"));

        var allTags = await _tagRepository.GetAllAsync();
        TagOptions.Clear();
        TagOptions.Add(FilterOption.All);
        foreach (var tag in allTags)
        {
            TagOptions.Add(new FilterOption(tag.Id.ToString(CultureInfo.InvariantCulture), tag.Nama));
        }

        await LoadPresetsAsync();

        long? gameToSelect = null;
        _suppressFilterChanges = true;
        try
        {
            if (PendingGameId is { } gameId)
            {
                gameToSelect = gameId;
                PendingGameId = null;
                PendingStorageFilterId = null;
                ClearFilterValues();
            }
            else if (PendingStorageFilterId is { } storageId)
            {
                PendingStorageFilterId = null;
                SelectedStorage = StorageOptions.FirstOrDefault(o => o.Value == storageId.ToString(CultureInfo.InvariantCulture)) ?? FilterOption.All;
                SelectedTag = TagOptions.FirstOrDefault(o => o.Value == currentTag.Value) ?? FilterOption.All;
            }
            else
            {
                SelectedStorage = StorageOptions.FirstOrDefault(o => o.Value == current.Value) ?? FilterOption.All;
                SelectedTag = TagOptions.FirstOrDefault(o => o.Value == currentTag.Value) ?? FilterOption.All;
            }
        }
        finally
        {
            _suppressFilterChanges = false;
        }

        await ApplyFiltersAsync();
        if (gameToSelect is { } target)
        {
            SelectGameById(target);
        }
    }

    private void ClearFilterValues()
    {
        SearchText = "";
        SelectedStatus = FilterOption.All;
        SelectedStorage = FilterOption.All;
        SelectedTag = FilterOption.All;
        SelectedSelesai = FilterOption.All;
        MinSize = "";
        MaxSize = "";
        _noteQuery = "";
        _regexSearch = false;
    }

    private void SelectGameById(long gameId)
    {
        PendingSelectedGameId = gameId;
        var row = Rows.FirstOrDefault(r => r.Game.Id == gameId);
        if (row is null)
        {
            StatusText = Services.Loc.Get("Ui_GameList_PickNotFound");
            return;
        }

        SelectedRows.Clear();
        SelectedRows.Add(row);
        RowsReloaded?.Invoke();
        StatusText = Services.Loc.Format("Ui_GameList_Picked", row.Game.Nama);
    }

    private async Task<Dictionary<long, string>> MapTagsByGameAsync(IReadOnlyList<long> gameIds)
    {
        var tags = await _tagRepository.GetTagsForGamesAsync(gameIds);
        return tags.ToDictionary(kv => kv.Key, kv => string.Join(", ", kv.Value.Select(t => t.Nama)));
    }

    partial void OnSearchTextChanged(string value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnSelectedStatusChanged(FilterOption value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnSelectedStorageChanged(FilterOption value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnSelectedTagChanged(FilterOption value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnSelectedSelesaiChanged(FilterOption value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnMinSizeChanged(string value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnMaxSizeChanged(string value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }
    partial void OnSelectedSortChanged(FilterOption value) { if (!_suppressFilterChanges) { _ = ApplyFiltersAsync(); } }

    [RelayCommand]
    private async Task ClearFilters()
    {
        _suppressFilterChanges = true;
        try
        {
            ClearFilterValues();
        }
        finally
        {
            _suppressFilterChanges = false;
        }
        await ApplyFiltersAsync();
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

        IEnumerable<Game> filtered = _allGames;

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (_regexSearch)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(search,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    filtered = filtered.Where(g => regex.IsMatch(g.Nama) || (g.Versi is not null && regex.IsMatch(g.Versi)));
                }
                catch (ArgumentException)
                {
                    filtered = filtered.Where(g => false);
                }
            }
            else
            {
                filtered = filtered.Where(g =>
                    g.Nama.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (g.Versi?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }
        }

        if (_noteQuery.Length > 0)
        {
            filtered = filtered.Where(g =>
            {
                var text = (g.Catatan ?? "") + "\n" + (_notesByGame.TryGetValue(g.Id, out var note) ? note : "");
                return text.Contains(_noteQuery, StringComparison.OrdinalIgnoreCase);
            });
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

        var sort = SelectedSort?.Value ?? "name";
        IOrderedEnumerable<Game> ordered;
        if (sort == "size")
        {
            ordered = filtered.OrderByDescending(g => g.UkuranGb);
        }
        else if (sort == "played")
        {
            ordered = filtered.OrderByDescending(g => g.JumlahMain).ThenByDescending(g => g.DimainkanPada);
        }
        else if (sort == "recent")
        {
            ordered = filtered.OrderByDescending(g => g.DimainkanPada ?? DateTime.MinValue);
        }
        else if (sort == "status")
        {
            ordered = filtered.OrderBy(g => g.Status.ToString(), StringComparer.OrdinalIgnoreCase);
        }
        else if (sort == "favorite")
        {
            ordered = filtered.OrderByDescending(g => g.Favorit).ThenBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase);
        }
        else if (sort == "rating")
        {
            ordered = filtered.OrderByDescending(g => g.Rating).ThenByDescending(g => g.Favorit);
        }
        else if (sort == "priority")
        {
            ordered = filtered.OrderByDescending(g => g.Prioritas).ThenBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase);
        }
        else if (sort == "time")
        {
            ordered = filtered.OrderByDescending(g => g.TotalDetik);
        }
        else
        {
            ordered = filtered.OrderBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase);
        }

        var rows = ordered
            .ThenBy(g => g.Urutan)
            .Select(g => new GameRow
            {
                Game = g,
                StorageName = g.StorageId is { } id && storageById.TryGetValue(id, out var name) ? name : Services.Loc.Get("Ui_NoStorage"),
                TagsText = _tagsByGame.TryGetValue(g.Id, out var tagText) ? tagText : "",
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

        if (PendingSelectedGameId is { } pendingId)
        {
            var pendingRow = rows.FirstOrDefault(r => r.Game.Id == pendingId);
            if (pendingRow is not null)
            {
                SelectedRows.Clear();
                SelectedRows.Add(pendingRow);
                StatusText = Services.Loc.Format("Ui_GameList_Picked", pendingRow.Game.Nama);
            }
            PendingSelectedGameId = null;
        }

        var selesaiCount = filtered.Count(g => g.SelesaiMain);
        SummaryText = Services.Loc.Format("Ui_GameList_Summary", rows.Count, selesaiCount);
        RowsReloaded?.Invoke();
    }

    [RelayCommand]
    private async Task LaunchGameAsync()
    {
        if (!await _launchLock.WaitAsync(0).ConfigureAwait(true))
        {
            return; // A launch is already in progress.
        }
        try
        {
            await LaunchGameCoreAsync();
        }
        finally
        {
            _launchLock.Release();
        }
    }

    private async Task LaunchGameCoreAsync()
    {
        if (SelectedRows.Count != 1)
        {
            StatusText = Services.Loc.Get("Ui_GameList_SelectOneToLaunch");
            return;
        }

        var game = SelectedRows[0].Game;
        if (!Services.GameLauncherService.IsValidExecutable(game.PathExe))
        {
            StatusText = Services.Loc.Get("Ui_GameList_NoExe");
            return;
        }

        if (_sessionTracker.IsRunning(game.Id))
        {
            StatusText = Services.Loc.Get("Ui_GameList_AlreadyRunning");
            return;
        }

        if (!await _sessionTracker.LaunchAsync(game))
        {
            StatusText = Services.Loc.Get("Ui_GameList_LaunchFailed");
            return;
        }

        StatusText = Services.Loc.Format("Ui_GameList_Launched", game.Nama);
        await ReloadAsync();
    }

    public IReadOnlyList<FilterPreset> Presets { get; private set; } = Array.Empty<FilterPreset>();

    [RelayCommand]
    private async Task LoadPresetsAsync()
    {
        Presets = await _presets.GetAllAsync();
    }

    [RelayCommand]
    private async Task OpenAdvancedSearchAsync()
    {
        var dialog = new Views.AdvancedSearchDialog { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || dialog.Result is not { } criteria)
        {
            return;
        }

        _suppressFilterChanges = true;
        try
        {
            SearchText = criteria.Name;
            MinSize = criteria.MinGb;
            MaxSize = criteria.MaxGb;
            _noteQuery = criteria.Note;
            _regexSearch = criteria.Regex;
        }
        finally
        {
            _suppressFilterChanges = false;
        }
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task SavePresetAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = Services.Loc.Get("Ui_Advanced_PresetNameRequired");
            return;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            SearchText,
            SelectedStatus = SelectedStatus.Value,
            SelectedStorage = SelectedStorage.Value,
            SelectedTag = SelectedTag.Value,
            SelectedSelesai = SelectedSelesai.Value,
            MinSize,
            MaxSize,
            NoteQuery = _noteQuery,
            RegexSearch = _regexSearch,
        });
        await _presets.SaveAsync(name.Trim(), json);
        await LoadPresetsAsync();
        StatusText = Services.Loc.Format("Ui_Advanced_PresetSaved", name.Trim());
    }

    [RelayCommand]
    private async Task ApplyPresetAsync(FilterPreset? preset)
    {
        if (preset is null)
        {
            return;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(preset.DefinisiJson);
            var root = document.RootElement;
            _suppressFilterChanges = true;
            try
            {
                SearchText = root.GetProperty("SearchText").GetString() ?? "";
                MinSize = root.GetProperty("MinSize").GetString() ?? "";
                MaxSize = root.GetProperty("MaxSize").GetString() ?? "";
                _noteQuery = root.GetProperty("NoteQuery").GetString() ?? "";
                _regexSearch = root.GetProperty("RegexSearch").GetBoolean();
                SelectedStatus = StatusOptions.FirstOrDefault(o => o.Value == root.GetProperty("SelectedStatus").GetString()) ?? FilterOption.All;
                SelectedTag = TagOptions.FirstOrDefault(o => o.Value == root.GetProperty("SelectedTag").GetString()) ?? FilterOption.All;
                SelectedSelesai = SelesaiOptions.FirstOrDefault(o => o.Value == root.GetProperty("SelectedSelesai").GetString()) ?? FilterOption.All;
            }
            finally
            {
                _suppressFilterChanges = false;
            }
            await ApplyFiltersAsync();
        }
        catch (Exception exception)
        {
            Services.CrashLog.Write(exception);
            StatusText = Services.Loc.Get("Ui_Advanced_PresetFailed");
        }
    }

    public async Task SaveFavoritAsync(GameRow row)
    {
        await _gameRepository.UpdateAsync(row.Game);
    }

    [RelayCommand]
    private void OpenDetail()
    {
        if (SelectedRows.Count != 1)
        {
            StatusText = Services.Loc.Get("Ui_GameList_SelectOneToLaunch");
            return;
        }

        var app = App.Services;
        var vm = new GameDetailViewModel(
            SelectedRows[0].Game,
            app.GetRequiredService<IGameRepository>(),
            app.GetRequiredService<IGameNoteRepository>(),
            app.GetRequiredService<IGameImageRepository>(),
            app.GetRequiredService<IPlaySessionRepository>(),
            app.GetRequiredService<Services.SessionTrackerService>());
        new Views.GameDetailWindow(vm) { Owner = System.Windows.Application.Current.MainWindow }.Show();
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

        await DownloadCoversForRowsAsync(rows);
    }

    [RelayCommand]
    private async Task DownloadMissingCoversAsync()
    {
        var rows = Rows.Where(row => Services.SteamCoverService.FindCoverPath(row.Game.Id) is null).ToList();
        if (rows.Count == 0)
        {
            StatusText = Services.Loc.Get("Ui_Cover_AllHaveOne");
            return;
        }

        if (!DialogService.Confirm(
                Services.Loc.Get("Ui_Cover_Title"),
                Services.Loc.Format("Ui_Cover_FetchConfirm", rows.Count)))
        {
            return;
        }

        await DownloadCoversForRowsAsync(rows);
    }

    private async Task DownloadCoversForRowsAsync(List<GameRow> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var service = new Services.SteamCoverService();
        var ok = 0;
        var failed = 0;
        Exception? firstError = null;
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
            catch (Exception exception)
            {
                failed++;
                firstError ??= exception;
                // Continue with the next one.
            }
        }

        if (firstError is not null)
        {
            Services.CrashLog.Write(firstError);
        }
        StatusText = failed == 0
            ? Services.Loc.Format("Ui_Cover_Progress", ok, rows.Count)
            : Services.Loc.Format("Ui_Cover_WithFailures", ok, failed, rows.Count);
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

