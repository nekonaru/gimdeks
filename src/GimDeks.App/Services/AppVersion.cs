using System.Reflection;

namespace GimDeks.App.Services;

/// <summary>Product version read from the assembly (InformationalVersion, stripped of build metadata).</summary>
public static class AppVersion
{
    public static string Current { get; } = GetVersion();

    private static string GetVersion()
    {
        var version = typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(App).Assembly.GetName().Version?.ToString(3)
            ?? "1.0.0";
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
