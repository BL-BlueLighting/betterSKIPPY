using System.Text.Json;
using SKIPPY.Models;

namespace SKIPPY.Services;

/// <summary>
/// bookmark managing
/// </summary>
public class BookmarkService
{
    private const string FileName = "bookmarks.json";
    private readonly string _filePath;
    private List<BookmarkInfo> _bookmarks = [];

    public IReadOnlyList<BookmarkInfo> Bookmarks => _bookmarks;

    public event Action? BookmarksChanged;

    public BookmarkService()
    {
        _filePath = GetStoragePath();
        Load();
    }

    /// <summary>
    /// new bookmark.
    /// </summary>
    public bool Add(string title, string url)
    {
        url = url.Trim();
        if (string.IsNullOrWhiteSpace(url)) return false;

        // Normalize
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        if (_bookmarks.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            return false;

        string displayTitle = string.IsNullOrWhiteSpace(title)
            ? ExtractTitleFromUrl(url)
            : title.Trim();

        _bookmarks.Add(new BookmarkInfo
        {
            Title = displayTitle,
            Url = url,
            AddedAt = DateTime.UtcNow,
        });

        Save();
        BookmarksChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Remove a bookmark
    /// </summary>
    public bool Remove(string url)
    {
        var item = _bookmarks.FirstOrDefault(b =>
            b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (item == null) return false;

        _bookmarks.Remove(item);
        Save();
        BookmarksChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Open a bookmark
    /// </summary>
    public static void Open(BookmarkInfo bookmark)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = bookmark.Url,
                UseShellExecute = true,
            });
        }
        catch
        {
            // there is nothing to do
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                _bookmarks = JsonSerializer.Deserialize<List<BookmarkInfo>>(json) ?? [];
            }
        }
        catch
        {
            _bookmarks = [];
        }
    }

    private void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(_bookmarks, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // bookmark not important so ignore.
        }
    }

    private static string ExtractTitleFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            string host = uri.Host;

            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                host = host[4..];

            if (host.Contains("wikidot"))
            {
                string[] parts = host.Split('.');
                if (parts.Length >= 2 && parts[0] != "scp-wiki" && parts[0] != "scp-wiki-cn")
                    return "SCP - " + parts[0];
                return "SCP Wiki";
            }

            string path = uri.AbsolutePath.Trim('/');
            if (!string.IsNullOrEmpty(path))
            {
                string lastSegment = path.Split('/').Last();
                lastSegment = Uri.UnescapeDataString(lastSegment);
                if (lastSegment.Length > 40)
                    lastSegment = lastSegment[..40] + "...";
                if (!string.IsNullOrEmpty(lastSegment))
                    return lastSegment;
            }

            return host;
        }
        catch
        {
            return url.Length > 40 ? url[..40] + "..." : url;
        }
    }

    private static string GetStoragePath()
    {
        string baseDir;

        if (OperatingSystem.IsWindows())
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support");
        }
        else // *nix
        {
            string? xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
                baseDir = xdgConfig;
            else
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config");
        }

        return Path.Combine(baseDir, "SKIPPY", FileName);
    }
}
