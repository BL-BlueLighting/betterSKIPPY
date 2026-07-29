using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace SKIPPY.Services;

/// <summary>
/// bubble manager
/// </summary>
public class BubbleService
{
    private readonly Random _random = new();
    private readonly Func<string> _getCurrentSkin;
    private readonly Func<PixelPoint> _getWindowPosition;
    private readonly Func<double> _getWindowWidth;
    private readonly Func<double> _getWindowHeight;
    private readonly Func<bool> _isMenuOpen;
    private DispatcherTimer? _bubbleTimer;
    private Window? _bubbleWindow;

    // conversation content

    private static readonly string[] NormalBubbles =
    [
        "你好,朋友！~ :o)我是Skippy，你的自动SCP助手！",
        "没问题，朋友！",
        "我能帮些什么吗？",
        ". . .",
        "让我来帮你搞定这个，朋友！",
        "这就对了，朋友！",
    ];

    // kcorena voice
    private static readonly string[] KcorenaVoice =
    [
        "欸？大狗？",
        "汪汪汪汪汪汪汪汪",
        "我们的网站正在蒸蒸日上噢~",
        "10086~是我滴家乡~🎶",
        "叫————！",
        "好渴，来杯柠檬什锦花茶！",
    ];

    // angry
    private const string AngryBubble = "…继续写你妈的傻逼格式？！";

    // ── 构造函数 ──────────────────────────────────────────────

    public BubbleService(
        Func<string> getCurrentSkin,
        Func<PixelPoint> getWindowPosition,
        Func<double> getWindowWidth,
        Func<double> getWindowHeight,
        Func<bool> isMenuOpen)
    {
        // shit
        _getCurrentSkin = getCurrentSkin;
        _getWindowPosition = getWindowPosition;
        _getWindowWidth = getWindowWidth;
        _getWindowHeight = getWindowHeight;
        _isMenuOpen = isMenuOpen;
    }

    // timer

