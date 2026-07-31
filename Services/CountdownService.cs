using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace SKIPPY.Services;

/// <summary>countdown display themes</summary>
public enum CountdownTheme
{
    /// <summary>vertical multi-line (距离 / name / 还有 / x 天 / x 时 / x 分 / x 秒)</summary>
    Xiaopo,

    /// <summary>single line: 距离 name 还有 x 月 x 日 x 时 x 分 x 秒 (month hidden when 0)</summary>
    Default,

    /// <summary>user format string with [name] [year] [month] [day] [hour] [minute] [second] [msecond]</summary>
    Custom,
}

/// <summary>
/// shows a countdown label above the pet (avoiding the CPU window).
/// theme + target persist to countdown.json.
/// </summary>
public class CountdownService
{
    private readonly Window _owner;
    private readonly Border _countdownBorder;
    private readonly TextBlock _countdownText;
    private DispatcherTimer? _timer;
    private readonly string _filePath;

    public string Label { get; private set; } = "";
    public DateTime? Target { get; private set; }
    public CountdownTheme Theme { get; private set; } = CountdownTheme.Xiaopo;
    public string CustomFormat { get; private set; } =
        "距离 [name] 还有 [month] 月 [day] 日 [hour] 时 [minute] 分 [second] 秒";

    public bool IsActive => Target != null;

    public CountdownService(Window owner, Border border, TextBlock text)
    {
        _owner = owner;
        _countdownBorder = border;
        _countdownText = text;
        _filePath = Path.Combine(GetConfigDir(), "countdown.json");
        Load();
        _countdownBorder.IsVisible = false;
    }

    public void Set(string label, DateTime? target, CountdownTheme theme, string customFormat)
    {
        Label = label ?? "";
        Target = target;
        Theme = theme;
        if (!string.IsNullOrWhiteSpace(customFormat))
            CustomFormat = customFormat;
        Persist();

        if (Target == null)
        {
            _countdownBorder.IsVisible = false;
            StopTimer();
            return;
        }

        StartTimer();
        UpdateText();
    }

    public void Clear() => Set("", null, Theme, CustomFormat);

    public void Start()
    {
        if (Target == null) return;
        StartTimer();
        UpdateText();
    }

    public void Stop()
    {
        StopTimer();
        _countdownBorder.IsVisible = false;
    }

    private void StartTimer()
    {
        StopTimer();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateText();
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void UpdateText()
    {
        if (Target == null) return;

        var now = DateTime.Now;
        var diff = Target.Value - now;

        if (diff <= TimeSpan.Zero)
        {
            _countdownText.Text = $"⏰ {Label} 已到！";
        }
        else
        {
            _countdownText.Text = BuildText(diff);
        }

        PositionOverlay();
    }

    private string BuildText(TimeSpan diff)
    {
        int totalDays = diff.Days;
        int years = totalDays / 365;
        int months = totalDays % 365 / 30;
        int days = totalDays % 365 % 30;
        int hours = diff.Hours;
        int mins = diff.Minutes;
        int secs = diff.Seconds;
        int ms = diff.Milliseconds;

        return Theme switch
        {
            CountdownTheme.Xiaopo =>
                $"距离\n{Label}\n还有\n{totalDays} 天\n{hours} 时\n{mins} 分\n{secs} 秒",

            CountdownTheme.Default =>
                $"距离 {Label} 还有" +
                (months > 0 ? $" {months} 月" : "") +
                $" {days} 日 {hours} 时 {mins} 分 {secs} 秒",

            CountdownTheme.Custom => CustomFormat
                .Replace("[name]", Label)
                .Replace("[year]", years.ToString())
                .Replace("[month]", months.ToString())
                .Replace("[day]", days.ToString())
                .Replace("[hour]", hours.ToString())
                .Replace("[minute]", mins.ToString())
                .Replace("[second]", secs.ToString())
                .Replace("[msecond]", ms.ToString()),

            _ => "",
        };
    }

    /// <summary>
    /// position ABOVE the CPU window.
    /// layout (canvas coords): pet top = 180, cpu window top = 180-28 = 152,
    /// countdown bottom = 148 → cy = 148 - h.
    /// </summary>
    private void PositionOverlay()
    {
        _countdownBorder.Measure(new Avalonia.Size(double.PositiveInfinity, double.PositiveInfinity));
        double w = _countdownBorder.DesiredSize.Width;
        double h = _countdownBorder.DesiredSize.Height;
        if (w <= 0) w = 200;
        if (h <= 0) h = 18;

        const int winW = 360, winH = 300;
        const int petCanvasX = 240;
        const int cpuTop = 152;   // petCanvasY(180) - 28

        double cx = petCanvasX + 60 - w / 2;   // centered over pet
        double cy = cpuTop - h - 4;            // above cpu window

        cx = Math.Max(0, Math.Min(cx, winW - w));
        cy = Math.Max(0, Math.Min(cy, winH - h));

        Avalonia.Controls.Canvas.SetLeft(_countdownBorder, cx);
        Avalonia.Controls.Canvas.SetTop(_countdownBorder, cy);
        _countdownBorder.IsVisible = true;
    }

    // ── persistence ───────────────────────────────────────────

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_filePath));
                Label = doc.RootElement.GetProperty("label").GetString() ?? "";
                if (doc.RootElement.TryGetProperty("target", out var t) &&
                    DateTime.TryParse(t.GetString(), out var dt))
                {
                    Target = dt;
                }
                if (doc.RootElement.TryGetProperty("theme", out var th) &&
                    Enum.TryParse<CountdownTheme>(th.GetString(), out var theme))
                {
                    Theme = theme;
                }
                if (doc.RootElement.TryGetProperty("customFormat", out var cf))
                    CustomFormat = cf.GetString() ?? CustomFormat;
            }
        }
        catch { }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(new
            {
                label = Label,
                target = Target?.ToString("o"),
                theme = Theme.ToString(),
                customFormat = CustomFormat,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
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
