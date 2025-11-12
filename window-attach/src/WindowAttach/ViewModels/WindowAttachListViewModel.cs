using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using WindowAttach.Models;
using WindowAttach.Services;
using WindowAttach;

namespace WindowAttach.ViewModels
{
    /// <summary>
    /// ViewModel for the window attachment list window
    /// </summary>
    public partial class WindowAttachListViewModel : ObservableObject
    {
        private readonly WindowAttachManagerService _managerService;
        private ReadOnlyObservableCollection<WindowAttachItemViewModel>? _attachments;

        public ReadOnlyObservableCollection<WindowAttachItemViewModel> Attachments => 
            _attachments ?? throw new InvalidOperationException("Attachments not initialized");

        [ObservableProperty]
        public partial bool HasAttachments { get; set; }

        public WindowAttachListViewModel()
        {
            // Get the manager service from AppState
            _managerService = AppState.ManagerService;
            
            // Connect to the cache with filtering (only show Main attachments, exclude Popup)
            // and sorting by registered time (newest first)
            _managerService.PairsCache.Connect()
                .Filter(pair => pair.AttachType == AttachType.Main) // Filter out popup attachments
                .Transform(pair => 
                {
                    var item = new WindowAttachItemViewModel(pair.Window1Handle, pair.Window2Handle, pair.RegisteredTime);
                    item.Detached += OnItemDetached;
                    return item;
                })
                .SortAndBind(
                    out var attachments,
                    SortExpressionComparer<WindowAttachItemViewModel>.Descending(x => x.RegisteredTime)) // Sort by registered time, newest first
                .Subscribe(_ => 
                {
                    // Update HasAttachments when collection changes
                    HasAttachments = attachments.Count > 0;
                });

            _attachments = attachments;
            HasAttachments = attachments.Count > 0;
        }

        [RelayCommand]
        private void Refresh()
        {
            // Refresh is handled automatically by the cache connection
            // This command can be used to force a refresh if needed
        }

        [RelayCommand]
        private void CloseWindow()
        {
            // This will be handled by the view
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnItemDetached(object? sender, EventArgs e)
        {
            if (sender is WindowAttachItemViewModel item)
            {
                Runner.Unregister(item.Window1Handle, item.Window2Handle);
            }
        }

        public event EventHandler? CloseRequested;
    }
}

