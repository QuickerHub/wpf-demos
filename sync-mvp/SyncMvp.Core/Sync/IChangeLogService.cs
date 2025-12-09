namespace SyncMvp.Core.Sync;

/// <summary>
/// Service for managing change logs
/// </summary>
public interface IChangeLogService
{
    /// <summary>
    /// Log a change
    /// </summary>
    Task LogChangeAsync(ChangeRecord change);

    /// <summary>
    /// Get all unsynced changes
    /// </summary>
    Task<List<ChangeRecord>> GetUnsyncedChangesAsync();

    /// <summary>
    /// Mark changes as synced
    /// </summary>
    Task MarkAsSyncedAsync(List<string> changeIds);

    /// <summary>
    /// Get all changes since a specific timestamp
    /// </summary>
    Task<List<ChangeRecord>> GetChangesSinceAsync(DateTime timestamp);
}
