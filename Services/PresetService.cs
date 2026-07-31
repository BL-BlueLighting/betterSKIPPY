using System.Text.Json;
using SKIPPY.Models;

namespace SKIPPY.Services;

/// <summary>
/// manages code presets. ships with built-in presets (read-only),
/// user presets are stored in JSON next to bookmarks/settings.
/// </summary>
public class PresetService
{
    private readonly string _filePath;
    private List<PresetInfo> _customPresets = [];

    public IReadOnlyList<PresetInfo> CustomPresets => _customPresets;

    public event Action? PresetsChanged;

    /// <summary>
    /// all presets = built-in + custom.
    /// a custom preset with the same name as a built-in overrides it (built-in hidden).
    /// </summary>
    public IEnumerable<PresetInfo> AllPresets
    {
        get
        {
            var customNames = _customPresets.Select(p => p.Name).ToHashSet();
            foreach (var p in BuiltInPresets)
            {
                if (!customNames.Contains(p.Name))
                    yield return p;
            }
            foreach (var p in _customPresets) yield return p;
        }
    }

    public PresetService()
    {
        _filePath = Path.Combine(GetConfigDir(), "presets.json");
        Load();
    }

    // ── built-in presets (read-only) ──────────────────────────

    private static readonly PresetInfo[] BuiltInPresets =
    [
        new()
        {
            Name = "Starter Preset",
            Items =
            [
                new() { Header = "评分模块", Code = "[[>]]\n[[module Rate]]\n[[/>]]" },
                new()
                {
                    Header = "基本格式",
                    Code = "[[>]]\n[[module Rate]]\n[[/>]]\n\n" +
                           "**项目编号：**SCP-CN-XXXX\n" +
                           "**项目等级：**Safe/Euclid/Keter（表明分级）\n\n" +
                           "**特殊收容措施：** [说明收容措施的段落]\n\n" +
                           "**描述：** [描述SCP的段落]\n\n" +
                           "**附录：** [可选的附加段落]\n\n" +
                           "[[footnote]]\n[[/footnote]]\n\n" +
                           "[[div class=\"footer-wikiwalk-nav\"]]\n[[=]]\n" +
                           "<< [[[SCP-CN-XXXW]]] | SCP-CN-XXXX | [[[SCP-CN-XXXY]]] >>\n" +
                           "[[/=]]\n[[/div]]",
                },
                new() { Header = "引用块", Code = "[[div class=\"blockquote\"]]\n[[/div]]" },
                new()
                {
                    Header = "折叠",
                    Code = "[[collapsible show=\"+ 点我打开\" hide=\"- 点我隐藏\"]]\n[[/collapsible]]",
                },
                new()
                {
                    Header = "图片框",
                    Code = "[[include component:image-block\n| name=\n| caption=\n]]",
                },
            ],
        },
        new()
        {
            Name = "中文CN专用预设",
            Items =
            [
                new() { Header = "CN评分模块", Code = "[[>]]\n[[module Rate]]\n[[/>]]" },
                new()
                {
                    Header = "CN格式",
                    Code = "[[>]]\n[[module Rate]]\n[[/>]]\n\n" +
                           "**项目编号：**SCP-CN-XXX\n" +
                           "**项目等级：**Safe\n" +
                           "**特殊收容措施：**\n\n**描述：**",
                },
            ],
        },
    ];

    // ── user presets ──────────────────────────────────────────

    /// <summary>create/overwrite a custom preset</summary>
    public void SaveCustomPreset(PresetInfo preset)
    {
        preset.IsCustom = true;
        var existing = _customPresets.FirstOrDefault(p => p.Name == preset.Name);
        if (existing != null)
            _customPresets[_customPresets.IndexOf(existing)] = preset;
        else
            _customPresets.Add(preset);
        Persist();
        PresetsChanged?.Invoke();
    }

    public void DeleteCustomPreset(string name)
    {
        _customPresets.RemoveAll(p => p.Name == name);
        Persist();
        PresetsChanged?.Invoke();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                _customPresets = JsonSerializer.Deserialize<List<PresetInfo>>(File.ReadAllText(_filePath)) ?? [];
        }
        catch { _customPresets = []; }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath,
                JsonSerializer.Serialize(_customPresets, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-critical */ }
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
