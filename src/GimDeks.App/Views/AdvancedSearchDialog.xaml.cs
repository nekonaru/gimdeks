using System.Windows;

namespace GimDeks.App.Views;

public sealed record AdvancedCriteria(string Name, string Note, string MinGb, string MaxGb, bool Regex);

public partial class AdvancedSearchDialog : Window
{
    public AdvancedSearchDialog()
    {
        InitializeComponent();
    }

    public AdvancedCriteria? Result { get; private set; }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        Result = new AdvancedCriteria(
            NameBox.Text.Trim(),
            NoteBox.Text.Trim(),
            MinBox.Text.Trim(),
            MaxBox.Text.Trim(),
            RegexBox.IsChecked == true);
        DialogResult = true;
    }
}
