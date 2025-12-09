using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyncMvp.Core.Models;
using SyncMvp.Core.Services;
using SyncMvp.Core.Sync;
using System.Collections.ObjectModel;

namespace SyncMvp.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IItemService _itemService;
    private readonly ISyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<ItemViewModel> items = new();

    [ObservableProperty]
    private string newItemText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isSyncing = false;

    public MainWindowViewModel(IItemService itemService, ISyncService syncService)
    {
        _itemService = itemService;
        _syncService = syncService;

        // Subscribe to sync events
        _syncService.SyncCompleted += OnSyncCompleted;

        // Subscribe to item changes
        _itemService.ItemsChanged += OnItemsChanged;

        // Load initial data
        LoadItemsAsync();

        // Auto-sync every 30 seconds
        var timer = new System.Timers.Timer(30000);
        timer.Elapsed += async (s, e) => await SyncAsync();
        timer.Start();
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemText))
            return;

        await _itemService.AddItemAsync(NewItemText);
        NewItemText = string.Empty;
    }

    [RelayCommand]
    private async Task UpdateItemAsync(ItemViewModel item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Text))
            return;

        await _itemService.UpdateItemAsync(item.Id, item.Text);
    }

    // Public method for ItemViewModel to call
    internal async Task UpdateItem(ItemViewModel item)
    {
        await UpdateItemAsync(item);
    }

    [RelayCommand]
    private async Task DeleteItemAsync(ItemViewModel item)
    {
        if (item == null)
            return;

        await _itemService.DeleteItemAsync(item.Id);
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        if (IsSyncing)
            return;

        IsSyncing = true;
        StatusMessage = "Syncing...";

        try
        {
            await _syncService.FullSyncAsync();
            StatusMessage = "Sync completed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async void LoadItemsAsync()
    {
        var items = await _itemService.GetAllItemsAsync();
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(new ItemViewModel(item, this));
        }
    }

    private void OnItemsChanged(object? sender, List<CommonItem> items)
    {
        // Update UI on UI thread
        App.Current.Dispatcher.Invoke(() =>
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new ItemViewModel(item, this));
            }
        });
    }

    private void OnSyncCompleted(object? sender, SyncResult result)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (result.Success)
            {
                StatusMessage = $"Synced: {result.ChangesUploaded} uploaded, {result.ChangesDownloaded} downloaded";
            }
            else
            {
                StatusMessage = $"Sync error: {result.ErrorMessage}";
            }
        });
    }
}

public partial class ItemViewModel : ObservableObject
{
    private readonly MainWindowViewModel _parent;
    private System.Threading.Timer? _debounceTimer;

    [ObservableProperty]
    private string id;

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private DateTime updatedAt;

    public ItemViewModel(CommonItem item, MainWindowViewModel parent)
    {
        _parent = parent;
        Id = item.Id;
        Text = item.Text;
        UpdatedAt = item.UpdatedAt;
    }

    partial void OnTextChanged(string value)
    {
        // Auto-save on text change (debounced)
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(async _ =>
        {
            await _parent.UpdateItem(this);
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }, null, 1000, Timeout.Infinite);
    }
}
