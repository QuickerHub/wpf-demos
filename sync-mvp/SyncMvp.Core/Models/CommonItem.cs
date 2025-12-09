namespace SyncMvp.Core.Models;

/// <summary>
/// Common item model for MVP testing
/// </summary>
public class CommonItem
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Text content
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Client ID that created/updated this item
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Version number for conflict resolution
    /// </summary>
    public long Version { get; set; } = 1;
}
