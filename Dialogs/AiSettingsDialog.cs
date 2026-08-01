using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

public static class AiSettingsDialog
{
    private static readonly SolidColorBrush DarkBg = new(Color.FromRgb(40, 40, 40));
    private static readonly SolidColorBrush Sub   = new(Color.FromRgb(140, 140, 140));

    public static void Show(AiConfigData cfg, Action onSave)
    {
        var d = new Window
        {
            Title = "🤖 AI 设置",
            Width = 580, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true, CanResize = false,
            Background = Brushes.Black,
        };

        var scroll = new ScrollViewer { MaxHeight = 650 };
        var root = new StackPanel { Margin = new Thickness(24, 20) };

        root.Children.Add(new TextBlock
        {
            Text = "🤖 AI 设置", FontSize = 22, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 14),
        });

        // ── Chat ───────────────────────────────────────────────
        root.Children.Add(new TextBlock { Text = "💬 对话 (Chat)", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) });

        root.Children.Add(new TextBlock { Text = "API 地址", FontSize = 12, Foreground = Sub, Margin = new Thickness(0, 0, 0, 2) });
        var chatUrl = new TextBox { Text = cfg.Chat.BaseUrl, Width = 450, Background = DarkBg, Margin = new Thickness(0, 0, 0, 8) };
        root.Children.Add(chatUrl);

        root.Children.Add(new TextBlock { Text = "API Key", FontSize = 12, Foreground = Sub, Margin = new Thickness(0, 0, 0, 2) });
        var chatKey = new TextBox { Text = cfg.Chat.ApiKey, Width = 450, PasswordChar = '•', Background = DarkBg, Margin = new Thickness(0, 0, 0, 8) };
        root.Children.Add(chatKey);

        root.Children.Add(new TextBlock { Text = "模型 ID", FontSize = 12, Foreground = Sub, Margin = new Thickness(0, 0, 0, 2) });
        var chatModel = new TextBox { Text = cfg.Chat.Model, Width = 450, Background = DarkBg, Margin = new Thickness(0, 0, 0, 14) };
        root.Children.Add(chatModel);

        // ── STT ────────────────────────────────────────────────
        root.Children.Add(new TextBlock { Text = "🎤 语音识别 (STT)", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) });

        root.Children.Add(new TextBlock { Text = "API 地址", FontSize = 12, Foreground = Sub, Margin = new Thickness(0, 0, 0, 2) });
        var sttUrl = new TextBox { Text = cfg.Stt.BaseUrl, Width = 450, Background = DarkBg, Margin = new Thickness(0, 0, 0, 8) };
        root.Children.Add(sttUrl);

        root.Children.Add(new TextBlock { Text = "API Key（可与对话共用同一 Key）", FontSize = 12, Foreground = Sub, Margin = new Thickness(0, 0, 0, 2) });
        var sttKey = new TextBox { Text = cfg.Stt.ApiKey, Width = 450, PasswordChar = '•', Background = DarkBg, Margin = new Thickness(0, 0, 0, 8) };
        root.Children.Add(sttKey);

        root.Children.Add(new TextBlock { Text = "模型 ID", FontSize = 12, Foreground = Sub, Margin = new Thickness(0, 0, 0, 2) });
        var sttModel = new TextBox { Text = cfg.Stt.Model, Width = 450, Background = DarkBg, Margin = new Thickness(0, 0, 0, 14) };
        root.Children.Add(sttModel);

        // ── System Prompt ──────────────────────────────────────
        root.Children.Add(new TextBlock { Text = "📝 系统提示词", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) });
        var promptBox = new TextBox
        {
            Text = cfg.SystemPrompt, Width = 520,
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            MinHeight = 100, Height = 100, Background = DarkBg,
            Margin = new Thickness(0, 0, 0, 6),
        };
        var promptScroll = new ScrollViewer { Content = promptBox, MaxHeight = 120 };
        root.Children.Add(promptScroll);

        var resetBtn = new Button
        {
            Content = "重置为默认",
            Padding = new Thickness(10, 4),
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 11,
            Margin = new Thickness(0, 0, 0, 12),
        };
        resetBtn.Click += (_, _) => promptBox.Text = AiConfigData.SKIPPY_DEFAULT_PROMPT;
        root.Children.Add(resetBtn);

        // ── File delete ────────────────────────────────────────
        var delCb = new CheckBox
        {
            IsChecked = cfg.AllowFileDelete,
            Content = "允许 AI 删除文件（危险操作，谨慎开启）",
            FontSize = 13, Margin = new Thickness(0, 0, 0, 14),
        };
        root.Children.Add(delCb);

        // ── Buttons ────────────────────────────────────────────
        var btnRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var saveBtn = new Button { Content = "保存", Padding = new Thickness(24, 8), Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13 };
        var cancelBtn = new Button { Content = "取消", Padding = new Thickness(24, 8), Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13 };
        btnRow.Children.Add(saveBtn);
        btnRow.Children.Add(cancelBtn);
        root.Children.Add(btnRow);

        scroll.Content = root;
        d.Content = scroll;

        saveBtn.Click += (_, _) =>
        {
            cfg.Chat.BaseUrl = chatUrl.Text?.Trim() ?? "";
            cfg.Chat.ApiKey  = chatKey.Text?.Trim() ?? "";
            cfg.Chat.Model   = chatModel.Text?.Trim() ?? "";
            cfg.Stt.BaseUrl  = sttUrl.Text?.Trim() ?? "";
            cfg.Stt.ApiKey   = sttKey.Text?.Trim() ?? "";
            cfg.Stt.Model    = sttModel.Text?.Trim() ?? "";
            cfg.SystemPrompt = promptBox.Text?.Trim() ?? "";
            cfg.AllowFileDelete = delCb.IsChecked ?? false;
            onSave();
            BubbleService.ShowToast("AI 设置已保存", null);
            d.Close();
        };
        cancelBtn.Click += (_, _) => d.Close();

        d.Show();
    }
}
