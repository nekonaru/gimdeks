using System.Windows;
using GimDeks.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GimDeks.App.Views;

public partial class OnboardingWindow : Window
{
    public OnboardingWindow()
    {
        InitializeComponent();
    }

    private void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = App.Services.GetRequiredService<AppSettingsService>();
        var current = settings.Load();
        current.UiLanguage = EnglishRadio.IsChecked == true ? "en" : "id";
        current.ShowOnboarding = false;
        settings.Save(current);
        App.Services.GetRequiredService<LocalizationService>().Apply(current.UiLanguage);
        DialogResult = true;
        Close();
    }
}
