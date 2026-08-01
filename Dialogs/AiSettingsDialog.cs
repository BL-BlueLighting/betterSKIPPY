using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

/// <summary>AI config — API key, chat model, system prompt</summary>
public static class AiSettingsDialog
{
    public static void Show(AiConfigData cfg, Action onSave)
    {
        var d = new Window
        {
            Title = "🤖 AI 设置",
            Width = 560, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true, CanResize = false,
            Background = Brushes.Black,
        };

        var root = new StackPanel { Margin = new Thickness(24, 20) };

        root.Children.Add(new TextBlock
        {
            Text = "🤖 AI 设置", FontSize = 22, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 14),
        });

        // API Key
        root.Children.Add(new TextBlock { Text = "SiliconFlow API Key", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
        var keyBox = new TextBox
        {
            Text = cfg.ApiKey, Width = 400,
            PasswordChar = '•',
            Margin = new Thickness(0, 0, 0, 10),
        };
        root.Children.Add(keyBox);

        // Chat Model
        root.Children.Add(new TextBlock { Text = "对话模型", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
        var modelBox = new TextBox
        {
            Text = cfg.ChatModel, Width = 400,
            Margin = new Thickness(0, 0, 0, 10),
        };
        root.Children.Add(modelBox);

        // System Prompt
        root.Children.Add(new TextBlock { Text = "系统提示词（SKIPPY 性格）", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
        var promptBox = new TextBox
        {
            Text = cfg.SystemPrompt, Width = 500,
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            MinHeight = 120, Height = 120,
            Margin = new Thickness(0, 0, 0, 8),
        };
        var promptScroll = new ScrollViewer { Content = promptBox, MaxHeight = 140 };
        root.Children.Add(promptScroll);

        // Reset prompt
        var resetBtn = new Button
        {
            Content = "重置为默认提示词",
            Padding = new Thickness(14, 6),
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 12),
        };
        resetBtn.Click += (_, _) => promptBox.Text = AiConfigData.SKIPPY_DEFAULT_PROMPT;
        root.Children.Add(resetBtn);

        // Allow file delete
        var delCb = new CheckBox
        {
            IsChecked = cfg.AllowFileDelete,
            Content = "允许 AI 删除文件（危险操作，谨慎开启）",
            FontSize = 13, Margin = new Thickness(0, 0, 0, 10),
        };
        root.Children.Add(delCb);

        // Buttons
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

        saveBtn.Click += (_, _) =>
        {
            cfg.ApiKey = keyBox.Text?.Trim() ?? "";
            cfg.ChatModel = modelBox.Text?.Trim() ?? "";
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
