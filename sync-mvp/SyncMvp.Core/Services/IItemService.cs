using SyncMvp.Core.Models;

namespace SyncMvp.Core.Services;

/// <summary>
/// Service for managing common items
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Get all items
    /// </summary>
    Task<List<CommonItem>> GetAllItemsAsync();

    /// <summary>
    /// Get item by ID
    /// </summary>
    Task<CommonItem?> GetItemByIdAsync(string id);

    /// <summary>
    /// Add a new item
    /// </summary>
    Task<CommonItem> AddItemAsync(string text);

    /// <summary>
    /// Update an existing item
    /// </summary>
    Task<CommonItem> UpdateItemAsync(string id, string text);

    /// <summary>
    /// Delete an item
    /// </summary>
    Task DeleteItemAsync(string id);

    /// <summary>
    /// Apply sync change (without logging change record)
    /// </summary>
    Task ApplySyncChangeAsync(CommonItem item);

    /// <summary>
    /// Apply sync delete (without logging change record)
    /// </summary>
    Task ApplySyncDeleteAsync(string itemId);

    /// <summary>
    /// Event fired when items change
    /// </summary>
    event EventHandler<List<CommonItem>>? ItemsChanged;
}
