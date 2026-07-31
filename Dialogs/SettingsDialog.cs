using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

public static class SettingsDialog
{
    private static readonly SolidColorBrush Green  = new(Color.FromRgb(80, 200, 80));
    private static readonly SolidColorBrush Red    = new(Color.FromRgb(255, 90, 90));
    private static readonly SolidColorBrush Yellow = new(Color.FromRgb(240, 200, 60));
    private static readonly SolidColorBrush Gray   = new(Color.FromRgb(140, 140, 140));
    private static readonly SolidColorBrush Warm   = new(Color.FromRgb(220, 180, 100));

    public static void Show(SettingsService settings, ScreenMonitorService? monitor, Action? onChanged = null)
    {
        var d = new Window
        {
            Title = "设置",
            Width = 480, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true, CanResize = false,
            Background = Brushes.Black,
        };

        var root = new StackPanel { Margin = new Thickness(24, 20) };

        root.Children.Add(new TextBlock
        {
            Text = "⚙️ 设置", FontSize = 22, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── screen monitor ────────────────────────────────────
        root.Children.Add(new TextBlock
        {
            Text = "屏幕监控", FontSize = 15, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 6),
        });

        var cb = new CheckBox
        {
            IsChecked = settings.Data.ScreenMonitorEnabled,
            Content = "检测屏幕上是否出现「机密分级」并自动切换红温皮肤",
            FontSize = 13, Margin = new Thickness(0, 0, 0, 4),
        };
        root.Children.Add(cb);

        // live status
        bool running = monitor?.IsRunning == true;
        var statusText = new TextBlock
        {
            Text = running ? "🔴 当前状态：监控中" : "⚫ 当前状态：未启动",
            FontSize = 12,
            Foreground = running ? Red : Gray,
            Margin = new Thickness(0, 2, 0, 8),
        };
        root.Children.Add(statusText);

        // interval
        var intLabel = new TextBlock
        {
            Text = $"检测间隔：{settings.Data.ScreenMonitorIntervalSec} 秒",
            FontSize = 13, Margin = new Thickness(0, 0, 0, 2),
        };
        root.Children.Add(intLabel);

        var intRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        var slider = new Slider
        {
            Minimum = 1, Maximum = 30,
            Value = settings.Data.ScreenMonitorIntervalSec,
            Width = 180, TickFrequency = 1, IsSnapToTickEnabled = true,
        };
        slider.ValueChanged += (_, _) =>
            intLabel.Text = $"检测间隔：{(int)slider.Value} 秒";
        intRow.Children.Add(slider);
        intRow.Children.Add(new TextBlock { Text = "1s", FontSize = 11, Foreground = Gray, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        intRow.Children.Add(new TextBlock { Text = "30s", FontSize = 11, Foreground = Gray, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        root.Children.Add(intRow);

        // tesseract status
        bool hasTess = monitor?.TesseractAvailable ?? false;
        root.Children.Add(new TextBlock
        {
            Text = hasTess ? "✅ Tesseract OCR 已就绪" : "⚠️ 未检测到 tesseract — 请安装 tesseract + chi_sim 语言包",
            FontSize = 12,
            Foreground = hasTess ? Green : Yellow,
            Margin = new Thickness(0, 8, 0, 8),
        });

        // ── inline warning (hidden by default, shown when enabling first time) ─
        var warningBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(50, 20, 20)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 60, 30)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10), Margin = new Thickness(0, 6, 0, 10),
            IsVisible = false,
        };
        var warnConfirmBtn = new Button
        {
            Content = "我已知晓，继续开启", Padding = new Thickness(16, 6),
            Background = new SolidColorBrush(Color.FromRgb(200, 60, 30)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 12,
        };
        var warnCancelBtn = new Button
        {
            Content = "取消", Padding = new Thickness(16, 6),
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 12,
        };

        warningBorder.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "⚠️ 请注意：\n\n" +
                           "SKIPPY 在启用该功能后会长时间观察你的屏幕，" +
                           "同时会产生一些疑似病毒的操作(如使用 ps1 脚本截图、调用 Tesseract) 等操作，" +
                           "会导致本软件报毒。\n本软件属于开源的 C# 软件，" +
                           "在 Github 搜索 betterSKIPPY 即可查看到源码，" +
                           "为了保证本软件正常运行，需要您将本软件添加至信任区或暂时关闭杀毒软件。\n\n" +
                           "Linux 用户请安装 maim(X11) / import(ImageMagick) / grim (Wayland) 来正常使用截图。\n\n" +
                           "同时本软件长时间截图可能会导致磁盘寿命折损 / 内存、CPU 占用升高，" +
                           "除非您想体验本功能，否则不建议长时间打开。",
                    FontSize = 12, TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 140)),
                    Margin = new Thickness(0, 0, 0, 10),
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { warnConfirmBtn, warnCancelBtn },
                },
            },
        };
        root.Children.Add(warningBorder);

        // ── privacy ────────────────────────────────────────────
        var privBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(40, 35, 20)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(100, 80, 30)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10), Margin = new Thickness(0, 6, 0, 14),
        };
        privBorder.Child = new TextBlock
        {
            Text = "🛡️ 隐私声明\n\n开启后 SKIPPY 将定期截屏并使用 OCR 检测「机密分级」。\n截图仅用于本地检测，不会上传或存储。\nKDE 用户请选择浏览器窗口，不要选择「整个屏幕」。",
            FontSize = 12, TextWrapping = TextWrapping.Wrap,
            Foreground = Warm,
        };
        root.Children.Add(privBorder);

        // ── save / cancel buttons ─────────────────────────────
        var btnRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        var saveBtn = new Button
        {
            Content = "保存", Padding = new Thickness(24, 8),
            Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
        };
        var cancelBtn = new Button
        {
            Content = "取消", Padding = new Thickness(24, 8),
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
        };
        btnRow.Children.Add(saveBtn);
        btnRow.Children.Add(cancelBtn);
        root.Children.Add(btnRow);

        d.Content = root;

        // ── save logic ────────────────────────────────────────
        saveBtn.Click += (_, _) =>
        {
            bool wasOn = settings.Data.ScreenMonitorEnabled;
            bool wantOn = cb.IsChecked ?? false;

            // first-time enable → show inline warning, defer save
            if (wantOn && !wasOn && !warningBorder.IsVisible)
            {
                warningBorder.IsVisible = true;
                return; // don't save yet — wait for warning confirmation
            }

            DoSave();
        };

        warnConfirmBtn.Click += (_, _) =>
        {
            warningBorder.IsVisible = false;
            DoSave();
        };

        warnCancelBtn.Click += (_, _) =>
        {
            warningBorder.IsVisible = false;
            cb.IsChecked = false;  // uncheck
        };

        cancelBtn.Click += (_, _) => d.Close();

        void DoSave()
        {
            bool wasOn = settings.Data.ScreenMonitorEnabled;
            bool wantOn = cb.IsChecked ?? false;
            settings.Data.ScreenMonitorEnabled = wantOn;
            settings.Data.ScreenMonitorIntervalSec = (int)slider.Value;
            settings.Save();
            onChanged?.Invoke();
            if (wantOn && !wasOn)
                BubbleService.ShowToast("屏幕监控已开启 🔴", null);
            else if (!wantOn && wasOn)
                BubbleService.ShowToast("屏幕监控已关闭 ⚫", null);
            d.Close();
        }

        d.Show();
    }
}
