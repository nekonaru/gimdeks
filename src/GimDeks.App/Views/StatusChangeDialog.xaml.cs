using System.Windows;
using GimDeks.Core.Models;

namespace GimDeks.App.Views;

public sealed partial class StatusChangeDialog : Window
{
    private sealed record StatusOption(GameStatus Status, string Label)
    {
        public override string ToString() => Label;
    }

    private StatusChangeDialog(GameStatus current)
    {
        InitializeComponent();
        StatusComboBox.ItemsSource = new[]
        {
            new StatusOption(GameStatus.BelumDownload, Services.Loc.Get("Ui_Status_BelumDownload")),
            new StatusOption(GameStatus.SudahAda, Services.Loc.Get("Ui_Status_SudahAda")),
            new StatusOption(GameStatus.Dikecualikan, Services.Loc.Get("Ui_Status_Dikecualikan")),
            new StatusOption(GameStatus.Dieliminasi, Services.Loc.Get("Ui_Status_Dieliminasi")),
        };
        StatusComboBox.SelectedItem = ((StatusOption[])StatusComboBox.ItemsSource).First(o => o.Status == current);
    }

    private (GameStatus Status, string? Alasan)? ResultValue { get; set; }

    public static (GameStatus Status, string? Alasan)? Show(GameStatus current)
    {
        var dialog = new StatusChangeDialog(current);
        return dialog.ShowDialog() == true ? dialog.ResultValue : null;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (StatusComboBox.SelectedItem is not StatusOption selected)
        {
            DialogResult = false;
            return;
        }

        ResultValue = (selected.Status,
            string.IsNullOrWhiteSpace(AlasanTextBox.Text) ? null : AlasanTextBox.Text.Trim());
        DialogResult = true;
    }
}
