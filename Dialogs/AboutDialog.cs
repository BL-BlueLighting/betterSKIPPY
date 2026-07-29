using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SKIPPY.Dialogs;

/// <summary>
/// About dialog for the SKIPPY application.
/// </summary>
public static class AboutDialog
{
    public static void Show()
    {
        var dialog = new Window
        {
            Title = "关于 SKIPPY",
            Width = 600.0,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            CanResize = false,
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
        };

        var rootPanel = new StackPanel { Margin = new Thickness(25.0, 20.0, 25.0, 20.0) };

        // Title
        rootPanel.Children.Add(new TextBlock
        {
            Text = "SKIPPY - 你的SCP写作助手",
            FontSize = 22.0,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0.0, 0.0, 0.0, 20.0),
        });

        // 开发者
        rootPanel.Children.Add(CreateLinkRow(
            "原作者：",
            "Roger_F",
            "https://space.bilibili.com/43559177"
        ));

        // 新版开发者
        rootPanel.Children.Add(CreateLinkRow(
            "betterSKIPPY 作者：",
            "BL.BlueLighting",
            "https://space.bilibili.com/1534852388"
        ));

        // 灵感
        rootPanel.Children.Add(CreateLinkRow(
            "灵感来源：",
            "你的第一篇SCP！",
            "https://scp-wiki-cn.wikidot.com/your-very-first-scp"));

        // 祖传 cc 协议
        rootPanel.Children.Add(new TextBlock
        {
            Text = "遵循 CC BY-SA 3.0 协议",
            FontSize = 13.0,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0.0, 0.0, 0.0, 20.0),
        });

        var closeButton = new Button
        {
            Content = "关闭",
            Padding = new Thickness(20.0, 7.0, 20.0, 7.0),
            Background = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0.0),
            Cursor = new Cursor(StandardCursorType.Hand),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            FontSize = 13.0,
        };
        closeButton.Click += (_, _) => dialog.Close();
        rootPanel.Children.Add(closeButton);

        dialog.Content = rootPanel;
        dialog.Show();
    }

    private static StackPanel CreateLinkRow(string label, string linkText, string url)
    {
        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
        };

        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 14.0,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
        });

        var link = new TextBlock
        {
            Text = linkText,
            FontSize = 14.0,
            Foreground = new SolidColorBrush(Color.FromRgb(50, 100, 200)),
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        link.PointerPressed += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Ignore launch failures
            }
        };

        panel.Children.Add(link);
        return panel;
    }
}
