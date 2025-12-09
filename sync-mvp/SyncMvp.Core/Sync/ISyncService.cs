namespace SyncMvp.Core.Sync;

/// <summary>
/// Service for synchronizing data between clients
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Sync local changes to remote
    /// </summary>
    Task SyncToRemoteAsync();

    /// <summary>
    /// Sync remote changes to local
    /// </summary>
    Task SyncFromRemoteAsync();

    /// <summary>
    /// Full sync (both directions)
    /// </summary>
    Task FullSyncAsync();

    /// <summary>
    /// Event fired when sync completes
    /// </summary>
    event EventHandler<SyncResult>? SyncCompleted;
}

/// <summary>
/// Result of synchronization operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ChangesUploaded { get; set; }
    public int ChangesDownloaded { get; set; }
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
}
