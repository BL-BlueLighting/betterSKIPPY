using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using SKIPPY.Dialogs;
using SKIPPY.Helpers;
using SKIPPY.Menu;
using SKIPPY.Services;

namespace SKIPPY;

public partial class MainWindow : Window
{
    private const int PetX = BubbleService.PetCanvasX;
    private const int PetY = BubbleService.PetCanvasY;
    private const int PetW = BubbleService.PetW;
    private const int WinW = BubbleService.WinW;
    private const int WinH = BubbleService.WinH;

    private SkinService _skinService = null!;
    private BubbleService _bubbleService = null!;
    private CpuMonitorService _cpuMonitorService = null!;
    private MenuBuilder _menuBuilder = null!;
    private BookmarkService _bookmarkService = null!;
    private PresetService _presets = null!;
    private CountdownService _countdown = null!;
    private AiService _ai = null!;
    private AiRoastService _aiRoast = null!;
    private AiConfigData _aiConfig = new();

    private bool _isDragging;
    private PixelPoint _dragStartWinPos;
    private Point _dragStartMousePos;
    private bool _initialPositioned;
    private bool _wasDrag;  // true if mouse moved enough to count as drag

    private PixelPoint PetScreenPos => new(Position.X + PetX, Position.Y + PetY);

    public MainWindow()
    {
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (!this.IsInitialized) InitializeComponent();
        LoadAiConfig();
        InitializeServices();
        SetupDragDrop();
    }

    // ── AI config ──────────────────────────────────────────────
    private void LoadAiConfig()
    {
        try
        {
            var path = Path.Combine(GetConfigDir(), "ai.json");
            if (File.Exists(path))
                _aiConfig = JsonSerializer.Deserialize<AiConfigData>(File.ReadAllText(path)) ?? new();
        }
        catch { }
    }

