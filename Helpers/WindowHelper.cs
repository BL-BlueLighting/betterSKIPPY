using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace SKIPPY.Helpers;

/// <summary>
/// multi platform window manager for Avalonia.
/// </summary>
internal static class WindowHelper
{
    /// <summary>
    /// get dpi
    /// </summary>
    public static double GetScaling(Window window)
    {
        return window.Screens?.ScreenFromWindow(window)?.Scaling ?? 1.0;
    }

    /// <summary>
    /// clamp window on screen
    /// </summary>
    public static void ClampToScreen(Window window)
    {
        var screen = window.Screens?.ScreenFromWindow(window);
        if (screen == null) return;

        var bounds = screen.WorkingArea;
        window.Position = new PixelPoint(
            Math.Max(bounds.X, Math.Min(window.Position.X, bounds.X + bounds.Width - (int)window.Width)),
            Math.Max(bounds.Y, Math.Min(window.Position.Y, bounds.Y + bounds.Height - (int)window.Height)));
    }

    /// <summary>
    /// positions the window at the bottom-right corner with a 20px margin.
    /// </summary>
    public static void PositionAtBottomRight(Window window)
    {
        var screen = window.Screens?.ScreenFromWindow(window);
        if (screen == null) return;

        var bounds = screen.WorkingArea;
        window.Position = new PixelPoint(
            bounds.X + bounds.Width - (int)window.Width - 20,
            bounds.Y + bounds.Height - (int)window.Height - 20);
    }

    /// <summary>
    /// gets the working area of the screen the window is on.
    /// </summary>
    public static PixelRect GetWorkingArea(Window window)
    {
        var screen = window.Screens?.ScreenFromWindow(window);
        return screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
    }
}
