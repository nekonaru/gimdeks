using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.ViewModels;

public sealed class StorageShare
{
    public required string Nama { get; init; }
    public int GameCount { get; init; }
    public double TotalGb { get; init; }
    public double ShareFraction { get; init; }
    private static CultureInfo IdId => Services.AppCulture.Current;
    public string TotalText => $"{TotalGb.ToString("N1", IdId)} GB";
    public string CountText => Services.Loc.Format("Ui_GameCount", GameCount);
    public string ShareText => $"{ShareFraction * 100:0}%";
}

public sealed class BiggestGame
{
    public required string Nama { get; init; }
    public required string StorageName { get; init; }
    public double UkuranGb { get; init; }
    private static CultureInfo IdId => Services.AppCulture.Current;
    public string UkuranText => $"{UkuranGb.ToString("N1", IdId)} GB";
}

public partial class StatisticsViewModel : ObservableObject, IRefreshable
{
    private readonly IGameRepository _gameRepository;
    private readonly IStorageRepository _storageRepository;

    [ObservableProperty]
    private string _totalGamesText = "";

    [ObservableProperty]
    private string _totalSizeText = "";

    [ObservableProperty]
    private string _averageSizeText = "";

    [ObservableProperty]
    private string _completionText = "";

    [ObservableProperty]
    private string _summaryText = "";

    public StatisticsViewModel(IGameRepository gameRepository, IStorageRepository storageRepository)
    {
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        StorageShares = new ObservableCollection<StorageShare>();
        BiggestGames = new ObservableCollection<BiggestGame>();
    }

    public ObservableCollection<StorageShare> StorageShares { get; }
    public ObservableCollection<BiggestGame> BiggestGames { get; }

    [RelayCommand]
    private void ExportReport()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "HTML report (*.html)|*.html",
            FileName = "gimdeks-report.html",
            Title = Services.Loc.Get("Ui_Stats_ReportTitle"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var lines = new List<string>
            {
                "<!doctype html><html><head><meta charset='utf-8'><title>GimDeks</title>",
                "<style>body{font-family:Segoe UI,Arial,sans-serif;margin:40px;color:#14221f}h1{color:#0e6b58}table{border-collapse:collapse;width:100%}td,th{padding:8px;border-bottom:1px solid #ddd;text-align:left}</style>",
                "</head><body><h1>GimDeks</h1>",
                $"<p>{SummaryText}</p><p>{TotalGamesText} | {TotalSizeText} | {AverageSizeText} | {CompletionText}</p>",
                "<h2>Storage</h2><table><tr><th>Storage</th><th>Games</th><th>GB</th><th>Share</th></tr>",
            };
            foreach (var share in StorageShares)
            {
                lines.Add($"<tr><td>{System.Net.WebUtility.HtmlEncode(share.Nama)}</td><td>{share.CountText}</td><td>{share.TotalText}</td><td>{share.ShareText}</td></tr>");
            }
            lines.Add("</table><h2>Biggest games</h2><table><tr><th>Game</th><th>Storage</th><th>GB</th></tr>");
            foreach (var game in BiggestGames)
            {
                lines.Add($"<tr><td>{System.Net.WebUtility.HtmlEncode(game.Nama)}</td><td>{System.Net.WebUtility.HtmlEncode(game.StorageName)}</td><td>{game.UkuranText}</td></tr>");
            }
            lines.Add("</table></body></html>");
            System.IO.File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines));
        }
        catch (Exception exception)
        {
            Services.CrashLog.Write(exception);
        }
    }

    public Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ReloadAsync()
    {
        var games = await _gameRepository.GetAllAsync();
        var storages = await _storageRepository.GetAllAsync();
        var counted = games.Where(g => CapacityCalculator.CountsTowardTotal(g.Status)).ToList();
        var totalGb = counted.Sum(g => g.UkuranGb);
        var average = counted.Count > 0 ? totalGb / counted.Count : 0;
        var selesai = counted.Count(g => g.SelesaiMain);

        TotalGamesText = Services.Loc.Format("Ui_GameCount", counted.Count);
        TotalSizeText = $"{totalGb.ToString("N1", GimDeks.App.Services.AppCulture.Current)} GB";
        AverageSizeText = Services.Loc.Format("Ui_Statistics_Average", average.ToString("N1", GimDeks.App.Services.AppCulture.Current));
        CompletionText = counted.Count > 0
            ? Services.Loc.Format("Ui_Statistics_Completion", selesai, counted.Count,
                (selesai * 100.0 / counted.Count).ToString("0.#", GimDeks.App.Services.AppCulture.Current))
            : Services.Loc.Get("Ui_Statistics_CompletionEmpty");

        StorageShares.Clear();
        var storageById = storages.ToDictionary(s => s.Id, s => s.Nama);
        var byStorage = counted.GroupBy(g => g.StorageId);
        foreach (var group in byStorage.OrderByDescending(g => g.Sum(x => x.UkuranGb)))
        {
            var gb = group.Sum(g => g.UkuranGb);
            StorageShares.Add(new StorageShare
            {
                Nama = group.Key is { } id && storageById.TryGetValue(id, out var name) ? name : Services.Loc.Get("Ui_NoStorage"),
                GameCount = group.Count(),
                TotalGb = gb,
                ShareFraction = totalGb > 0 ? gb / totalGb : 0,
            });
        }

        BiggestGames.Clear();
        foreach (var game in counted.OrderByDescending(g => g.UkuranGb).Take(10))
        {
            BiggestGames.Add(new BiggestGame
            {
                Nama = game.Nama,
                StorageName = game.StorageId is { } storageId && storageById.TryGetValue(storageId, out var name) ? name : Services.Loc.Get("Ui_NoStorage"),
                UkuranGb = game.UkuranGb,
            });
        }

        SummaryText = Services.Loc.Format("Ui_Statistics_Summary", storages.Count, games.Count(g => g.Status == GameStatus.Dikecualikan), games.Count(g => g.Status == GameStatus.Dieliminasi));
    }
}