    /// <summary>
    /// random break with bubble view
    /// </summary>
    public void StartTimer()
    {
        _bubbleTimer?.Stop();

        _bubbleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_random.Next(6, 14)),
        };
        _bubbleTimer.Tick += OnBubbleTimerTick;
        _bubbleTimer.Start();
    }

    /// <summary>
    /// stop timer with window
    /// </summary>
    public void Stop()
    {
        _bubbleTimer?.Stop();
        _bubbleTimer = null;
        _bubbleWindow?.Close();
        _bubbleWindow = null;
    }

    // 定时器 tick——换一个新的随机间隔，然后显示气泡
    private void OnBubbleTimerTick(object? sender, EventArgs e)
    {
        if (_bubbleTimer == null) return;
        _bubbleTimer.Interval = TimeSpan.FromSeconds(_random.Next(6, 14));

        string text = GetBubbleText();
        ShowBubble(text);
    }

    /// <summary>
    /// select bubble text with correct skin.
    /// </summary>
    public string GetBubbleText()
    {
        string skin = _getCurrentSkin();

        if (skin == "kcorena")
        {
            return KcorenaVoice[_random.Next(KcorenaVoice.Length)];
        }

        if (skin == "红温" && _random.NextDouble() < 0.3)
        {
            return AngryBubble;
        }

        return NormalBubbles[_random.Next(NormalBubbles.Length)];
    }

    /// <summary>
    /// is Angry?
    /// </summary>
    public static bool IsAngryBubble(string text, string currentSkin)
    {
        if (currentSkin == "红温" && text == AngryBubble)
            return true;
        return false;
    }

    /// <summary>
    /// bubble viewer
    /// </summary>
    public void ShowBubble(string text)
    {
        if (_isMenuOpen()) return;

        _bubbleWindow?.Close();
        _bubbleWindow = null;

        bool isAngry = IsAngryBubble(text, _getCurrentSkin());
        var winPos = _getWindowPosition();

        _bubbleWindow = new Window
        {
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            ShowActivated = false,
            Focusable = false,     // don't steal keyboard focus from whatever the user is doing
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 220.0,
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            CornerRadius = new CornerRadius(8.0),
            Padding = new Thickness(10.0, 6.0, 10.0, 6.0),
            Margin = new Thickness(2.0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = isAngry
                    ? new SolidColorBrush(Color.FromRgb(200, 30, 30))
                    : new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                FontSize = 13.0,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = isAngry ? FontWeight.Bold : FontWeight.Normal,
                MaxWidth = 200.0,
            },
        };

        _bubbleWindow.Content = border;

        border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double width = border.DesiredSize.Width;
        double height = border.DesiredSize.Height;

        if (width <= 0) width = 200;
        if (height <= 0) height = 40;

        var screen = _bubbleWindow.Screens?.ScreenFromPoint(winPos);
        var workArea = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);

        double petLeftEdge = winPos.X + 20.0;
        double petRightEdge = winPos.X + 100.0;

        double bubbleLeft;
        if (petRightEdge + width + 4.0 <= workArea.Right)
        {
            bubbleLeft = petRightEdge + 4.0;
        }
        else
        {
            bubbleLeft = petLeftEdge - width - 4.0;
        }

        double bubbleTop = winPos.Y + 35.0;

        bubbleLeft = Math.Max(workArea.X, Math.Min(bubbleLeft, workArea.Right - width));
        bubbleTop = Math.Max(workArea.Y, Math.Min(bubbleTop, workArea.Bottom - height));

        _bubbleWindow.Position = new PixelPoint((int)bubbleLeft, (int)bubbleTop);
        _bubbleWindow.Show();
        var autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.0) };
        autoCloseTimer.Tick += (_, _) =>
        {
            autoCloseTimer.Stop();
            _bubbleWindow?.Close();
            _bubbleWindow = null;
        };
        autoCloseTimer.Start();
    }

    /// <summary>
    /// upd pos
    /// </summary>
    public void UpdatePosition()
    {
        if (_bubbleWindow == null || !_bubbleWindow.IsVisible) return;
        double width = _bubbleWindow.Bounds.Width;
        double height = _bubbleWindow.Bounds.Height;
        if (width <= 0.0) width = 200.0;
        if (height <= 0.0) height = 40.0;
        var winPos = _getWindowPosition();
        var screen = _bubbleWindow.Screens?.ScreenFromPoint(winPos);
        var workArea = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);

        double petLeftEdge = winPos.X + 20.0;
        double petRightEdge = winPos.X + 100.0;

        double bubbleLeft;
        if (petRightEdge + width + 4.0 <= workArea.Right)
        {
            bubbleLeft = petRightEdge + 4.0;
        }
        else
        {
            bubbleLeft = petLeftEdge - width - 4.0;
        }

        double bubbleTop = winPos.Y + 35.0;

        bubbleLeft = Math.Max(workArea.X, Math.Min(bubbleLeft, workArea.Right - width));
        bubbleTop = Math.Max(workArea.Y, Math.Min(bubbleTop, workArea.Bottom - height));

        _bubbleWindow.Position = new PixelPoint((int)bubbleLeft, (int)bubbleTop);
    }

    /// <summary>
    /// toast shower
    /// </summary>
    public static void ShowToast(string text)
    {
        var toast = new Window
        {
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            ShowActivated = false,
            Focusable = false,     // no focus stealing
            SizeToContent = SizeToContent.WidthAndHeight,
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(235, 30, 30, 30)),
            CornerRadius = new CornerRadius(8.0),
            Padding = new Thickness(12.0, 5.0, 12.0, 5.0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 12.0,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

        toast.Content = border;

        var screen = toast.Screens?.Primary;
        var workArea = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        toast.WindowStartupLocation = WindowStartupLocation.Manual;
        toast.Position = new PixelPoint(
            workArea.X + (workArea.Width / 2) - 50,
            workArea.Y + (workArea.Height / 2) - 20);
        toast.Show();

        var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.0) };
        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            toast.Close();
        };
        closeTimer.Start();
    }
}
