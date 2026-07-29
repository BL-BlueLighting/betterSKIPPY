using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SKIPPY.Services;

namespace SKIPPY.Menu;

/// <summary>
/// build menu
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

    private static readonly (string Header, string Code)[] CodeSnippets =
    [
        ("评分模块",
            "[[>]]\n[[module Rate]]\n[[/>]]"),
        ("基本格式",
            "[[>]]\n[[module Rate]]\n[[/>]]\n\n" +
            "**项目编号：**SCP-CN-XXXX\n" +
            "**项目等级：**Safe/Euclid/Keter（表明分级）\n\n" +
            "**特殊收容措施：** [说明收容措施的段落]\n\n" +
            "**描述：** [描述SCP的段落]\n\n" +
            "**附录：** [可选的附加段落]\n\n" +
            "[[footnote]]\n[[/footnote]]\n\n" +
            "[[div class=\"footer-wikiwalk-nav\"]]\n[[=]]\n" +
            "<< [[[SCP-CN-XXXW]]] | SCP-CN-XXXX | [[[SCP-CN-XXXY]]] >>\n" +
            "[[/=]]\n[[/div]]"),
        ("引用块",
            "[[div class=\"blockquote\"]]\n[[/div]]"),
        ("折叠",
            "[[collapsible show=\"+ 点我打开\" hide=\"- 点我隐藏\"]]\n[[/collapsible]]"),
        ("图片框",
            "[[include component:image-block\n| name=\n| caption=\n]]"),
    ];

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
        BookmarkService bookmarkService)
    {
        _codeMenu = codeMenu;
        _portalMenu = portalMenu;
        _deletedMenu = deletedMenu;
        _skinMenu = skinMenu;
        _bookmarkMenu = bookmarkMenu;
        _menu = menu;
        _onSkinSelected = onSkinSelected;
        _bookmarkService = bookmarkService;

        // Rebuild bookmarks when they change
        _bookmarkService.BookmarksChanged += BuildBookmarkMenu;
    }

    /// <summary>
    /// build all menus
    /// </summary>
    public void BuildAll()
    {
        BuildCodeMenu();
        BuildPortalMenu();
        BuildDeletedMenu();
        BuildBookmarkMenu();
        BuildSkinMenu();
    }

    private void BuildCodeMenu()
    {
        _codeMenu.Items.Clear();

        foreach (var (header, code) in CodeSnippets)
        {
            var item = new MenuItem { Header = header };
            item.Click += async (_, _) =>
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(_menu);
                    if (topLevel?.Clipboard != null)
                        await topLevel.Clipboard.SetTextAsync(code);
                }
                catch { /* ignore clipboard errors */ }
                BubbleService.ShowToast("已复制");
                _menu.Close();
            };
            _codeMenu.Items.Add(item);
        }
    }

    private void BuildPortalMenu()
    {
        _portalMenu.Items.Clear();

        foreach (var (name, url) in PortalSites)
        {
            var item = new MenuItem { Header = name };
            item.Click += (_, _) =>
            {
                OpenUrl(url);
                _menu.Close();
            };
            _portalMenu.Items.Add(item);
        }
    }

    private void BuildDeletedMenu()
    {
        _deletedMenu.Items.Clear();

        foreach (var (name, url) in DeletedSites)
        {
            var item = new MenuItem { Header = name };
            item.Click += (_, _) =>
            {
                OpenUrl(url);
                _menu.Close();
            };
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

    /// <summary>
    /// build bookmark menu
    /// </summary>
    private void BuildBookmarkMenu()
    {
        _bookmarkMenu.Items.Clear();

        var bookmarks = _bookmarkService.Bookmarks;

        if (bookmarks.Count == 0)
        {
            var emptyItem = new MenuItem
            {
                Header = "（暂无收藏 — 拖动网页标签/地址到宠物即可收藏）",
                IsEnabled = false,
            };
            _bookmarkMenu.Items.Add(emptyItem);
            return;
        }

        foreach (var bm in bookmarks)
        {
            var item = new MenuItem { Header = $"🔖 {bm.Title}" };

            // Add tooltip with full URL
            ToolTip.SetTip(item, bm.Url);

            item.Click += (_, _) =>
            {
                BookmarkService.Open(bm);
                _menu.Close();
            };

            _bookmarkMenu.Items.Add(item);
        }

        _bookmarkMenu.Items.Add(new Separator());

        var clearItem = new MenuItem { Header = "清空收藏夹" };
        clearItem.Click += (_, _) =>
        {
            foreach (var bm in bookmarks.ToList())
                _bookmarkService.Remove(bm.Url);
            _menu.Close();
        };
        _bookmarkMenu.Items.Add(clearItem);
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
        catch
        {
            // Ignore launch failures
        }
    }
}
