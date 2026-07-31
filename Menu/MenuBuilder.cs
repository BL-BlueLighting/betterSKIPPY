using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SKIPPY.Models;
using SKIPPY.Services;

namespace SKIPPY.Menu;

/// <summary>
/// builds all context menus. code menu now shows presets (built-in + custom).
/// </summary>
public class MenuBuilder
{
    private readonly MenuItem _codeMenu;
    private readonly MenuItem _portalMenu;
    private readonly MenuItem _deletedMenu;
    private readonly MenuItem _skinMenu;
    private readonly MenuItem _bookmarkMenu;
    private readonly ContextMenu _menu;
    private readonly Action<string> _onSkinSelected;
    private readonly BookmarkService _bookmarkService;
    private readonly PresetService _presets;

    private static readonly (string Name, string Url)[] PortalSites =
    [
        ("SCP基金会", "https://scp-wiki-cn.wikidot.com/"),
        ("SCP基金会中文分部沙盒站", "https://scp-wiki-cn.wikidot.com/sandbox"),
        ("如何撰写一篇SCP文档", "https://scp-wiki-cn.wikidot.com/how-to-write-an-scp"),
        ("维基语法", "https://scp-wiki-cn.wikidot.com/wiki-syntax"),
        ("维基语法快速参考", "https://scp-wiki-cn.wikidot.com/syntax-quick-reference"),
        ("SCP格式资源", "https://scp-wiki-cn.wikidot.com/scp-style-resource"),
        ("版式收录及推荐中心页", "https://scp-wiki-cn.wikidot.com/theme-rec-hub"),
        ("SCPPER-CN", "https://scpper.mer.run/"),
    ];

    private static readonly (string Name, string Url)[] DeletedSites =
    [
        ("SCP基金会云国分部", "https://scp-wiki-cloud.wikidot.com/"),
    ];

    public MenuBuilder(
        MenuItem codeMenu,
        MenuItem portalMenu,
        MenuItem deletedMenu,
        MenuItem skinMenu,
        MenuItem bookmarkMenu,
        ContextMenu menu,
        Action<string> onSkinSelected,
        BookmarkService bookmarkService,
        PresetService presets)
    {
        _codeMenu = codeMenu;
        _portalMenu = portalMenu;
        _deletedMenu = deletedMenu;
        _skinMenu = skinMenu;
        _bookmarkMenu = bookmarkMenu;
        _menu = menu;
        _onSkinSelected = onSkinSelected;
        _bookmarkService = bookmarkService;
        _presets = presets;

        _bookmarkService.BookmarksChanged += BuildBookmarkMenu;
        _presets.PresetsChanged += BuildCodeMenu;
    }

    public void BuildAll()
    {
        BuildCodeMenu();
        BuildPortalMenu();
        BuildDeletedMenu();
        BuildBookmarkMenu();
        BuildSkinMenu();
    }

    // ── code menu (from presets) ──────────────────────────────

    private void BuildCodeMenu()
    {
        _codeMenu.Items.Clear();

        foreach (var preset in _presets.AllPresets)
        {
            var presetItem = new MenuItem
            {
                Header = preset.Name + (preset.IsCustom ? " ✏️" : ""),
            };

            foreach (var item in preset.Items)
            {
                var mi = new MenuItem { Header = item.Header };
                mi.Click += async (_, _) =>
                {
                    try
                    {
                        var topLevel = TopLevel.GetTopLevel(_menu);
                        if (topLevel?.Clipboard != null)
                            await topLevel.Clipboard.SetTextAsync(item.Code);
                    }
                    catch { /* ignore clipboard errors */ }
                    BubbleService.ShowToast("已复制");
                    _menu.Close();
                };
                presetItem.Items.Add(mi);
            }

            // presets with no items → disabled
            if (presetItem.Items.Count == 0)
                presetItem.IsEnabled = false;

            _codeMenu.Items.Add(presetItem);
        }

        // always append "manage" entry at bottom
        _codeMenu.Items.Add(new Separator());
        var manage = new MenuItem { Header = "📋 管理预设..." };
        manage.Click += (_, _) =>
        {
            _menu.Close();
            _openPresetDialog?.Invoke();
        };
        _codeMenu.Items.Add(manage);
    }

    public Action? _openPresetDialog;  // set by MainWindow

    private void BuildPortalMenu()
    {
        _portalMenu.Items.Clear();
        foreach (var (name, url) in PortalSites)
        {
            var item = new MenuItem { Header = name };
            item.Click += (_, _) => { OpenUrl(url); _menu.Close(); };
            _portalMenu.Items.Add(item);
        }
    }

    private void BuildDeletedMenu()
    {
        _deletedMenu.Items.Clear();
        foreach (var (name, url) in DeletedSites)
        {
            var item = new MenuItem { Header = name };
            item.Click += (_, _) => { OpenUrl(url); _menu.Close(); };
            _deletedMenu.Items.Add(item);
        }
    }

    private void BuildSkinMenu()
    {
        _skinMenu.Items.Clear();
        foreach (var skin in SkinService.Skins)
        {
            var item = new MenuItem { Header = skin.Name };
            item.Click += (_, _) =>
            {
                _onSkinSelected(skin.File);
                _menu.Close();
            };
            _skinMenu.Items.Add(item);
        }
    }

    private void BuildBookmarkMenu()
    {
        _bookmarkMenu.Items.Clear();
        var bookmarks = _bookmarkService.Bookmarks;
        if (bookmarks.Count == 0)
        {
            _bookmarkMenu.Items.Add(new MenuItem
            {
                Header = "（暂无收藏 — 拖动网页标签/地址到宠物即可收藏）",
                IsEnabled = false,
            });
            return;
        }

        foreach (var bm in bookmarks)
        {
            var item = new MenuItem { Header = $"🔖 {bm.Title}" };
            ToolTip.SetTip(item, bm.Url);
            item.Click += (_, _) => { BookmarkService.Open(bm); _menu.Close(); };
            _bookmarkMenu.Items.Add(item);
        }

        _bookmarkMenu.Items.Add(new Separator());
        var clear = new MenuItem { Header = "清空收藏夹" };
        clear.Click += (_, _) =>
        {
            foreach (var bm in bookmarks.ToList())
                _bookmarkService.Remove(bm.Url);
            _menu.Close();
        };
        _bookmarkMenu.Items.Add(clear);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
