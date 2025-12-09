namespace SyncMvp.Core.Sync;

/// <summary>
/// Change record for synchronization
/// </summary>
public class ChangeRecord
{
    /// <summary>
    /// Unique identifier for this change
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when change occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation type: INSERT, UPDATE, DELETE
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Entity data (JSON serialized)
    /// </summary>
    public string EntityData { get; set; } = string.Empty;

    /// <summary>
    /// Client ID that made this change
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number for ordering changes
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Whether this change has been synced
    /// </summary>
    public bool IsSynced { get; set; } = false;
}
