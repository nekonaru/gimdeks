using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace GimDeks.App.Views;

public partial class GameEditDialog : Window
{
    private static CultureInfo IdId => Services.AppCulture.Current;

    private readonly List<Storage> _storages;
    private readonly Game? _original;
    private readonly Dictionary<long, TextBox> _customBoxes = new();

    public GameEditDialog(IReadOnlyList<Storage> storages, Game? original, string? tagText = null)
    {
        InitializeComponent();
        _storages = storages.ToList();
        _original = original;
        Title = original is null ? Services.Loc.Get("Ui_GameDialog_AddTitle") : Services.Loc.Get("Ui_GameDialog_EditTitle");

        StatusComboBox.ItemsSource = new[]
        {
            new StatusOption(GameStatus.BelumDownload, Services.Loc.Get("Ui_Status_BelumDownload")),
            new StatusOption(GameStatus.SudahAda, Services.Loc.Get("Ui_Status_SudahAda")),
            new StatusOption(GameStatus.Dikecualikan, Services.Loc.Get("Ui_Status_Dikecualikan")),
            new StatusOption(GameStatus.Dieliminasi, Services.Loc.Get("Ui_Status_Dieliminasi")),
        };
        StorageComboBox.ItemsSource = BuildStorageItems(_storages);
        RatingComboBox.ItemsSource = Enumerable.Range(0, 6).Select(i => new RatingOption(i, i == 0 ? Services.Loc.Get("Ui_GameDialog_NoRating") : i + " / 5")).ToArray();
        _ = LoadCustomFieldsAsync();

        if (original is not null)
        {
            NamaTextBox.Text = original.Nama;
            var hasLocalCover = Services.SteamCoverService.FindCoverPath(original.Id) is not null;
            RemoveCoverButton.Visibility = hasLocalCover ? Visibility.Visible : Visibility.Collapsed;
            UpdateCoverPreview(original.Id);
            FetchCoverButton.IsEnabled = original.Id > 0;
            CoverHintText.Text = Services.Loc.Get("Ui_GameDialog_CoverEnabled");
            VersiTextBox.Text = original.Versi ?? "";
            UkuranTextBox.Text = original.UkuranGb.ToString("N1", IdId);
            StatusComboBox.SelectedItem = ((StatusOption[])StatusComboBox.ItemsSource)
                .First(s => s.Status == original.Status);
            StorageComboBox.SelectedItem = original.StorageId is { } id
                ? ((StorageOption[])StorageComboBox.ItemsSource).FirstOrDefault(s => s.StorageId == id) ?? StorageComboBox.Items[0]
                : StorageComboBox.Items[0];
            AsalTextBox.Text = original.AsalStorage ?? "";
            AlasanTextBox.Text = original.Alasan ?? "";
            CatatanTextBox.Text = original.Catatan ?? "";
            PathExeTextBox.Text = original.PathExe ?? "";
            TagTextBox.Text = tagText ?? "";
            FavoritCheckBox.IsChecked = original.Favorit;
            RatingComboBox.SelectedItem = ((RatingOption[])RatingComboBox.ItemsSource)
                .First(r => r.Value == Math.Clamp(original.Rating, 0, 5));
            PrioritasTextBox.Text = original.Prioritas.ToString(CultureInfo.InvariantCulture);
            FolderTextBox.Text = original.FolderPath ?? "";
            SelesaiCheckBox.IsChecked = original.SelesaiMain;
        }
        else
        {
            StatusComboBox.SelectedItem = ((StatusOption[])StatusComboBox.ItemsSource).First();
            StorageComboBox.SelectedIndex = 0;
            FetchCoverButton.IsEnabled = false;
            RemoveCoverButton.Visibility = Visibility.Collapsed;
            CoverHintText.Text = Services.Loc.Get("Ui_GameDialog_CoverNewGame");
            RatingComboBox.SelectedIndex = 0;
        }
    }

    public Game? Result { get; private set; }
    public string ResultTagText => TagTextBox.Text.Trim();

    private static StorageOption[] BuildStorageItems(IEnumerable<Storage> storages) =>
        new[] { new StorageOption(null, Services.Loc.Get("Ui_NoStorage")) }
            .Concat(storages.OrderBy(s => s.Urutan).ThenBy(s => s.Nama, StringComparer.CurrentCultureIgnoreCase)
                .Select(s => new StorageOption(s.Id, s.Nama)))
            .ToArray();

