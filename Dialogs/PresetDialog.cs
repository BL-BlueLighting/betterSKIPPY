using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SKIPPY.Models;
using SKIPPY.Services;

namespace SKIPPY.Dialogs;

/// <summary>
/// manage custom presets — create, edit, delete.
/// NOTE: never Clear()+Add() inside SelectionChanged — Avalonia's selection model
/// crashes when the collection mutates mid-commit. Always replace ItemsSource.
/// </summary>
public static class PresetDialog
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(80, 200, 80));
    private static readonly SolidColorBrush Gray  = new(Color.FromRgb(140, 140, 140));

    public static void Show(PresetService presets, Action onChanged)
    {
        var d = new Window
        {
            Title = "预设管理",
            Width = 520, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true, CanResize = false,
            Background = Brushes.Black,
        };

        var root = new StackPanel { Margin = new Thickness(24, 20) };
        root.Children.Add(new TextBlock
        {
            Text = "📋 预设管理", FontSize = 22, FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 14),
        });

        var listBox = new ListBox { Height = 200, Margin = new Thickness(0, 0, 0, 12) };
        root.Children.Add(listBox);

        void RefreshList()
        {
            // replace ItemsSource wholesale — safe even during selection commit
            listBox.ItemsSource = presets.AllPresets
                .Select(p => p.Name + (p.IsCustom ? "  (自定义)" : ""))
                .ToList();
        }
        RefreshList();

        // ── new preset ─────────────────────────────────────────
        var nameRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };
        var nameBox = new TextBox { Width = 180, Watermark = "预设名称" };
        nameRow.Children.Add(nameBox);
        var addBtn = new Button
        {
            Content = "新建预设", Padding = new Thickness(14, 6),
            Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
        };
        nameRow.Children.Add(addBtn);
        root.Children.Add(nameRow);

        // ── editing area ───────────────────────────────────────
        var editPanel = new StackPanel { IsVisible = false };
        var itemList = new ListBox { Height = 140, Margin = new Thickness(0, 0, 0, 8) };
        var itemNameBox = new TextBox { Watermark = "片段标题", Width = 200 };
        var itemCodeBox = new TextBox { Watermark = "片段代码（多行）", AcceptsReturn = true, Height = 60 };
        var addItemBtn = new Button { Content = "添加片段", Padding = new Thickness(14, 6), Background = new SolidColorBrush(Color.FromRgb(80, 160, 80)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = new Cursor(StandardCursorType.Hand), FontSize = 12 };
        var editItemBtn = new Button { Content = "编辑选中片段", Padding = new Thickness(14, 6), Background = new SolidColorBrush(Color.FromRgb(60, 130, 220)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = new Cursor(StandardCursorType.Hand), FontSize = 12, IsVisible = false };
        var cancelEditBtn = new Button { Content = "取消编辑", Padding = new Thickness(14, 6), Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = new Cursor(StandardCursorType.Hand), FontSize = 12, IsVisible = false };
        var itemBtnRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };

        editPanel.Children.Add(new TextBlock { Text = "编辑预设", FontSize = 15, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) });
        editPanel.Children.Add(itemList);
        editPanel.Children.Add(new TextBlock { Text = "片段内容：", FontSize = 12, Foreground = Gray, Margin = new Thickness(0, 4, 0, 2) });
        editPanel.Children.Add(itemNameBox);
        editPanel.Children.Add(itemCodeBox);
        itemBtnRow.Children.Add(addItemBtn);
        itemBtnRow.Children.Add(editItemBtn);
        itemBtnRow.Children.Add(cancelEditBtn);
        editPanel.Children.Add(itemBtnRow);
        editPanel.Children.Add(new TextBlock { Text = "💡 双击片段可编辑 · 右键可删除", FontSize = 11, Foreground = Gray, Margin = new Thickness(0, 6, 0, 0) });
        root.Children.Add(editPanel);

        PresetInfo? editing = null;
        int editingItemIndex = -1;   // -1 = add mode, >=0 = editing existing item

        // ── helpers ────────────────────────────────────────────
        void RefreshItems()
        {
            if (editing == null) { itemList.ItemsSource = null; return; }
            itemList.ItemsSource = editing.Items
                .Select(i => i.Header + "  (" + i.Code.Length + " 字符)")
                .ToList();
        }

        // persist + refresh menus + reload list (deferred — never inside SelectionChanged)
        void PersistAndRefresh()
        {
            if (editing != null)
            {
                editing.IsCustom = true;
                presets.SaveCustomPreset(editing);
                onChanged();
            }
            Dispatcher.UIThread.Post(RefreshList, DispatcherPriority.Background);
        }

        void ResetToAddMode()
        {
            editingItemIndex = -1;
            itemNameBox.Text = "";
            itemCodeBox.Text = "";
            addItemBtn.IsVisible = true;
            editItemBtn.IsVisible = false;
            cancelEditBtn.IsVisible = false;
        }

        void LoadItemIntoBoxes(int idx)
        {
            if (editing == null || idx < 0 || idx >= editing.Items.Count) return;
            editingItemIndex = idx;
            itemNameBox.Text = editing.Items[idx].Header;
            itemCodeBox.Text = editing.Items[idx].Code;
            addItemBtn.IsVisible = false;
            editItemBtn.IsVisible = true;
            editItemBtn.Content = "保存修改";
            cancelEditBtn.IsVisible = true;
        }

        // ── events ─────────────────────────────────────────────

        // selection: only sets state, never mutates the list here
        listBox.SelectionChanged += (_, _) =>
        {
            if (listBox.SelectedIndex < 0) return;
            var all = presets.AllPresets.ToList();
            if (listBox.SelectedIndex >= all.Count) return;
            var sel = all[listBox.SelectedIndex];

            if (!sel.IsCustom)
            {
                // built-in → working copy (persisted on first edit via PersistAndRefresh)
                sel = new PresetInfo
                {
                    Name = sel.Name,
                    IsCustom = true,
                    Items = sel.Items.Select(i => new PresetItem { Header = i.Header, Code = i.Code }).ToList(),
                };
            }

            editing = sel;
            editPanel.IsVisible = true;
            ResetToAddMode();
            RefreshItems();
        };

        addBtn.Click += (_, _) =>
        {
            string n = nameBox.Text?.Trim() ?? "";
            if (n == "") return;
            var p = new PresetInfo { Name = n, IsCustom = true };
            presets.SaveCustomPreset(p);
            onChanged();
            nameBox.Text = "";
            editing = p;
            editPanel.IsVisible = true;
            ResetToAddMode();
            RefreshItems();
            Dispatcher.UIThread.Post(RefreshList, DispatcherPriority.Background);
        };

        addItemBtn.Click += (_, _) =>
        {
            if (editing == null) return;
            string h = itemNameBox.Text?.Trim() ?? "";
            string c = itemCodeBox.Text ?? "";
            if (h == "" || c == "") return;
            editing.Items.Add(new PresetItem { Header = h, Code = c });
            PersistAndRefresh();
            itemNameBox.Text = "";
            itemCodeBox.Text = "";
            RefreshItems();
        };

        editItemBtn.Click += (_, _) =>
        {
            if (editing == null || editingItemIndex < 0) return;
            string h = itemNameBox.Text?.Trim() ?? "";
            string c = itemCodeBox.Text ?? "";
            if (h == "" || c == "") return;
            editing.Items[editingItemIndex] = new PresetItem { Header = h, Code = c };
            PersistAndRefresh();
            ResetToAddMode();
            RefreshItems();
        };

        cancelEditBtn.Click += (_, _) => ResetToAddMode();

        itemList.DoubleTapped += (_, _) =>
        {
            if (itemList.SelectedIndex >= 0) LoadItemIntoBoxes(itemList.SelectedIndex);
        };

        // context menu on items
        var editMenu = new MenuItem { Header = "编辑该片段" };
        editMenu.Click += (_, _) =>
        {
            if (itemList.SelectedIndex >= 0) LoadItemIntoBoxes(itemList.SelectedIndex);
        };
        var delItemMenu = new MenuItem { Header = "删除该片段" };
        delItemMenu.Click += (_, _) =>
        {
            if (editing == null || itemList.SelectedIndex < 0) return;
            editing.Items.RemoveAt(itemList.SelectedIndex);
            PersistAndRefresh();
            ResetToAddMode();
            RefreshItems();
        };
        itemList.ContextMenu = new ContextMenu { Items = { editMenu, delItemMenu } };

        // ── delete preset ──────────────────────────────────────
        var delPresetBtn = new Button
        {
            Content = "删除选中预设", Padding = new Thickness(14, 6),
            Background = new SolidColorBrush(Color.FromRgb(200, 60, 60)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 10),
        };
        delPresetBtn.Click += (_, _) =>
        {
            if (listBox.SelectedIndex < 0) return;
            var all = presets.AllPresets.ToList();
            if (listBox.SelectedIndex >= all.Count) return;
            var sel = all[listBox.SelectedIndex];
            if (sel.IsCustom)
            {
                presets.DeleteCustomPreset(sel.Name);
                onChanged();
                editing = null;
                editPanel.IsVisible = false;
                Dispatcher.UIThread.Post(RefreshList, DispatcherPriority.Background);
            }
        };
        root.Children.Add(delPresetBtn);

        var closeBtn = new Button
        {
            Content = "关闭", Padding = new Thickness(24, 8),
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand), FontSize = 13,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        closeBtn.Click += (_, _) => d.Close();
        root.Children.Add(closeBtn);

        d.Content = root;
        d.Show();
    }
}
