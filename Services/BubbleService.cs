using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace SKIPPY.Services;

/// <summary>
/// bubble manager. renders inline (one OS window = zero focus-steal).
/// window is fixed 360×220, pet at canvas (240,100) size 120.
/// bubble floats in the space around the pet — no dynamic resize needed.
/// </summary>
public class BubbleService
{
    private readonly Random _random = new();
    private readonly Func<string> _getCurrentSkin;
    private readonly Func<PixelPoint> _getPetScreenPos;   // pet top-left on screen
    private readonly Func<PixelPoint> _getWinPos;          // window top-left on screen
    private readonly Func<bool> _isMenuOpen;
    private readonly Window _owner;
    private readonly Border _bubbleBorder;
    private readonly TextBlock _bubbleText;

    private DispatcherTimer? _bubbleTimer;
    private DispatcherTimer? _autoCloseTimer;
    private bool _bubbleVisible;

    // ── canvas layout constants ──────────────────────────────
    // pet sits at (PX, PY) within a 360×300 window
    // tall window leaves room above the pet for: countdown → CPU window → pet
    public const int PetCanvasX = 240;
    public const int PetCanvasY = 180;
    public const int PetW = 120;
    public const int WinW = 360;
    public const int WinH = 300;

    // conversation
    private static readonly string[] NormalBubbles =
    [
        "你好,朋友！~ :o)我是Skippy，你的自动SCP助手！",
        "没问题，朋友！", "我能帮些什么吗？", ". . .",
        "让我来帮你搞定这个，朋友！", "这就对了，朋友！",
    ];
    private static readonly string[] KcorenaVoice =
    [
        "欸？大狗？", "汪汪汪汪汪汪汪汪", "我们的网站正在蒸蒸日上噢~",
        "10086~是我滴家乡~🎶", "叫————！", "好渴，来杯柠檬什锦花茶！",
    ];
    private const string AngryBubble = "…继续写你妈的傻逼格式？！";

    // ── ctor ──────────────────────────────────────────────────

    public BubbleService(
        Func<string> getCurrentSkin,
        Func<PixelPoint> getPetScreenPos,
        Func<PixelPoint> getWinPos,
        Func<bool> isMenuOpen,
        Window owner, Border bubbleBorder, TextBlock bubbleText)
    {
        _getCurrentSkin = getCurrentSkin;
        _getPetScreenPos = getPetScreenPos;
        _getWinPos = getWinPos;
        _isMenuOpen = isMenuOpen;
        _owner = owner;
        _bubbleBorder = bubbleBorder;
        _bubbleText = bubbleText;
        _bubbleBorder.IsVisible = false;
    }

    // ── timer ─────────────────────────────────────────────────

