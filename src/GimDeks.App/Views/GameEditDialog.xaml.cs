using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.Views;

public partial class GameEditDialog : Window
{
    private static readonly CultureInfo IdId = CultureInfo.GetCultureInfo("id-ID");

    private readonly List<Storage> _storages;
    private readonly Game? _original;

    public GameEditDialog(IReadOnlyList<Storage> storages, Game? original, string? tagText = null)
    {
        InitializeComponent();
        _storages = storages.ToList();
        _original = original;
        Title = original is null ? "Tambah Game" : "Ubah Game";

        StatusComboBox.ItemsSource = new[]
        {
            new StatusOption(GameStatus.BelumDownload, "Belum Download"),
            new StatusOption(GameStatus.SudahAda, "Sudah Ada"),
            new StatusOption(GameStatus.Dikecualikan, "Dikecualikan"),
            new StatusOption(GameStatus.Dieliminasi, "Dieliminasi"),
        };
        StorageComboBox.ItemsSource = BuildStorageItems(_storages);

        if (original is not null)
        {
            NamaTextBox.Text = original.Nama;
            UpdateCoverPreview(original.Id);
            FetchCoverButton.IsEnabled = original.Id > 0;
            CoverHintText.Text = "Cari ke Steam dan unduh cover (internet).";
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
            CoverHintText.Text = "Simpan game ini dulu, lalu buka lagi untuk mengunduh cover.";
        }
    }

    public Game? Result { get; private set; }
    public string ResultTagText => TagTextBox.Text.Trim();

    private static StorageOption[] BuildStorageItems(IEnumerable<Storage> storages) =>
        new[] { new StorageOption(null, "Tanpa Storage") }
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
            ErrorTextBlock.Text = "Nama game wajib diisi.";
            return;
        }

        var ukuranText = UkuranTextBox.Text.Trim();
        var ukuran = string.IsNullOrWhiteSpace(ukuranText) ? 0.0 : NumberParser.ParseGb(ukuranText);
        if (ukuran is null)
        {
            ErrorTextBlock.Text = "Ukuran harus berupa angka, contoh 32,2 atau 32.2.";
            return;
        }

        if (ukuran < 0)
        {
            ErrorTextBlock.Text = "Ukuran tidak boleh negatif.";
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
            ErrorTextBlock.Text = "Isi nama game dulu, lalu unduh cover.";
            return;
        }

        if (System.Windows.MessageBox.Show(
                $"Game \"{title}\" akan dicari di Steam (internet) dan cover-nya diunduh dari CDN Steam.\nData yang dikirim ke internet hanya judul game. Lanjutkan?",
                "Ambil Cover (Internet)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        FetchCoverButton.IsEnabled = false;
        CoverHintText.Text = "Mengunduh cover dari Steam...";
        try
        {
            var result = await new Services.SteamCoverService().FetchCoverAsync(title, _original.Id) is { } r ? r : null;
            if (result is null)
            {
                CoverHintText.Text = "Tidak menemukan game ini di Steam.";
                return;
            }

            UpdateCoverPreview(_original.Id);
            CoverHintText.Text = $"Cover tersimpan untuk \"{result.MatchedName}\".";
        }
        catch (Exception exception)
        {
            CoverHintText.Text = "Gagal mengunduh cover: " + exception.Message;
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
