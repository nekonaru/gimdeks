using System.IO;
using System.Text.Json;
using GimDeks.Data;

namespace GimDeks.App.Services;

public sealed class AppSettings
{
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;
    public bool IsMaximized { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool IsLightTheme { get; set; }
    public string? LastImportedPath { get; set; }
    public string UiLanguage { get; set; } = "id";
    public int LastPage { get; set; }
    public double? LowSpaceThresholdGb { get; set; } = 100;
    public int BackupRetention { get; set; } = 20;
    public bool ShowOnboarding { get; set; } = true;
}

/// <summary>
/// Persists window geometry and small UI preferences as JSON next to the database.
/// The file is read once and cached; Save updates both the cache and the file.
/// </summary>
public sealed class AppSettingsService
{
    private static readonly object Gate = new();
    private static AppSettings? _cached;
    private static string? _cachedPath;

    private readonly string _settingsPath;

    public AppSettingsService()
    {
        _settingsPath = DatabasePaths.ResolveSettingsPath();
    }

    public AppSettings Load()
    {
        lock (Gate)
        {
            if (_cached is not null && string.Equals(_cachedPath, _settingsPath, StringComparison.OrdinalIgnoreCase))
            {
                return _cached;
            }

            _cached = ReadFile();
            _cachedPath = _settingsPath;
            return _cached;
        }
    }

    private AppSettings ReadFile()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveLastImportedPath(string path)
    {
        var settings = Load();
        settings.LastImportedPath = path;
        Save(settings);
    }

    public void Save(AppSettings settings)
    {
        lock (Gate)
        {
            _cached = settings;
            _cachedPath = _settingsPath;
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
}
