using System.Windows;
using GimDeks.Core.Models;

namespace GimDeks.App.Views;

public sealed partial class StoragePickerDialog : Window
{
    private StoragePickerDialog(IReadOnlyList<Storage> storages, string title)
    {
        InitializeComponent();
        Title = title;
        StorageComboBox.ItemsSource = storages.OrderBy(s => s.Urutan).ThenBy(s => s.Nama, StringComparer.CurrentCultureIgnoreCase).ToList();
        StorageComboBox.DisplayMemberPath = nameof(Storage.Nama);
        if (StorageComboBox.Items.Count > 0)
        {
            StorageComboBox.SelectedIndex = 0;
        }
    }

    private Storage? Selected => StorageComboBox.SelectedItem as Storage;

    /// <summary>Shows the dialog modally; returns null when cancelled.</summary>
    public static Storage? Show(IReadOnlyList<Storage> storages, string title)
    {
        var dialog = new StoragePickerDialog(storages, title);
        return dialog.ShowDialog() == true ? dialog.Selected : null;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OkButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
