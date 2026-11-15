namespace GimDeks.App.Services;

/// <summary>Raises an app-wide request to focus the search box (Ctrl+F).</summary>
public static class FocusService
{
    public static event Action? SearchRequested;

    public static void RaiseSearchRequested() => SearchRequested?.Invoke();
}
