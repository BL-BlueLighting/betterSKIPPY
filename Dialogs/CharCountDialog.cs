using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace SKIPPY.Dialogs;

/// <summary>
/// character counting
/// </summary>
public static class CharCountDialog
{
    public static void Show()
    {
        var dialog = new Window
        {
            Title = "字数统计",
            Width = 460.0,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            CanResize = false,
            Background = Brushes.Black,
        };

        var rootPanel = new StackPanel { Margin = new Thickness(25.0, 20.0, 25.0, 20.0) };

        rootPanel.Children.Add(new TextBlock
        {
            Text = "字数统计",
            FontSize = 18.0,
            FontWeight = FontWeight.Bold,
            // Foreground from theme
            Margin = new Thickness(0.0, 0.0, 0.0, 15.0),
        });

        var inputBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(1.0),
            CornerRadius = new CornerRadius(3.0),
            Margin = new Thickness(0.0, 0.0, 0.0, 12.0),
            Width = 400.0,
            Height = 180.0,
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        };

        var inputBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.Transparent,
            // Foreground from theme
            Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
            FontSize = 13.0,
            BorderThickness = new Thickness(0.0),
            MinWidth = 380.0,
            Width = 398.0,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
        };

        inputBorder.Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
            Content = inputBox,
        };

        rootPanel.Children.Add(inputBorder);

        var statsPanel = new StackPanel { Margin = new Thickness(0.0, 0.0, 0.0, 15.0) };

        var cnText = CreateStatLabel("中文字数：0");
        var enText = CreateStatLabel("英文单词数：0");
        var wcText = CreateStatLabel("总字数：0");
        var chText = CreateStatLabel("总字符数：0");

        statsPanel.Children.Add(cnText);
        statsPanel.Children.Add(enText);
        statsPanel.Children.Add(wcText);
        statsPanel.Children.Add(chText);
        rootPanel.Children.Add(statsPanel);

        inputBox.TextChanged += (_, _) =>
        {
            string text = inputBox.Text ?? "";
            int chineseCount = Regex.Matches(text, @"[一-鿿]").Count;
            int englishWords = Regex.Matches(text, @"[a-zA-Z]+").Count;
            int totalWords = chineseCount + englishWords;
            int totalChars = text.Length;

            cnText.Text = $"中文字数：{chineseCount}";
            enText.Text = $"英文单词数：{englishWords}";
            wcText.Text = $"总字数：{totalWords}";
            chText.Text = $"总字符数：{totalChars}";
        };

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

    private static TextBlock CreateStatLabel(string initialText)
    {
        return new TextBlock
        {
            Text = initialText,
            // Foreground from theme
            FontSize = 14.0,
            Margin = new Thickness(0.0, 0.0, 0.0, 4.0),
        };
    }
}
