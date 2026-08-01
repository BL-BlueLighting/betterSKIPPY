using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

/// <summary>
/// text-input fallback when no mic + response display.
/// shown after STT returns text (or user typed), then LLM responds.
/// </summary>
public static class AiQuestionDialog
{
    public static async void Show(AiService ai, Window owner, string? prefill = null)
    {
        var d = new Window
        {
            Title = "🤖 问 SKIPPY",
            Width = 500, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true, CanResize = true,
            Background = Brushes.Black,
        };

        var root = new StackPanel { Margin = new Thickness(20) };

        root.Children.Add(new TextBlock
        {
            Text = "🤖 问 SKIPPY", FontSize = 20, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 12),
        });

        var inputBox = new TextBox
        {
            Text = prefill ?? "",
            Watermark = "输入你的问题...",
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            MinHeight = 80, Height = 100,
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
        };
        root.Children.Add(inputBox);

        var toolLog = new TextBlock
        {
            Text = "", FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            Margin = new Thickness(0, 4, 0, 0), IsVisible = false,
        };
        root.Children.Add(toolLog);

        var responseBlock = new TextBlock
        {
            Text = "", FontSize = 13, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 12),
        };

        var scroll = new ScrollViewer { Content = responseBlock, MaxHeight = 200 };
        root.Children.Add(scroll);

        var btnRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var sendBtn = new Button
        {
            Content = "发送", Padding = new Thickness(20, 8),
            Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
        };
        var closeBtn = new Button
        {
            Content = "关闭", Padding = new Thickness(20, 8),
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
        };
        btnRow.Children.Add(sendBtn);
        btnRow.Children.Add(closeBtn);
        root.Children.Add(btnRow);

        d.Content = root;

        sendBtn.Click += async (_, _) =>
        {
            string text = inputBox.Text?.Trim() ?? "";
            if (text == "") return;
            sendBtn.IsEnabled = false;
            sendBtn.Content = "思考中...";
            responseBlock.Text = "⏳";
            toolLog.Text = "";
            toolLog.IsVisible = false;

            string? reply = await Task.Run(() => ai.ChatAsync(text,
                onToolResult: tr =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        toolLog.Text += tr + "\n";
                        toolLog.IsVisible = true;
                    });
                }));

            responseBlock.Text = reply ?? "请求失败";
            sendBtn.IsEnabled = true;
            sendBtn.Content = "发送";
        };
        closeBtn.Click += (_, _) => d.Close();

        d.Show();
    }
}
