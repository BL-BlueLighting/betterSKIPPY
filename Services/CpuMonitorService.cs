using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace SKIPPY.Services;

/// <summary>
/// cpu usage watching
/// </summary>
public class CpuMonitorService
{
    private Window? _cpuWindow;
    private DispatcherTimer? _cpuTimer;
    private readonly Func<PixelPoint> _getWindowPosition;
    private readonly Func<double> _getWindowWidth;
    private TimeSpan _prevTotalProcessorTime;
    private DateTime _prevSampleTime;
    private bool _usePerformanceCounter; 
    public Window? CpuWindow => _cpuWindow;

    public CpuMonitorService(
        Func<PixelPoint> getWindowPosition,
        Func<double> getWindowWidth)
    {
        _getWindowPosition = getWindowPosition;
        _getWindowWidth = getWindowWidth;

        // special judging for windows
        _usePerformanceCounter = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    /// <summary>
    /// init cpu watching window
    /// </summary>
    public void Initialize()
    {
        // create base
        _cpuWindow = new Window
        {
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            Width = 90.0,
            Height = 24.0,
            CanResize = false,
        };

        var cpuText = new TextBlock
        {
            Text = "CPU: --",
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            FontSize = 11.0,
            FontFamily = new FontFamily("Consolas, monospace"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 20)),
            CornerRadius = new CornerRadius(8.0),
            Padding = new Thickness(8.0, 2.0, 8.0, 2.0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Child = cpuText,
        };

        _cpuWindow.Content = border;
        _cpuWindow.Show();
        UpdatePosition();

        _prevTotalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;
        _prevSampleTime = DateTime.UtcNow;

        _cpuTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
        _cpuTimer.Tick += (_, _) => UpdateCpuReading(cpuText);
        _cpuTimer.Start();
    }

    /// <summary>
    /// update cpu window pos
    /// </summary>
    public void UpdatePosition()
    {
        if (_cpuWindow == null) return;

        var pos = _getWindowPosition();
        double winW = _getWindowWidth();

        // place above the pet — cpu window is 24px tall, put it 4px above
        int newX = pos.X + (int)((winW - _cpuWindow.Width) / 2.0);
        int newY = Math.Max(pos.Y - 28, 2);

        _cpuWindow.Position = new PixelPoint(newX, newY);
    }

    /// <summary>
    /// stop watching and stop window.
    /// </summary>
    public void Stop()
    {
        _cpuTimer?.Stop();
        _cpuTimer = null;
        _cpuWindow?.Close();
        _cpuWindow = null;
    }

    // cpu reading upd
    private void UpdateCpuReading(TextBlock cpuText)
    {
        try
        {
            int usage;

            // windows on system api
            if (_usePerformanceCounter)
            {
                usage = GetWindowsCpuUsageSafe();
            }
            else
            {
                usage = GetCrossPlatformCpuUsage();
            }

            // format
            cpuText.Text = $"CPU: {usage}%";

            Color color = usage switch
            {
                < 30 => Color.FromRgb(140, 220, 140),
                < 70 => Color.FromRgb(240, 200, 80),
                _    => Color.FromRgb(255, 100, 100),
            };
            cpuText.Foreground = new SolidColorBrush(color);
        }
        catch
        {
            // failed view normal
            cpuText.Text = "CPU: --";
        }
    }

    // windows only
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private static int GetWindowsCpuUsageSafe()
    {
        try
        {
            using var counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            counter.NextValue();
            Thread.Sleep(100);
            float raw = counter.NextValue();
            return (int)Math.Round(raw);
        }
        catch
        {
            // no permission or disabled.
            return 0;
        }
    }

    // bad way but works on more platforms.
    private int GetCrossPlatformCpuUsage()
    {
        var currentTime = DateTime.UtcNow;
        var currentProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;

        double elapsedMs = (currentTime - _prevSampleTime).TotalMilliseconds;
        double cpuUsedMs = (currentProcessorTime - _prevTotalProcessorTime).TotalMilliseconds;

        _prevTotalProcessorTime = currentProcessorTime;
        _prevSampleTime = currentTime;

        if (elapsedMs <= 0) return 0;
        int cpuCount = Environment.ProcessorCount;
        if (cpuCount <= 0) cpuCount = 1;

        int usage = (int)Math.Round((cpuUsedMs / elapsedMs) * 100.0 / cpuCount);
        return Math.Clamp(usage, 0, 100);
    }
}
