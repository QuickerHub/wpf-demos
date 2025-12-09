using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using SyncMvp.Core.Models;
using SyncMvp.Core.Services;
using System.Text.Json;

namespace SyncMvp.Core.Sync;

/// <summary>
/// WebDAV-based synchronization service
/// </summary>
public class WebDavSyncService : ISyncService
{
    private readonly IChangeLogService _changeLogService;
    private readonly IItemService _itemService;
    private readonly ILogger<WebDavSyncService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _webDavUrl;
    private readonly string _clientId;

    public event EventHandler<SyncResult>? SyncCompleted;

    public WebDavSyncService(
        IChangeLogService changeLogService,
        IItemService itemService,
        ILogger<WebDavSyncService> logger,
        HttpClient httpClient,
        string webDavUrl)
    {
        _changeLogService = changeLogService;
        _itemService = itemService;
        _logger = logger;
        _httpClient = httpClient;
        _webDavUrl = webDavUrl.TrimEnd('/');
        _clientId = Environment.MachineName + "_" + Environment.UserName;
    }

    public async Task SyncToRemoteAsync()
    {
        try
        {
            var unsyncedChanges = await _changeLogService.GetUnsyncedChangesAsync();
            if (unsyncedChanges.Count == 0)
            {
                _logger.LogInformation("No local changes to sync");
                return;
            }

            // Upload changes to WebDAV
            var changeLogPath = $"{_webDavUrl}/changes/{_clientId}/{DateTime.UtcNow:yyyyMMddHHmmss}_changes.json";
            var json = JsonSerializer.Serialize(unsyncedChanges, new JsonSerializerOptions { WriteIndented = true });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(changeLogPath, content);
            response.EnsureSuccessStatusCode();

            // Mark as synced
            var changeIds = unsyncedChanges.Select(x => x.Id).ToList();
            await _changeLogService.MarkAsSyncedAsync(changeIds);

            _logger.LogInformation("Synced {Count} changes to remote", unsyncedChanges.Count);

            SyncCompleted?.Invoke(this, new SyncResult
            {
                Success = true,
                ChangesUploaded = unsyncedChanges.Count,
                SyncTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync to remote");
            SyncCompleted?.Invoke(this, new SyncResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SyncTime = DateTime.UtcNow
            });
            throw;
        }
    }

    public async Task SyncFromRemoteAsync()
    {
        try
        {
            // List all change log files from other clients
            var changesPath = $"{_webDavUrl}/changes/";
            var clients = await ListClientsAsync(changesPath);

            var allChanges = new List<ChangeRecord>();
            foreach (var clientId in clients.Where(c => c != _clientId))
            {
                var clientChangesPath = $"{changesPath}{clientId}/";
                var changeFiles = await ListChangeFilesAsync(clientChangesPath);

                foreach (var file in changeFiles)
                {
                    var filePath = $"{clientChangesPath}{file}";
                    var changes = await DownloadChangesAsync(filePath);
                    allChanges.AddRange(changes);
                }
            }

            if (allChanges.Count == 0)
            {
                _logger.LogInformation("No remote changes to sync");
                return;
            }

            // Apply changes in order
            var sortedChanges = allChanges.OrderBy(x => x.Timestamp).ThenBy(x => x.SequenceNumber).ToList();
            await ApplyChangesAsync(sortedChanges);

            _logger.LogInformation("Synced {Count} changes from remote", allChanges.Count);

            SyncCompleted?.Invoke(this, new SyncResult
            {
                Success = true,
                ChangesDownloaded = allChanges.Count,
                SyncTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync from remote");
            SyncCompleted?.Invoke(this, new SyncResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SyncTime = DateTime.UtcNow
            });
            throw;
        }
    }

    public async Task FullSyncAsync()
    {
        await SyncToRemoteAsync();
        await SyncFromRemoteAsync();
    }

    private async Task<List<string>> ListClientsAsync(string path)
    {
        var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), path);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();
        
        var clients = new List<string>();
        foreach (var item in items)
        {
            if (item.TryGetProperty("name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name))
                    clients.Add(name);
            }
        }
        
        return clients;
    }

    private async Task<List<string>> ListChangeFilesAsync(string path)
    {
        var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), path);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();
        
        var files = new List<string>();
        foreach (var item in items)
        {
            if (item.TryGetProperty("name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name) && name.EndsWith(".json"))
                    files.Add(name);
            }
        }
        
        return files;
    }

    private async Task<List<ChangeRecord>> DownloadChangesAsync(string filePath)
    {
        var response = await _httpClient.GetAsync(filePath);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var changes = JsonSerializer.Deserialize<List<ChangeRecord>>(json) ?? new List<ChangeRecord>();

        return changes;
    }

    private async Task ApplyChangesAsync(List<ChangeRecord> changes)
    {
        foreach (var change in changes)
        {
            try
            {
                switch (change.Operation)
                {
                    case "INSERT":
                    case "UPDATE":
                        var item = JsonSerializer.Deserialize<CommonItem>(change.EntityData);
                        if (item != null)
                        {
                            // Use sync method to avoid creating new change records
                            await _itemService.ApplySyncChangeAsync(item);
                        }
                        break;

                    case "DELETE":
                        // Use sync method to avoid creating new change records
                        await _itemService.ApplySyncDeleteAsync(change.EntityId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply change {ChangeId}", change.Id);
            }
        }
    }
}