    public void StartTimer()
    {
        _bubbleTimer?.Stop();
        _bubbleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_random.Next(6, 14)) };
        _bubbleTimer.Tick += (_, _) =>
        {
            if (_bubbleTimer == null) return;
            _bubbleTimer.Interval = TimeSpan.FromSeconds(_random.Next(6, 14));
            ShowBubble(GetBubbleText());
        };
        _bubbleTimer.Start();
    }

    public void Stop() { _bubbleTimer?.Stop(); _bubbleTimer = null; HideBubble(); }

    // ── text ──────────────────────────────────────────────────

    public string GetBubbleText()
    {
        string s = _getCurrentSkin();
        if (s == "kcorena") return KcorenaVoice[_random.Next(KcorenaVoice.Length)];
        if (s == "红温" && _random.NextDouble() < 0.3) return AngryBubble;
        return NormalBubbles[_random.Next(NormalBubbles.Length)];
    }

    static bool IsAngry(string text, string skin)
        => skin == "红温" && text == AngryBubble;

    // ── show / hide ───────────────────────────────────────────

    public void ShowBubble(string text)
    {
        if (_isMenuOpen()) return;
        HideBubble();

        bool angry = IsAngry(text, _getCurrentSkin());
        var petPos = _getPetScreenPos();   // screen coords
        var winPos = _getWinPos();

        // style
        _bubbleBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        _bubbleText.Text = text;
        _bubbleText.Foreground = angry
            ? new SolidColorBrush(Color.FromRgb(200, 30, 30))
            : new SolidColorBrush(Color.FromRgb(30, 30, 30));
        _bubbleText.FontWeight = angry ? FontWeight.Bold : FontWeight.Normal;

        // measure
        _bubbleBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double bw = Math.Max(_bubbleBorder.DesiredSize.Width, 40);
        double bh = Math.Max(_bubbleBorder.DesiredSize.Height, 20);

        var screen = _owner.Screens?.ScreenFromPoint(petPos);
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);

        // prefer right of pet, fallback left
        bool fitsRight = petPos.X + PetW + 4 + bw <= area.Right;
        double screenX = fitsRight
            ? petPos.X + PetW + 4
            : petPos.X - bw - 4;
        double screenY = petPos.Y - bh - 4;  // above the pet

        // clamp to screen
        screenX = Math.Max(area.X, Math.Min(screenX, area.Right - bw));
        screenY = Math.Max(area.Y, Math.Min(screenY, area.Bottom - bh));

        // → canvas coords
        double cx = screenX - winPos.X;
        double cy = screenY - winPos.Y;

        // make sure it fits within the window (Wayland clips overflow)
        if (cx < 0) cx = 0;
        if (cy < 0) cy = 0;
        if (cx + bw > WinW) cx = WinW - bw;
        if (cy + bh > WinH) cy = WinH - bh;

        Canvas.SetLeft(_bubbleBorder, cx);
        Canvas.SetTop(_bubbleBorder, cy);
        _bubbleBorder.IsVisible = true;
        _bubbleVisible = true;

        // 3 s auto-close
        _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _autoCloseTimer.Tick += (_, _) => { _autoCloseTimer?.Stop(); _autoCloseTimer = null; HideBubble(); };
        _autoCloseTimer.Start();
    }

    public void UpdatePosition()
    {
        // reposition bubble when pet moves — lightweight, same logic as ShowBubble
        if (!_bubbleVisible) return;

        double bw = Math.Max(_bubbleBorder.Bounds.Width, 40);
        double bh = Math.Max(_bubbleBorder.Bounds.Height, 20);
        var petPos = _getPetScreenPos();
        var winPos = _getWinPos();
        var screen = _owner.Screens?.ScreenFromPoint(petPos);
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);

        bool fitsRight = petPos.X + PetW + 4 + bw <= area.Right;
        double screenX = fitsRight
            ? petPos.X + PetW + 4
            : petPos.X - bw - 4;
        double screenY = petPos.Y - bh - 4;

        screenX = Math.Max(area.X, Math.Min(screenX, area.Right - bw));
        screenY = Math.Max(area.Y, Math.Min(screenY, area.Bottom - bh));

        double cx = screenX - winPos.X;
        double cy = screenY - winPos.Y;
        if (cx < 0) cx = 0; if (cy < 0) cy = 0;
        if (cx + bw > WinW) cx = WinW - bw;
        if (cy + bh > WinH) cy = WinH - bh;

        Canvas.SetLeft(_bubbleBorder, cx);
        Canvas.SetTop(_bubbleBorder, cy);
    }

    private void HideBubble()
    {
        _bubbleBorder.IsVisible = false;
        _bubbleVisible = false;
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
    }

    public bool IsAdjustingLayout => false;  // no more dynamic resize

    // ── toast ─────────────────────────────────────────────────

    public static void ShowToast(string text, Window? owner = null)
    {
        var toast = new Window
        {
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            Background = Brushes.Transparent, Topmost = true,
            ShowInTaskbar = false, ShowActivated = false, Focusable = false,
            SizeToContent = SizeToContent.WidthAndHeight,
        };
        toast.Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(235, 30, 30, 30)),
            CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 5),
            Child = new TextBlock
            {
                Text = text, Foreground = Brushes.White, FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };
        var scr = owner?.Screens?.Primary ?? toast.Screens?.Primary;
        var wa = scr?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        toast.WindowStartupLocation = WindowStartupLocation.Manual;
        toast.Position = new PixelPoint(wa.X + wa.Width / 2 - 50, wa.Y + wa.Height / 2 - 20);
        if (owner != null) toast.Show(owner); else toast.Show();
        var ct = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        ct.Tick += (_, _) => { ct.Stop(); toast.Close(); };
        ct.Start();
    }
}
