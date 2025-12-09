using FreeSql;
using Microsoft.Extensions.Logging;
using SyncMvp.Core.Models;
using SyncMvp.Core.Sync;

namespace SyncMvp.Core.Services;

/// <summary>
/// Implementation of item service with change tracking
/// </summary>
public class ItemService : IItemService
{
    private readonly IFreeSql _db;
    private readonly ILogger<ItemService> _logger;
    private readonly IChangeLogService _changeLogService;
    private readonly string _clientId;

    public event EventHandler<List<CommonItem>>? ItemsChanged;

    public ItemService(
        IFreeSql db,
        ILogger<ItemService> logger,
        IChangeLogService changeLogService)
    {
        _db = db;
        _logger = logger;
        _changeLogService = changeLogService;
        _clientId = Environment.MachineName + "_" + Environment.UserName;

        // Ensure table exists
        _db.CodeFirst.SyncStructure<CommonItem>();
    }

    public async Task<List<CommonItem>> GetAllItemsAsync()
    {
        var items = await _db.Select<CommonItem>()
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return items;
    }

    public async Task<CommonItem?> GetItemByIdAsync(string id)
    {
        return await _db.Select<CommonItem>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<CommonItem> AddItemAsync(string text)
    {
        var item = new CommonItem
        {
            Id = Guid.NewGuid().ToString(),
            Text = text,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ClientId = _clientId,
            Version = 1
        };

        await _db.Insert(item).ExecuteAffrowsAsync();

        // Log change
        await _changeLogService.LogChangeAsync(new ChangeRecord
        {
            Operation = "INSERT",
            EntityId = item.Id,
            EntityData = System.Text.Json.JsonSerializer.Serialize(item),
            ClientId = _clientId,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Added item: {Id}, Text: {Text}", item.Id, item.Text);
        NotifyItemsChanged();

        return item;
    }

    public async Task<CommonItem> UpdateItemAsync(string id, string text)
    {
        var item = await GetItemByIdAsync(id);
        if (item == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found");
        }

        item.Text = text;
        item.UpdatedAt = DateTime.UtcNow;
        item.Version++;
        item.ClientId = _clientId;

        await _db.Update<CommonItem>()
            .Set(x => x.Text, text)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.Version, item.Version)
            .Set(x => x.ClientId, _clientId)
            .Where(x => x.Id == id)
            .ExecuteAffrowsAsync();

        // Log change
        await _changeLogService.LogChangeAsync(new ChangeRecord
        {
            Operation = "UPDATE",
            EntityId = item.Id,
            EntityData = System.Text.Json.JsonSerializer.Serialize(item),
            ClientId = _clientId,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Updated item: {Id}, Text: {Text}", item.Id, item.Text);
        NotifyItemsChanged();

        return item;
    }

    public async Task DeleteItemAsync(string id)
    {
        var item = await GetItemByIdAsync(id);
        if (item == null)
        {
            return;
        }

        await _db.Delete<CommonItem>()
            .Where(x => x.Id == id)
            .ExecuteAffrowsAsync();

        // Log change
        await _changeLogService.LogChangeAsync(new ChangeRecord
        {
            Operation = "DELETE",
            EntityId = id,
            EntityData = string.Empty,
            ClientId = _clientId,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Deleted item: {Id}", id);
        NotifyItemsChanged();
    }

    public async Task ApplySyncChangeAsync(CommonItem item)
    {
        var existing = await GetItemByIdAsync(item.Id);
        if (existing == null)
        {
            // Insert without logging change
            await _db.Insert(item).ExecuteAffrowsAsync();
        }
        else if (item.Version > existing.Version)
        {
            // Update without logging change
            await _db.Update<CommonItem>()
                .Set(x => x.Text, item.Text)
                .Set(x => x.UpdatedAt, item.UpdatedAt)
                .Set(x => x.Version, item.Version)
                .Set(x => x.ClientId, item.ClientId)
                .Where(x => x.Id == item.Id)
                .ExecuteAffrowsAsync();
        }
        
        NotifyItemsChanged();
    }

    public async Task ApplySyncDeleteAsync(string itemId)
    {
        await _db.Delete<CommonItem>()
            .Where(x => x.Id == itemId)
            .ExecuteAffrowsAsync();
        
        NotifyItemsChanged();
    }

    private async void NotifyItemsChanged()
    {
        try
        {
            var items = await GetAllItemsAsync();
            ItemsChanged?.Invoke(this, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NotifyItemsChanged");
        }
    }
}
