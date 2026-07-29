using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using SKIPPY.Dialogs;
using SKIPPY.Helpers;
using SKIPPY.Menu;
using SKIPPY.Services;

namespace SKIPPY;

/// <summary>
/// 原程序 MainWindow 存天下导致的。
/// 目前是给 mainwindow 拆成伯邑考做成别的。
/// </summary>
public partial class MainWindow : Window
{
    // ── Services ──────────────────────────────────────────────
    private SkinService _skinService = null!;
    private BubbleService _bubbleService = null!;
    private CpuMonitorService _cpuMonitorService = null!;
    private MenuBuilder _menuBuilder = null!;
    private BookmarkService _bookmarkService = null!;

    // drag statuses
    private bool _isDragging;
    private PixelPoint _dragStartWindowPos;
    private Point _dragStartMousePos;

    // enable to flag to forece cancel dragging
    // private bool _forceCancelDrag = false;  // TODO: 记得加回来

    // track whether initial positioning has happened
    private bool _initialPositioned;

    public MainWindow()
    {
        // 透明度在初始化之前设置，否则会闪白底
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];

        // kill the default centering — we do our own positioning
        WindowStartupLocation = WindowStartupLocation.Manual;

        if (!this.IsInitialized)
            InitializeComponent();
        InitializeServices();
        SetupDragDrop();
    }

    // ── Initialization ────────────────────────────────────────

    private void InitializeServices()
    {
        // FindControl 在 Avalonia 编译时绑定偶尔趋势
        // 这里全部显式 find 一遍，多写几行但不会崩
        var ballImage = this.FindControl<Image>("BallImage")!;
        var ball = this.FindControl<Border>("Ball")!;
        var ballMirror = (ScaleTransform)ball.RenderTransform!;

        var menuControl = this.FindControl<ContextMenu>("MenuControl")!;
        var codeMenu = this.FindControl<MenuItem>("CodeMenu")!;
        var portalMenu = this.FindControl<MenuItem>("PortalMenu")!;
        var deletedMenu = this.FindControl<MenuItem>("DeletedMenu")!;
        var skinMenu = this.FindControl<MenuItem>("SkinMenu")!;
        var bookmarkMenu = this.FindControl<MenuItem>("BookmarkMenu")!;

        if (menuControl == null) return;  // this if looks never be triggered, for final.

        _bookmarkService = new BookmarkService();

        _skinService = new SkinService(ballImage, ballMirror);
        _skinService.LoadSkin(_skinService.CurrentSkin);

        _bubbleService = new BubbleService(
            getCurrentSkin: () => _skinService.CurrentSkin,
            getWindowPosition: () => Position,
            getWindowWidth: () => Width,
            getWindowHeight: () => Height,
            isMenuOpen: () => menuControl.IsOpen);

        _cpuMonitorService = new CpuMonitorService(
            getWindowPosition: () => Position,
            getWindowWidth: () => Width);

        _menuBuilder = new MenuBuilder(
            codeMenu, portalMenu, deletedMenu, skinMenu, bookmarkMenu, menuControl,
            onSkinSelected: skinFile =>
            {
                _skinService.LoadSkin(skinFile);
                _bubbleService.ShowBubble(_bubbleService.GetBubbleText());
            },
            bookmarkService: _bookmarkService);

        _menuBuilder.BuildAll();
        _bubbleService.StartTimer();

        // delay cpu window init until first Opened — avoids top-left overlap
        // _cpuMonitorService.Initialize() is called in OnFirstOpened

        Opened += OnFirstOpened;
        PositionChanged += OnPositionChanged;
    }

    /// <summary>
    /// First-time opened: position the window, THEN show cpu window.
    /// This fixes the bug where the cpu label overlaps the pet at (0,0).
    /// </summary>
    private void OnFirstOpened(object? sender, EventArgs e)
    {
        // one-shot — only run on first open
        Opened -= OnFirstOpened;

        // position window at bottom-right
        WindowHelper.PositionAtBottomRight(this);
        WindowHelper.ClampToScreen(this);

        // now safe to show cpu monitor — main window is positioned
        _cpuMonitorService.Initialize();
        _cpuMonitorService.UpdatePosition();

        _bubbleService.UpdatePosition();
        _skinService.UpdateMirror(Position.X, Width);
        _initialPositioned = true;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (!_initialPositioned) return;  // skip before initial pos is set
        _bubbleService.UpdatePosition();
        _cpuMonitorService.UpdatePosition();
        _skinService.UpdateMirror(Position.X, Width);
    }

    /// <summary>
    /// drag to favourite
    /// </summary>
    private void SetupDragDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (HasUrlInDragData(e.Data))
        {
            e.DragEffects = DragDropEffects.Link;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // get url from data
        string? url = ExtractUrlFromDragData(e.Data);

        if (!string.IsNullOrWhiteSpace(url))
        {
            bool added = _bookmarkService.Add(title: "", url: url);
            if (added)
            {
                BubbleService.ShowToast("已收藏！📑");
                _bubbleService.ShowBubble("已收藏：\n" + TruncateUrl(url));
            }
        }

        e.Handled = true;
    }

    // 检查拖放有没有 url
    private static bool HasUrlInDragData(IDataObject data)
    {
        foreach (string format in data.GetDataFormats())
        {
            if (format.Contains("text", StringComparison.OrdinalIgnoreCase) ||
                format.Contains("url", StringComparison.OrdinalIgnoreCase) ||
                format.Contains("UniformResourceLocator", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    // 获取 Url
    private static string? ExtractUrlFromDragData(IDataObject data)
    {
        if (data.Contains("text/uri-list")) // common
        {
            string? rawUris = data.Get("text/uri-list") as string;
            if (!string.IsNullOrWhiteSpace(rawUris))
            {
                string[] lines = rawUris.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (!line.StartsWith('#') && IsHttpUrl(line.Trim()))
                        return line.Trim();
                }
            }
        }

        if (data.Contains("UniformResourceLocator")) // win explorer
        {
            string? url = data.Get("UniformResourceLocator") as string;
            if (!string.IsNullOrWhiteSpace(url))
            {
                url = url.Trim();
                if (IsHttpUrl(url)) return url;
            }
        }

        if (data.Contains("text/x-moz-url")) // firefox
        {
            string? rawText = data.Get("text/x-moz-url") as string;
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                string[] lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (IsHttpUrl(trimmed)) return trimmed;
                }
            }
        }

        if (data.Contains("text/plain"))
        {
            string? rawText = data.Get("text/plain") as string;
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                string trimmed = rawText.Trim();
                if (IsHttpUrl(trimmed)) return trimmed;
            }
        }

        foreach (string format in data.GetDataFormats())// drag by self
        {
            if (format is "text/uri-list" or "text/x-moz-url"
                or "UniformResourceLocator" or "text/plain")
                continue;

            if (data.Get(format) is string raw && !string.IsNullOrWhiteSpace(raw))
            {
                string trimmed = raw.Trim();
                if (IsHttpUrl(trimmed)) return trimmed;
            }
        }

        return null;
    }

    private static bool IsHttpUrl(string text)
    {
        return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    // break url to show bubble
    private static string TruncateUrl(string url, int maxLen = 50)
    {
        try{
            var uri = new Uri(url);
            string h = uri.Host;
            if (uri.AbsolutePath.Length > 1) h += "/...";
            return h;
        }
        catch{
            return url.Length > maxLen ? url[..maxLen] + "..." : url;
        }
    }

    // ── Drag-to-move ──────────────────────────────────────────

    private void Ball_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed) return;

        _dragStartWindowPos = Position;
        _dragStartMousePos = e.GetPosition(this);
        _isDragging = true;
        e.Handled = true;
    }

    private void Ball_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var pos = e.GetPosition(this);
        var d = pos - _dragStartMousePos;
        var s = Screens.ScreenFromWindow(this)?.Scaling ?? 1.0;

        Position = new PixelPoint(
            _dragStartWindowPos.X + (int)Math.Round(d.X * s),
            _dragStartWindowPos.Y + (int)Math.Round(d.Y * s));
        e.Handled = true;
    }

    private void Ball_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        WindowHelper.ClampToScreen(this);
        _bubbleService.UpdatePosition();
        _cpuMonitorService.UpdatePosition();
        e.Handled = true;
    }

    // ── Menu handlers ─────────────────────────────────────────

    private void SearchBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var sb = this.FindControl<TextBox>("SearchBox")!;
        var mc = this.FindControl<ContextMenu>("MenuControl")!;
        string q = sb.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(q)) return;
        try{
            Process.Start(new ProcessStartInfo{
                FileName = "https://scpper.mer.run/search?q=" + Uri.EscapeDataString(q),
                UseShellExecute = true
            });
        }catch{}
        mc.Close();
        sb.Text = "";
    }

    private void CharCount_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
        this.FindControl<ContextMenu>("MenuControl")?.Close();
        CharCountDialog.Show();
    }

    private void AiRoast_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
        this.FindControl<ContextMenu>("MenuControl")?.Close();
        AiRoastDialog.Show();
    }

    private void About_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
        this.FindControl<ContextMenu>("MenuControl")?.Close();
        AboutDialog.Show();
    }

    private void Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
        this.FindControl<ContextMenu>("MenuControl")?.Close();
        (Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }

    // ── Lifecycle ─────────────────────────────────────────────

    protected override void OnClosed(EventArgs e)
    {
        _bubbleService.Stop();
        _cpuMonitorService.Stop();
        base.OnClosed(e);
    }
}
