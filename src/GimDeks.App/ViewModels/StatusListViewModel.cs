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
        foreach (var game in games.OrderBy(g => g.Nama, StringComparer.CurrentCultureIgnoreCase))
        {
            Rows.Add(new GameRow
            {
                Game = game,
                StorageName = game.Status is GameStatus.Dikecualikan or GameStatus.Dieliminasi
                    ? (game.Status == GameStatus.Dikecualikan ? game.Alasan ?? "" : game.AsalStorage ?? "")
                    : "Tanpa Storage",
            });
        }

        SummaryText = $"{Rows.Count} game";
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
            DialogService.Error("Pulihkan", "Belum ada storage. Buat atau import storage dulu.");
            return;
        }

        var target = Views.StoragePickerDialog.Show(storages, "Pulihkan ke Storage");
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

        StatusText = $"{SelectedRows.Count} game dipulihkan ke {target.Nama}.";
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedRows.Count == 0)
        {
            return;
        }

        if (!DialogService.Confirm("Hapus", $"Hapus {SelectedRows.Count} game dari daftar? Tindakan ini tidak bisa dibatalkan."))
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

    public override string Title => "Dikecualikan";
    public override string Subtitle => "Game online, mobile, atau free yang tidak dihitung ke storage manapun.";
    public override string AlasanHeader => "Alasan";
}

public sealed class EliminatedViewModel : StatusListViewModelBase
{
    public EliminatedViewModel(IGameRepository gameRepository, IStorageRepository storageRepository)
        : base(gameRepository, storageRepository, GameStatus.Dieliminasi) { }

    public override string Title => "Dieliminasi";
    public override string Subtitle => "Game yang dikeluarkan dari rencana download.";
    public override string AlasanHeader => "Asal HDD / Alasan";
}
