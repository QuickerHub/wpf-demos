using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quicker.Common;
using QuickerActionPanel.Models;
using QuickerActionPanel.Views;
using System.Collections.ObjectModel;

namespace QuickerActionPanel.ViewModels
{
    /// <summary>
    /// ViewModel for the action item list
    /// </summary>
    public partial class ActionItemListViewModel : ObservableObject
    {
        private readonly ObservableCollection<ActionItemViewModel> _actionItems = new();
        private readonly ActionDropHandler _dropHandler;

        [ObservableProperty]
        private LayoutMode _layoutMode = LayoutMode.IconAndText;

        public ActionItemListViewModel()
        {
            _dropHandler = new ActionDropHandler();
            _dropHandler.ActionDropped += DropHandler_ActionDropped;
        }

        /// <summary>
        /// Gets the drop handler for drag and drop operations
        /// </summary>
        public ActionDropHandler DropHandler => _dropHandler;

        /// <summary>
        /// Gets the collection of action items
        /// </summary>
        public ObservableCollection<ActionItemViewModel> ActionItems => _actionItems;

        /// <summary>
        /// Adds an action item to the list
        /// </summary>
        public void AddActionItem(ActionItem actionItem)
        {
            if (actionItem == null)
                return;

            var commonItem = new ActionCommonItem
            {
                ActionId = actionItem.Id ?? string.Empty,
                ActionParam = string.Empty,
                Icon = actionItem.Icon ?? string.Empty,
                Title = actionItem.Title ?? string.Empty
            };

            var viewModel = new ActionItemViewModel(commonItem);
            _actionItems.Add(viewModel);
        }

        private void DropHandler_ActionDropped(ActionItem actionItem)
        {
            AddActionItem(actionItem);
        }

        /// <summary>
        /// Removes an action item from the list
        /// </summary>
        [RelayCommand]
        public void RemoveActionItem(ActionItemViewModel? item)
        {
            if (item != null && _actionItems.Contains(item))
            {
                _actionItems.Remove(item);
            }
        }

        /// <summary>
        /// Clears all action items
        /// </summary>
        [RelayCommand]
        public void ClearAll()
        {
            _actionItems.Clear();
        }
    }
}
