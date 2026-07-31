using System.Text.Json;
using SKIPPY.Models;

namespace SKIPPY.Services;

/// <summary>
/// reads/writes settings.json next to the bookmarks file.
/// </summary>
public class SettingsService
{
    private readonly string _filePath;
    private SettingsData _data = new();

    public SettingsData Data => _data;

    public event Action? Changed;

    public SettingsService()
    {
        _filePath = Path.Combine(GetConfigDir(), "settings.json");
        Load();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath,
                JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
            Changed?.Invoke();
        }
        catch { /* non-critical */ }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                _data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(_filePath)) ?? new();
        }
        catch { _data = new(); }
    }

    private static string GetConfigDir()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SKIPPY");
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "SKIPPY");
        var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        return Path.Combine(string.IsNullOrEmpty(xdg)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : xdg, "SKIPPY");
    }
}
