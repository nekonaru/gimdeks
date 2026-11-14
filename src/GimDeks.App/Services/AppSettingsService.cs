using System.IO;
using System.Text.Json;
using GimDeks.Data;

namespace GimDeks.App.Services;

public sealed class AppSettings
{
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;
    public bool IsMaximized { get; set; }
}

/// <summary>Persists window geometry and small UI preferences as JSON next to the database.</summary>
public sealed class AppSettingsService
{
    private readonly string _settingsPath;

    public AppSettingsService()
    {
        _settingsPath = Path.Combine(DatabasePaths.AppDataDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings));
        }
        catch
        {
            // Settings are best effort; never crash on save.
        }
    }
}
