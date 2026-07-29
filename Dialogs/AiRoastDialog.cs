using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

/// <summary>
/// dialog of AI
/// </summary>
public static class AiRoastDialog
{
    public static void Show()
    {
        var dialog = new Window
        {
            Title = "AI 吐槽",
            Width = 500.0,
            Height = 480.0,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            CanResize = true,
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
        };

        var rootPanel = new DockPanel { Margin = new Thickness(20.0) };

        var titleBlock = new TextBlock
        {
            Text = "AI 吐槽",
            FontSize = 20.0,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            Margin = new Thickness(0.0, 0.0, 0.0, 12.0),
        };
        DockPanel.SetDock(titleBlock, Dock.Top);
        rootPanel.Children.Add(titleBlock);

        var hintBlock = new TextBlock
        {
            Text = "粘贴 SCP 文章内容，点击发送获取 AI 吐槽：",
            FontSize = 13.0,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
        };
        DockPanel.SetDock(hintBlock, Dock.Top);
        rootPanel.Children.Add(hintBlock);

        var inputBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            Padding = new Thickness(8.0),
            FontSize = 13.0,
            MinHeight = 120.0,
            Height = 160.0,
            Watermark = "在此粘贴文章内容...",
        };

        var inputScroll = new ScrollViewer
        {
            Content = inputBox,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
        };
        DockPanel.SetDock(inputScroll, Dock.Top);
        rootPanel.Children.Add(inputScroll);
        
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
            Spacing = 8.0,
        };

        var sendButton = new Button
        {
            Content = "发送",
            Padding = new Thickness(20.0, 8.0, 20.0, 8.0),
            Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0.0),
            Cursor = new Cursor(StandardCursorType.Hand),
            FontSize = 13.0,
        };

        var closeButton = new Button
        {
            Content = "关闭",
            Padding = new Thickness(20.0, 8.0, 20.0, 8.0),
            Background = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0.0),
            Cursor = new Cursor(StandardCursorType.Hand),
            FontSize = 13.0,
        };

        buttonPanel.Children.Add(sendButton);
        buttonPanel.Children.Add(closeButton);
        DockPanel.SetDock(buttonPanel, Dock.Top);
        rootPanel.Children.Add(buttonPanel);

        // Response area
        var responseBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            CornerRadius = new CornerRadius(4.0),
            Padding = new Thickness(10.0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1.0),
        };

        var responseBlock = new TextBlock
        {
            Text = "等待输入...",
            FontSize = 13.0,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
        };

        var responseScroll = new ScrollViewer
        {
            Content = responseBlock,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            MaxHeight = 200.0,
        };

        responseBorder.Child = responseScroll;
        rootPanel.Children.Add(responseBorder);

        dialog.Content = rootPanel;

        // Events
        sendButton.Click += async (_, _) =>
        {
            string content = inputBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(content))
            {
                responseBlock.Text = "请先粘贴文章内容。";
                responseBlock.Foreground = new SolidColorBrush(Color.FromRgb(200, 50, 50));
                return;
            }

            sendButton.IsEnabled = false;
            sendButton.Content = "请求中...";
            responseBlock.Text = "⏳ 正在请求 AI 吐槽...";
            responseBlock.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));

            string? result = await Task.Run(() => AiRoastService.RoastAsync(content));

            if (result != null)
            {
                responseBlock.Text = result;
                responseBlock.Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            }
            else
            {
                responseBlock.Text = "请求失败，请检查网络或 API 配置。";
                responseBlock.Foreground = new SolidColorBrush(Color.FromRgb(200, 50, 50));
            }

            sendButton.IsEnabled = true;
            sendButton.Content = "发送";
        };

        closeButton.Click += (_, _) => dialog.Close();

        dialog.Show();
    }
}
