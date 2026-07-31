using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SKIPPY.Services;

/// <summary>
/// periodically screenshots the screen and checks for "机密分级" via tesseract OCR.
///
/// ── PRIVACY NOTICE ──
/// when enabled, SKIPPY will observe your screen.
/// on KDE (and other Linux DEs), it is strongly recommended to select
/// only a specific browser window — do NOT capture the entire screen.
/// this prevents accidental leaking of sensitive information.
///
/// requires:
///   Linux:   maim (or import/grim) + tesseract with chi_sim
///   macOS:   built-in screencapture + tesseract with chi_sim
///   Windows: built-in screenshot + tesseract with chi_sim
///
/// on detection → callback fires → caller switches to 红温 skin
/// </summary>
public class ScreenMonitorService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly Action _onDetected;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _running;

    private static readonly string TempScreenshot = Path.Combine(Path.GetTempPath(), "skippy_monitor.png");
    private static readonly string TesseractExe = FindTesseract();

    // cache the first working screenshot command to avoid trying broken tools every cycle
    private static (string Exe, string Args)? _workingScreenshotCmd;

    public bool IsRunning => _running;
    public bool TesseractAvailable { get; private set; }

    public ScreenMonitorService(SettingsService settings, Action onDetected)
    {
        _settings = settings;
        _onDetected = onDetected;
        TesseractAvailable = CheckTesseract();
    }

    public void Start()
    {
        if (_running) return;
        TesseractAvailable = CheckTesseract();  // re-check in case tesseract was just installed
        _running = true;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.Data.ScreenMonitorIntervalSec));
        _ = Loop(_cts.Token);
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
    }

    private async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_timer != null)
                    await _timer.WaitForNextTickAsync(ct);

                if (!_settings.Data.ScreenMonitorEnabled)
                {
                    Stop();
                    break;
                }

                // update interval in case settings changed
                if (_timer != null)
                    _timer.Period = TimeSpan.FromSeconds(_settings.Data.ScreenMonitorIntervalSec);

                if (!TesseractAvailable) continue;

                bool ok = CaptureScreenshot();
                if (!ok) continue;

                string? text = RunOcr();
                if (text != null && text.Contains("机密分级"))
                {
                    _onDetected();
                    // cooldown: skip a few checks so we don't fire repeatedly
                    await Task.Delay(10000, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { /* keep trying */ }
        }
    }

    // ── screenshot ────────────────────────────────────────────

    private static bool CaptureScreenshot()
    {
        try { File.Delete(TempScreenshot); } catch { }
        if (_workingScreenshotCmd != null)
        {
            var (exe, args) = _workingScreenshotCmd.Value;
            return RunShell(exe, args) && File.Exists(TempScreenshot);
        }

        // discover working tool once, cache
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _workingScreenshotCmd = DiscoverLinuxScreenshotTool();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _workingScreenshotCmd = ("screencapture", $"-x {TempScreenshot}");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _workingScreenshotCmd = null; // handled separately

        if (_workingScreenshotCmd != null)
        {
            var (exe, args) = _workingScreenshotCmd.Value;
            return RunShell(exe, args) && File.Exists(TempScreenshot);
        }

        // Windows fallback (not cached)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return WindowsScreenshot();

        return false;
    }

    private static (string, string)? DiscoverLinuxScreenshotTool()
    {
        bool wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null;

        var candidates = new List<(string Exe, string Args)>();
        if (wayland)
        {
            candidates.Add(("spectacle", $"-b -o {TempScreenshot}"));
            candidates.Add(("grim", $"-o {TempScreenshot}"));
            candidates.Add(("maim", $"-f png {TempScreenshot}"));
        }
        else
        {
            candidates.Add(("maim", $"-f png {TempScreenshot}"));
            candidates.Add(("import", $"-window root {TempScreenshot}"));
        }

        foreach (var (exe, args) in candidates)
        {
            if (RunShellQuiet(exe, args) && File.Exists(TempScreenshot))
                return (exe, args);
        }
        return null;
    }

    // like RunShell but suppresses stderr (no terminal spam)
    private static bool RunShellQuiet(string exe, string args)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
            });
            if (p == null) return false;
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool WindowsScreenshot()
    {
        // write a tiny .ps1 script to avoid inline quoting hell
        var psFile = Path.Combine(Path.GetTempPath(), "skippy_screenshot.ps1");
        try
        {
            File.WriteAllText(psFile, $@"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$w = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width
$h = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Height
$b = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($b)
$g.CopyFromScreen(0, 0, 0, 0, $b.Size)
$b.Save('{TempScreenshot.Replace("'", "''")}', [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$b.Dispose()
");
            return RunShell("powershell", $"-ExecutionPolicy Bypass -File \"{psFile}\"");
        }
        catch { return false; }
        finally { try { File.Delete(psFile); } catch { } }
    }

    // ── ocr ───────────────────────────────────────────────────

    private string? RunOcr()
    {
        if (!File.Exists(TempScreenshot)) return null;
        try
        {
            var result = RunShellRead(TesseractExe, $"{TempScreenshot} stdout -l chi_sim --psm 6");
            try { File.Delete(TempScreenshot); } catch { }
            return result;
        }
        catch { return null; }
    }

    private static bool CheckTesseract()
    {
        try { return RunShell(TesseractExe, "--version"); }
        catch { return false; }
    }

    /// <summary>find tesseract — bundled portable dir first, then PATH</summary>
    private static string FindTesseract()
    {
        // 1. bundled in publish/tesseract/ (Windows portable install)
        var bundled = Path.Combine(AppContext.BaseDirectory, "tesseract", "tesseract.exe");
        if (File.Exists(bundled)) return bundled;

        // 2. also check .. (dev layout: bin/Debug/net8.0/ → ../publish/tesseract/)
        bundled = Path.Combine(AppContext.BaseDirectory, "..", "publish", "tesseract", "tesseract.exe");
        if (File.Exists(bundled)) return bundled;

        // 3. fallback to PATH
        return "tesseract";
    }

    // ── helpers ───────────────────────────────────────────────

    private static bool RunShell(string exe, string args)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
            p?.WaitForExit(5000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static string? RunShellRead(string exe, string args)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            });
            if (p == null) return null;
            p.WaitForExit(5000);
            var output = p.StandardOutput.ReadToEnd();
            return p.ExitCode == 0 ? output : null;
        }
        catch { return null; }
    }

    public void Dispose()
    {
        Stop();
        try { File.Delete(TempScreenshot); } catch { }
    }
}
