namespace SKIPPY.Models;

/// <summary>
/// persistent app settings
/// </summary>
public class SettingsData
{
    /// <summary>enable screen monitoring for "机密分级"</summary>
    public bool ScreenMonitorEnabled { get; set; } = false;

    /// <summary>check interval in seconds</summary>
    public int ScreenMonitorIntervalSec { get; set; } = 3;
}
