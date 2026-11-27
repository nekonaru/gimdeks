using System.Windows;

namespace GimDeks.App.Views;

/// <summary>Shared motion tokens. Durations only animate opacity and transforms.</summary>
public static class Motion
{
    public static Duration Fast => new(TimeSpan.FromMilliseconds(120));
    public static Duration Normal => new(TimeSpan.FromMilliseconds(200));
    public static Duration Page => new(TimeSpan.FromMilliseconds(320));

    public static bool Reduced => SystemParameters.ClientAreaAnimation == false;
}
