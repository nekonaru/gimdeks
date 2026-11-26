using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GimDeks.App.Views;

public sealed class PaletteItem
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required Action Action { get; init; }
}

public partial class CommandPaletteWindow : Window
{
    private readonly List<PaletteItem> _all = new();

    public CommandPaletteWindow()
    {
        InitializeComponent();
        var services = App.Services;
        var main = services.GetRequiredService<MainViewModel>();
        var games = services.GetRequiredService<GimDeks.Core.Interfaces.IGameRepository>().GetAllAsync().GetAwaiter().GetResult();

        foreach (var game in games)
        {
            var id = game.Id;
            _all.Add(new PaletteItem
            {
                Title = game.Nama,
                Subtitle = Loc.Get("Ui_Palette_Game"),
                Action = () =>
                {
                    main.GameList.SetPendingGame(id);
                    main.NavigateTo(Page.GameList);
                },
            });
        }

        _all.Add(new PaletteItem { Title = Loc.Get("Ui_Palette_Theme"), Subtitle = Loc.Get("Ui_Palette_Action"), Action = () => main.ToggleThemeCommand.Execute(null) });
        _all.Add(new PaletteItem { Title = Loc.Get("Ui_Palette_Language"), Subtitle = Loc.Get("Ui_Palette_Action"), Action = () => main.ToggleLanguageCommand.Execute(null) });
        _all.Add(new PaletteItem { Title = Loc.Get("Ui_Palette_Export"), Subtitle = Loc.Get("Ui_Palette_Action"), Action = () => main.ExportCommand.Execute(null) });
        _all.Add(new PaletteItem { Title = Loc.Get("Ui_Palette_Backup"), Subtitle = Loc.Get("Ui_Palette_Action"), Action = () => main.NavigateTo(Page.ImportExport) });
        _all.Add(new PaletteItem { Title = Loc.Get("Ui_Palette_Health"), Subtitle = Loc.Get("Ui_Palette_Action"), Action = () => main.NavigateTo(Page.Health) });
        _all.Add(new PaletteItem { Title = Loc.Get("Ui_Palette_Scan"), Subtitle = Loc.Get("Ui_Palette_Action"), Action = () => main.NavigateTo(Page.ScanGames) });

        Results.ItemsSource = ResultsSource;
        UpdateResults("");
    }

    public ObservableCollection<PaletteItem> ResultsSource { get; } = new();

    private void SearchBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateResults(SearchBox.Text);

    private void UpdateResults(string query)
    {
        ResultsSource.Clear();
        var q = query.Trim();
        foreach (var item in _all)
        {
            if (q.Length == 0 || item.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || item.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                ResultsSource.Add(item);
            }
        }
        if (ResultsSource.Count > 0)
        {
            Results.SelectedIndex = 0;
        }
    }

    private void Results_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) => InvokeSelected();

    private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            InvokeSelected();
        }
        else if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void InvokeSelected()
    {
        if (Results.SelectedItem is PaletteItem item)
        {
            Close();
            item.Action();
        }
    }
}
