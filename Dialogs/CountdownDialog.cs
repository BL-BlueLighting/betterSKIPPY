using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

/// <summary>
/// set/clear the countdown — label, target, theme
/// </summary>
public static class CountdownDialog
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(80, 200, 80));
    private static readonly SolidColorBrush Gray  = new(Color.FromRgb(140, 140, 140));

    public static void Show(CountdownService countdown)
    {
        var d = new Window
        {
            Title = "倒计时",
            Width = 460, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true, CanResize = false,
            Background = Brushes.Black,
        };

        var root = new StackPanel { Margin = new Thickness(24, 20) };

        root.Children.Add(new TextBlock
        {
            Text = "⏰ 倒计时", FontSize = 22, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 16),
        });

        // label
        root.Children.Add(new TextBlock { Text = "名称（如：SCP竞赛）", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
        var labelBox = new TextBox
        {
            Text = countdown.Label,
            Width = 300, Margin = new Thickness(0, 0, 0, 12),
        };
        root.Children.Add(labelBox);

        // datetime
        root.Children.Add(new TextBlock { Text = "目标时间", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
        var datePicker = new DatePicker
        {
            SelectedDate = countdown.Target ?? new DateTimeOffset(DateTime.Now + TimeSpan.FromDays(30)),
            Margin = new Thickness(0, 0, 0, 6),
        };
        root.Children.Add(datePicker);

        // hour/minute — ComboBox (NumericUpDown renders as invisible spinners on some themes)
        var timeRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };

        var hourCombo = new ComboBox { Width = 70, SelectedIndex = countdown.Target?.Hour ?? 12 };
        for (int i = 0; i < 24; i++) hourCombo.Items.Add($"{i} 时");
        var minCombo = new ComboBox { Width = 70, SelectedIndex = countdown.Target?.Minute ?? 0 };
        for (int i = 0; i < 60; i++) minCombo.Items.Add($"{i} 分");

        timeRow.Children.Add(hourCombo);
        timeRow.Children.Add(minCombo);
        root.Children.Add(timeRow);

        // ── theme ──────────────────────────────────────────────
        root.Children.Add(new TextBlock { Text = "显示主题", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
        var themeCombo = new ComboBox { Width = 200, Margin = new Thickness(0, 0, 0, 6) };
        themeCombo.Items.Add("小破球主题（默认）");
        themeCombo.Items.Add("默认主题");
        themeCombo.Items.Add("自定义主题");
        themeCombo.SelectedIndex = (int)countdown.Theme;
        root.Children.Add(themeCombo);

        // custom format (visible only when Custom selected)
        var customPanel = new StackPanel { IsVisible = countdown.Theme == CountdownTheme.Custom };
        customPanel.Children.Add(new TextBlock
        {
            Text = "自定义格式（可用占位符）：\n" +
                   "[name] [year] [month] [day] [hour] [minute] [second] [msecond]",
            FontSize = 11,
            Foreground = Gray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 4),
        });
        var formatBox = new TextBox
        {
            Text = countdown.CustomFormat,
            Width = 380, AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 50, Margin = new Thickness(0, 0, 0, 10),
        };
        customPanel.Children.Add(formatBox);
        root.Children.Add(customPanel);

        themeCombo.SelectionChanged += (_, _) =>
            customPanel.IsVisible = themeCombo.SelectedIndex == 2;

        // current status
        var statusText = new TextBlock
        {
            Text = countdown.IsActive
                ? $"✅ 当前：{countdown.Label} → {countdown.Target:yyyy-MM-dd HH:mm}"
                : "⚫ 未设置倒计时",
            FontSize = 12,
            Foreground = countdown.IsActive ? Green : Gray,
            Margin = new Thickness(0, 0, 0, 14),
        };
        root.Children.Add(statusText);

        // buttons
        var btnRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var saveBtn = new Button
        {
            Content = "设置", Padding = new Thickness(24, 8),
            Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
        };
        var clearBtn = new Button
        {
            Content = "清除", Padding = new Thickness(24, 8),
            Background = new SolidColorBrush(Color.FromRgb(200, 60, 60)),
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
        btnRow.Children.Add(clearBtn);
        btnRow.Children.Add(cancelBtn);
        root.Children.Add(btnRow);

        d.Content = root;

        saveBtn.Click += (_, _) =>
        {
            string label = labelBox.Text?.Trim() ?? "";
            var dt = (datePicker.SelectedDate ?? new DateTimeOffset(DateTime.Now)).LocalDateTime;
            dt = dt.Date + TimeSpan.FromHours((hourCombo.SelectedIndex < 0 ? 12 : hourCombo.SelectedIndex))
                        + TimeSpan.FromMinutes((minCombo.SelectedIndex < 0 ? 0 : minCombo.SelectedIndex));
            var theme = (CountdownTheme)Math.Clamp(themeCombo.SelectedIndex, 0, 2);
            countdown.Set(label, dt, theme, formatBox.Text ?? "");
            BubbleService.ShowToast("倒计时已设置 ⏰", null);
            d.Close();
        };
        clearBtn.Click += (_, _) =>
        {
            countdown.Clear();
            BubbleService.ShowToast("倒计时已清除", null);
            d.Close();
        };
        cancelBtn.Click += (_, _) => d.Close();

        d.Show();
    }
}
