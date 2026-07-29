namespace SKIPPY.Models;

/// <summary>
/// Represents a saved bookmark (page favorite).
/// </summary>
public class BookmarkInfo
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
