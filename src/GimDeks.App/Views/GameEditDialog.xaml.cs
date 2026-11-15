using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.Views;

public partial class GameEditDialog : Window
{
    private static CultureInfo IdId => Services.AppCulture.Current;

    private readonly List<Storage> _storages;
    private readonly Game? _original;

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

        if (original is not null)
        {
            NamaTextBox.Text = original.Nama;
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
            TagTextBox.Text = tagText ?? "";
            SelesaiCheckBox.IsChecked = original.SelesaiMain;
        }
        else
        {
            StatusComboBox.SelectedItem = ((StatusOption[])StatusComboBox.ItemsSource).First();
            StorageComboBox.SelectedIndex = 0;
            FetchCoverButton.IsEnabled = false;
            CoverHintText.Text = Services.Loc.Get("Ui_GameDialog_CoverNewGame");
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
            Nama = nama,
            Versi = NullIfEmpty(VersiTextBox.Text),
            UkuranGb = ukuran.Value,
            Status = status.Status,
            StorageId = storage.StorageId,
            AsalStorage = NullIfEmpty(AsalTextBox.Text),
            Alasan = NullIfEmpty(AlasanTextBox.Text),
            Catatan = NullIfEmpty(CatatanTextBox.Text),
            SelesaiMain = SelesaiCheckBox.IsChecked == true,
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

    private sealed record StatusOption(GameStatus Status, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record StorageOption(long? StorageId, string Label)
    {
        public override string ToString() => Label;
    }
}
