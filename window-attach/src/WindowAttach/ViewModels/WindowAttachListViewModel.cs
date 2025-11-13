using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private readonly ObservableCollection<WindowAttachItemViewModel> _attachmentsCollection = new ObservableCollection<WindowAttachItemViewModel>();

        public ReadOnlyObservableCollection<WindowAttachItemViewModel> Attachments { get; }

        [ObservableProperty]
        public partial bool HasAttachments { get; set; }

        public WindowAttachListViewModel()
        {
            // Get the manager service from AppState
            _managerService = AppState.ManagerService;
            
            // Initialize read-only collection
            Attachments = new ReadOnlyObservableCollection<WindowAttachItemViewModel>(_attachmentsCollection);
            
            // Load initial data
            Refresh();
        }

        [RelayCommand]
        private void Refresh()
        {
            // Clear existing items
            foreach (var item in _attachmentsCollection)
            {
                item.Detached -= OnItemDetached;
            }
            _attachmentsCollection.Clear();
            
            // Get all Main attachments, sorted by registered time (newest first)
            var mainPairs = _managerService.GetMainPairs()
                .OrderByDescending(pair => pair.RegisteredTime)
                .ToList();
            
            // Create view models for each pair
            foreach (var pair in mainPairs)
            {
                var item = new WindowAttachItemViewModel(pair.Window1Handle, pair.Window2Handle, pair.RegisteredTime);
                item.Detached += OnItemDetached;
                _attachmentsCollection.Add(item);
            }
            
            // Update HasAttachments
            HasAttachments = _attachmentsCollection.Count > 0;
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

