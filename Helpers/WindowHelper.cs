using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace SKIPPY.Helpers;

internal static class WindowHelper
{
    public static double GetScaling(Window w)
        => w.Screens?.ScreenFromWindow(w)?.Scaling ?? 1.0;

    /// <summary>clamp so the PET stays on screen, not the whole window</summary>
    public static void ClampPetToScreen(Window w, int petX, int petY, int petW, int petH, int winW, int winH)
    {
        var s = w.Screens?.ScreenFromWindow(w);
        if (s == null) return;
        var b = s.WorkingArea;
        int nx = Math.Clamp(w.Position.X, b.X - petX, b.X + b.Width - petX - petW);
        int ny = Math.Clamp(w.Position.Y, b.Y - petY, b.Y + b.Height - petY - petH);
        w.Position = new PixelPoint(nx, ny);
    }

    /// <summary>put pet at bottom-right of screen with 20px margin</summary>
    public static void PositionPetAtBottomRight(Window w, int petX, int petY, int petW, int petH, int winW, int winH)
    {
        var s = w.Screens?.ScreenFromWindow(w);
        if (s == null) return;
        var b = s.WorkingArea;
        // pet screen pos = (w.X + petX, w.Y + petY)
        // want pet at (b.Right - petW - 20, b.Bottom - petH - 20)
        w.Position = new PixelPoint(
            b.X + b.Width - petX - petW - 20,
            b.Y + b.Height - petY - petH - 20);
    }

    public static PixelRect GetWorkingArea(Window w)
    {
        var s = w.Screens?.ScreenFromWindow(w);
        return s?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
    }
}