    private void SaveAiConfig()
    {
        try
        {
            var dir = GetConfigDir();
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ai.json"),
                JsonSerializer.Serialize(_aiConfig, new JsonSerializerOptions { WriteIndented = true }));
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

    // ── Initialization ────────────────────────────────────────

    private void InitializeServices()
    {
        var ballImage = this.FindControl<Image>("BallImage")!;
        var ball = this.FindControl<Border>("Ball")!;
        var ballMirror = (ScaleTransform)ball.RenderTransform!;
        var menuControl = this.FindControl<ContextMenu>("MenuControl")!;
        var codeMenu = this.FindControl<MenuItem>("CodeMenu")!;
        var portalMenu = this.FindControl<MenuItem>("PortalMenu")!;
        var deletedMenu = this.FindControl<MenuItem>("DeletedMenu")!;
        var skinMenu = this.FindControl<MenuItem>("SkinMenu")!;
        var bookmarkMenu = this.FindControl<MenuItem>("BookmarkMenu")!;
        var bubbleOverlay = this.FindControl<Border>("BubbleOverlay")!;
        var bubbleText = this.FindControl<TextBlock>("BubbleText")!;
        var countdownOverlay = this.FindControl<Border>("CountdownOverlay")!;
        var countdownText = this.FindControl<TextBlock>("CountdownText")!;
        if (menuControl == null) return;

        _bookmarkService = new BookmarkService();
        _presets = new PresetService();
        _countdown = new CountdownService(this, countdownOverlay, countdownText);

        _ai = new AiService(() => _aiConfig);
        _aiRoast = new AiRoastService(() => _aiConfig);

        _skinService = new SkinService(ballImage, ballMirror);
        _skinService.LoadSkin(_skinService.CurrentSkin);

        _bubbleService = new BubbleService(
            getCurrentSkin: () => _skinService.CurrentSkin,
            getPetScreenPos: () => PetScreenPos,
            getWinPos: () => Position,
            isMenuOpen: () => menuControl.IsOpen,
            owner: this, bubbleOverlay, bubbleText);

        _cpuMonitorService = new CpuMonitorService(
            getWindowPosition: () => PetScreenPos,
            getWindowWidth: () => (double)PetW);

        _menuBuilder = new MenuBuilder(
            codeMenu, portalMenu, deletedMenu, skinMenu, bookmarkMenu, menuControl,
            onSkinSelected: sf => { _skinService.LoadSkin(sf); _bubbleService.ShowBubble(_bubbleService.GetBubbleText()); },
            bookmarkService: _bookmarkService,
            presets: _presets);

        _menuBuilder._openPresetDialog = () => PresetDialog.Show(_presets, () => { });

        _menuBuilder.BuildAll();
        _bubbleService.StartTimer();

        Opened += OnFirstOpened;
        PositionChanged += OnPositionChanged;
    }

    private void OnFirstOpened(object? sender, EventArgs e)
    {
        Opened -= OnFirstOpened;
        WindowHelper.PositionPetAtBottomRight(this, PetX, PetY, PetW, PetW, WinW, WinH);
        _cpuMonitorService.Initialize();
        _cpuMonitorService.UpdatePosition();
        _bubbleService.UpdatePosition();
        _skinService.UpdateMirror(PetScreenPos.X, PetW);
        _countdown.Start();
        _initialPositioned = true;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (!_initialPositioned) return;
        _bubbleService.UpdatePosition();
        _cpuMonitorService.UpdatePosition();
        _skinService.UpdateMirror(PetScreenPos.X, PetW);
        _countdown.Start();
    }

    // ── drag to favourite ─────────────────────────────────────

    private void SetupDragDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragOver(object? s, DragEventArgs e)
    {
        e.DragEffects = HasUrlInDragData(e.Data) ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? s, DragEventArgs e)
    {
        string? url = ExtractUrlFromDragData(e.Data);
        if (!string.IsNullOrWhiteSpace(url) && _bookmarkService.Add("", url))
        {
            BubbleService.ShowToast("已收藏！📑", this);
            _bubbleService.ShowBubble("已收藏：\n" + TruncateUrl(url));
        }
        e.Handled = true;
    }

    private static bool HasUrlInDragData(IDataObject d)
    {
        foreach (var f in d.GetDataFormats())
            if (f.Contains("text", StringComparison.OrdinalIgnoreCase)
             || f.Contains("url", StringComparison.OrdinalIgnoreCase)
             || f.Contains("UniformResourceLocator", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string? ExtractUrlFromDragData(IDataObject d)
    {
        string? TryGet(string fmt)
        {
            if (!d.Contains(fmt)) return null;
            var lines = (d.Get(fmt) as string)?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines?.Select(l => l.Trim()).FirstOrDefault(IsHttpUrl);
        }
        string? u = TryGet("text/uri-list") ?? TryGet("UniformResourceLocator")
                  ?? TryGet("text/x-moz-url") ?? TryGet("text/plain");
        if (u != null) return u;
        foreach (var f in d.GetDataFormats())
        {
            if (f is "text/uri-list" or "text/x-moz-url" or "UniformResourceLocator" or "text/plain") continue;
            if (d.Get(f) is string raw && IsHttpUrl(raw.Trim())) return raw.Trim();
        }
        return null;
    }

    private static bool IsHttpUrl(string t)
        => t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string TruncateUrl(string url, int max = 50)
    {
        try { var u = new Uri(url); return u.Host + (u.AbsolutePath.Length > 1 ? "/..." : ""); }
        catch { return url.Length > max ? url[..max] + "..." : url; }
    }

    // ── drag / click on pet ────────────────────────────────────

    private void Ball_PointerPressed(object? s, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragStartWinPos = Position;
        _dragStartMousePos = e.GetPosition(this);
        _isDragging = true;
        _wasDrag = false;
        e.Handled = true;
    }

    private void Ball_PointerMoved(object? s, PointerEventArgs e)
    {
        if (!_isDragging) return;
        var d = e.GetPosition(this) - _dragStartMousePos;
        if (Math.Abs(d.X) > 3 || Math.Abs(d.Y) > 3) _wasDrag = true;
        if (!_wasDrag) return;  // ignore tiny moves, treat as click

        var sc = Screens.ScreenFromWindow(this)?.Scaling ?? 1.0;
        Position = new PixelPoint(
            _dragStartWinPos.X + (int)Math.Round(d.X * sc),
            _dragStartWinPos.Y + (int)Math.Round(d.Y * sc));
        e.Handled = true;
    }

    private async void Ball_PointerReleased(object? s, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (!_wasDrag)
        {
            // click (not drag) → AI Question
            await HandleAiQuestionClick();
            return;
        }

        WindowHelper.ClampPetToScreen(this, PetX, PetY, PetW, PetW, WinW, WinH);
        _bubbleService.UpdatePosition();
        _cpuMonitorService.UpdatePosition();
        e.Handled = true;
    }

    /// <summary>click on pet face → record mic or show text input → send to AI</summary>
    private async Task HandleAiQuestionClick()
    {
        var menuControl = this.FindControl<ContextMenu>("MenuControl");
        if (menuControl?.IsOpen == true) return;  // don't trigger during right-click

        bool hasMic = AiService.HasMicrophone();
        string? userText = null;

        if (hasMic)
        {
            _bubbleService.ShowBubble("正在听...🎤");
            await Task.Delay(300);  // brief delay so user sees the bubble

            var audioPath = Path.Combine(Path.GetTempPath(), "skippy_question.wav");
            var recorded = AiService.RecordAudio(audioPath);
            if (recorded != null && File.Exists(recorded))
            {
                _bubbleService.ShowBubble("识别中...");
                userText = await _ai.SpeechToTextAsync(recorded);
                try { File.Delete(recorded); } catch { }
            }

            if (string.IsNullOrWhiteSpace(userText))
            {
                _bubbleService.ShowBubble("没听清，请打字吧");
                userText = null;  // fall through to text dialog
            }
        }

        // if no mic or STT failed → text input
        if (string.IsNullOrWhiteSpace(userText))
        {
            Dispatcher.UIThread.Post(() => AiQuestionDialog.Show(_ai, this, null));
        }
        else
        {
            // got voice text → send to AI directly, show response in dialog
            var finalText = userText;
            Dispatcher.UIThread.Post(() => AiQuestionDialog.Show(_ai, this, finalText));
        }
    }

    // ── menu ──────────────────────────────────────────────────

    private void SearchBtn_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var sb = this.FindControl<TextBox>("SearchBox")!;
        var mc = this.FindControl<ContextMenu>("MenuControl")!;
        var q = sb.Text?.Trim() ?? ""; if (q == "") return;
        try { Process.Start(new ProcessStartInfo { FileName = "https://scpper.mer.run/search?q=" + Uri.EscapeDataString(q), UseShellExecute = true }); } catch { }
        mc.Close(); sb.Text = "";
    }

    private void CharCount_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    { this.FindControl<ContextMenu>("MenuControl")?.Close(); CharCountDialog.Show(); }

    private void AiQuestion_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    { this.FindControl<ContextMenu>("MenuControl")?.Close(); AiQuestionDialog.Show(_ai, this, null); }

    private void AiRoast_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    { this.FindControl<ContextMenu>("MenuControl")?.Close(); AiRoastDialog.Show(_aiRoast); }

    private void PresetManage_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    { this.FindControl<ContextMenu>("MenuControl")?.Close(); PresetDialog.Show(_presets, () => { }); }

    private void Countdown_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    { this.FindControl<ContextMenu>("MenuControl")?.Close(); CountdownDialog.Show(_countdown); }

    private void About_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    { this.FindControl<ContextMenu>("MenuControl")?.Close(); AboutDialog.Show(); }

    private void AiSettings_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.FindControl<ContextMenu>("MenuControl")?.Close();
        AiSettingsDialog.Show(_aiConfig, () => { SaveAiConfig(); });
    }

    private void Exit_Click(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.FindControl<ContextMenu>("MenuControl")?.Close();
        (Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _bubbleService.Stop();
        _cpuMonitorService.Stop();
        _countdown.Stop();
        base.OnClosed(e);
    }
}
