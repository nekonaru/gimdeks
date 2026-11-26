using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.App.Services;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.App.ViewModels;

public abstract partial class StatusListViewModelBase : ObservableObject, IRefreshable
{
    private readonly IGameRepository _gameRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly GameStatus _status;

    [ObservableProperty]
    private string _summaryText = "";

    [ObservableProperty]
    private string _statusText = "";

    protected StatusListViewModelBase(
        IGameRepository gameRepository,
        IStorageRepository storageRepository,
        GameStatus status)
    {
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        _status = status;
        Rows = new ObservableCollection<GameRow>();
        SelectedRows = new ObservableCollection<GameRow>();
        SelectedRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSelection));
    }

    public abstract string Title { get; }
    public abstract string Subtitle { get; }
    public abstract string AlasanHeader { get; }

    public ObservableCollection<GameRow> Rows { get; }
    public ObservableCollection<GameRow> SelectedRows { get; }
    public bool HasSelection => SelectedRows.Count > 0;

    public Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ReloadAsync()
    {
        var games = await _gameRepository.GetByStatusAsync(_status);
        Rows.Clear();
        SelectedRows.Clear();
        foreach (var game in games.OrderBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase))
        {
            Rows.Add(new GameRow
            {
                Game = game,
                StorageName = game.Status is GameStatus.Dikecualikan or GameStatus.Dieliminasi
                    ? (game.Status == GameStatus.Dikecualikan ? game.Alasan ?? "" : game.AsalStorage ?? "")
                    : Services.Loc.Get("Ui_NoStorage"),
            });
        }

        SummaryText = Services.Loc.Format("Ui_StatusList_Summary", Rows.Count);
    }

    [RelayCommand]
    private async Task RestoreSelectedAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        var storages = await _storageRepository.GetAllAsync();
        if (storages.Count == 0)
        {
            DialogService.Error(Services.Loc.Get("Ui_Dialog_Restore"), Services.Loc.Get("Ui_Restore_NoStorage"));
            return;
        }

        var target = Views.StoragePickerDialog.Show(storages, Services.Loc.Get("Ui_Move_RestoreTitle"));
        if (target is null)
        {
            return;
        }

        foreach (var row in SelectedRows.ToList())
        {
            var game = row.Game;
            game.Status = GameStatus.SudahAda;
            game.StorageId = target.Id;
            game.Alasan = null;
            game.AsalStorage = null;
            await _gameRepository.UpdateAsync(game);
        }

        StatusText = Services.Loc.Format("Ui_Restore_Done", SelectedRows.Count, target.Nama);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        if (!DialogService.Confirm(Services.Loc.Get("Ui_Delete_Title"), Services.Loc.Format("Ui_Delete_Confirm", SelectedRows.Count)))
        {
            return;
        }

        foreach (var row in SelectedRows.ToList())
        {
            await _gameRepository.DeleteAsync(row.Game.Id);
        }

        await ReloadAsync();
    }
}

public sealed class ExcludedViewModel : StatusListViewModelBase
{
    public ExcludedViewModel(IGameRepository gameRepository, IStorageRepository storageRepository)
        : base(gameRepository, storageRepository, GameStatus.Dikecualikan) { }

    public override string Title => Services.Loc.Get("Ui_Excluded_Title");
    public override string Subtitle => Services.Loc.Get("Ui_Excluded_Subtitle");
    public override string AlasanHeader => Services.Loc.Get("Ui_Excluded_Reason");
}

public sealed class EliminatedViewModel : StatusListViewModelBase
{
    public EliminatedViewModel(IGameRepository gameRepository, IStorageRepository storageRepository)
        : base(gameRepository, storageRepository, GameStatus.Dieliminasi) { }

    public override string Title => Services.Loc.Get("Ui_Eliminated_Title");
    public override string Subtitle => Services.Loc.Get("Ui_Eliminated_Subtitle");
    public override string AlasanHeader => Services.Loc.Get("Ui_Eliminated_Reason");
}
