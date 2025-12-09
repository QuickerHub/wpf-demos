using FreeSql;
using Microsoft.Extensions.Logging;

namespace SyncMvp.Core.Sync;

/// <summary>
/// Implementation of change log service using SQLite
/// </summary>
public class ChangeLogService : IChangeLogService
{
    private readonly IFreeSql _db;
    private readonly ILogger<ChangeLogService> _logger;

    public ChangeLogService(IFreeSql db, ILogger<ChangeLogService> logger)
    {
        _db = db;
        _logger = logger;

        // Ensure table exists
        _db.CodeFirst.SyncStructure<ChangeRecord>();
    }

    public async Task LogChangeAsync(ChangeRecord change)
    {
        // Get next sequence number
        var maxSeq = await _db.Select<ChangeRecord>()
            .MaxAsync(x => (long?)x.SequenceNumber) ?? 0;

        change.SequenceNumber = maxSeq + 1;
        change.IsSynced = false;

        await _db.Insert(change).ExecuteAffrowsAsync();
        _logger.LogDebug("Logged change: {Operation} for entity {EntityId}", change.Operation, change.EntityId);
    }

    public async Task<List<ChangeRecord>> GetUnsyncedChangesAsync()
    {
        return await _db.Select<ChangeRecord>()
            .Where(x => !x.IsSynced)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync();
    }

    public async Task MarkAsSyncedAsync(List<string> changeIds)
    {
        if (changeIds.Count == 0) return;

        await _db.Update<ChangeRecord>()
            .Set(x => x.IsSynced, true)
            .Where(x => changeIds.Contains(x.Id))
            .ExecuteAffrowsAsync();

        _logger.LogDebug("Marked {Count} changes as synced", changeIds.Count);
    }

    public async Task<List<ChangeRecord>> GetChangesSinceAsync(DateTime timestamp)
    {
        return await _db.Select<ChangeRecord>()
            .Where(x => x.Timestamp >= timestamp)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync();
    }
}