    private void StatusComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusComboBox.SelectedItem is StatusOption { Status: GameStatus.BelumDownload or GameStatus.SudahAda })
        {
            StorageComboBox.IsEnabled = true;
        }
        else
        {
            // Dikecualikan and Dieliminasi carry no storage by definition.
            StorageComboBox.SelectedIndex = 0;
            StorageComboBox.IsEnabled = false;
        }
    }

    private async Task LoadCustomFieldsAsync()
    {
        var repo = App.Services.GetRequiredService<GimDeks.Core.Interfaces.ICustomFieldRepository>();
        var fields = await repo.GetAllAsync();
        if (fields.Count == 0)
        {
            return;
        }
        var values = _original is null ? new Dictionary<long, string?>() : await repo.GetValuesAsync(_original.Id);
        foreach (var field in fields)
        {
            var label = new TextBlock { Text = field.Nama, Opacity = 0.8 };
            var box = new TextBox
            {
                Margin = new Thickness(0, 4, 0, 10),
                Text = values.TryGetValue(field.Id, out var value) ? value ?? "" : "",
            };
            _customBoxes[field.Id] = box;
            CustomFieldsPanel.Children.Add(label);
            CustomFieldsPanel.Children.Add(box);
        }
    }

    public async Task SaveCustomFieldsAsync(long gameId)
    {
        if (_customBoxes.Count == 0)
        {
            return;
        }
        var repo = App.Services.GetRequiredService<GimDeks.Core.Interfaces.ICustomFieldRepository>();
        var values = _customBoxes.ToDictionary(p => p.Key, p => string.IsNullOrWhiteSpace(p.Value.Text) ? null : p.Value.Text.Trim());
        await repo.SetValuesAsync(gameId, values);
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var nama = NamaTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nama))
        {
            ErrorTextBlock.Text = Services.Loc.Get("Ui_GameDialog_NameRequired");
            return;
        }

        var ukuranText = UkuranTextBox.Text.Trim();
        var ukuran = string.IsNullOrWhiteSpace(ukuranText) ? 0.0 : NumberParser.ParseGb(ukuranText);
        if (ukuran is null)
        {
            ErrorTextBlock.Text = Services.Loc.Get("Ui_GameDialog_SizeInvalid");
            return;
        }

        if (ukuran < 0)
        {
            ErrorTextBlock.Text = Services.Loc.Get("Ui_GameDialog_SizeNegative");
            return;
        }

        var status = (StatusOption)StatusComboBox.SelectedItem!;
        var storage = StorageComboBox.IsEnabled ? (StorageOption)StorageComboBox.SelectedItem! : (StorageOption)StorageComboBox.Items[0]!;

        Result = new Game
        {
            Id = _original?.Id ?? 0,
            Nama = nama,
            Versi = NullIfEmpty(VersiTextBox.Text),
            UkuranGb = ukuran.Value,
            Status = status.Status,
            StorageId = storage.StorageId,
            AsalStorage = NullIfEmpty(AsalTextBox.Text),
            Alasan = NullIfEmpty(AlasanTextBox.Text),
            Catatan = NullIfEmpty(CatatanTextBox.Text),
            PathExe = NullIfEmpty(PathExeTextBox.Text),
            SelesaiMain = SelesaiCheckBox.IsChecked == true,
            Favorit = FavoritCheckBox.IsChecked == true,
            Rating = RatingComboBox.SelectedItem is RatingOption rating ? rating.Value : 0,
            Prioritas = int.TryParse(PrioritasTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var prioritas) ? prioritas : 0,
            FolderPath = NullIfEmpty(FolderTextBox.Text),
            JumlahMain = _original?.JumlahMain ?? 0,
            DimainkanPada = _original?.DimainkanPada,
            TotalDetik = _original?.TotalDetik ?? 0,
            Urutan = _original?.Urutan ?? 0,
        };

        DialogResult = true;
    }

    private static string? NullIfEmpty(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private void UpdateCoverPreview(long gameId)
    {
        var path = Services.SteamCoverService.FindCoverPath(gameId);
        CoverImage.Source = path is null ? null : new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.Absolute));
    }

    private async void FetchCoverButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_original is null || _original.Id <= 0)
        {
            return;
        }

        var title = NamaTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorTextBlock.Text = Services.Loc.Get("Ui_GameDialog_CoverNameFirst");
            return;
        }

        if (System.Windows.MessageBox.Show(
                Services.Loc.Format("Ui_Cover_OnlineBody", title),
                Services.Loc.Get("Ui_Cover_OnlineTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        FetchCoverButton.IsEnabled = false;
        CoverHintText.Text = Services.Loc.Get("Ui_Cover_Downloading");
        try
        {
            var result = await new Services.SteamCoverService().FetchCoverAsync(title, _original.Id) is { } r ? r : null;
            if (result is null)
            {
                CoverHintText.Text = Services.Loc.Get("Ui_GameDialog_CoverNotFound");
                return;
            }

            UpdateCoverPreview(_original.Id);
            CoverHintText.Text = Services.Loc.Format("Ui_GameDialog_CoverSaved", result.MatchedName);
        }
        catch (Exception exception)
        {
            CoverHintText.Text = Services.Loc.Format("Ui_GameDialog_CoverFailed", exception.Message);
        }
        finally
        {
            FetchCoverButton.IsEnabled = true;
        }
    }

    private void BrowseExeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe",
            Title = Services.Loc.Get("Ui_GameDialog_PathExeTitle"),
        };
        if (dialog.ShowDialog() == true)
        {
            PathExeTextBox.Text = dialog.FileName;
        }
    }

    private void RemoveCoverButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_original is null || _original.Id <= 0)
        {
            return;
        }

        var removed = new Services.CoverFileService().RemoveCover(_original.Id);
        UpdateCoverPreview(_original.Id);
        Views.GameIdToCoverConverter.Invalidate(_original.Id);
        RemoveCoverButton.Visibility = Visibility.Collapsed;
        CoverHintText.Text = removed
            ? Services.Loc.Get("Ui_Button_CoverRemoved")
            : Services.Loc.Get("Ui_GameDialog_CoverNoCover");
    }

    private sealed record StatusOption(GameStatus Status, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record StorageOption(long? StorageId, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record RatingOption(int Value, string Label)
    {
        public override string ToString() => Label;
    }
}

