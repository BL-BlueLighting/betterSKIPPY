namespace SKIPPY.Models;

/// <summary>
/// a code preset — a named collection of code snippets
/// </summary>
public class PresetInfo
{
    public string Name { get; set; } = "";
    public bool IsCustom { get; set; } = false;
    public List<PresetItem> Items { get; set; } = [];
}

/// <summary>
/// single snippet inside a preset
/// </summary>
public class PresetItem
{
    public string Header { get; set; } = "";
    public string Code { get; set; } = "";
}
