using CommunityToolkit.Mvvm.ComponentModel;
using Quicker.Common;
using QuickerActionPanel.Models;
using QuickerActionPanel.Utils;

namespace QuickerActionPanel.ViewModels
{
    /// <summary>
    /// ViewModel for a single action item based on ActionCommonItem
    /// </summary>
    public partial class ActionItemViewModel : ObservableObject
    {
        private readonly ActionCommonItem _actionCommonItem;
        private readonly ActionItem? _actionItem;

        public ActionItemViewModel(ActionCommonItem actionCommonItem)
        {
            _actionCommonItem = actionCommonItem ?? throw new System.ArgumentNullException(nameof(actionCommonItem));
            _actionItem = QuickerUtil.GetActionById(_actionCommonItem.ActionId);
        }

        /// <summary>
        /// Gets the underlying action common item
        /// </summary>
        public ActionCommonItem ActionCommonItem => _actionCommonItem;

        /// <summary>
        /// Gets the action item for display purposes (immutable, loaded from ActionId)
        /// </summary>
        public ActionItem? ActionItem => _actionItem;

        /// <summary>
        /// Gets the action ID
        /// </summary>
        public string ActionId => _actionCommonItem.ActionId;

        /// <summary>
        /// Gets or sets the action parameters
        /// </summary>
        public string ActionParam
        {
            get => _actionCommonItem.ActionParam;
            set => _actionCommonItem.ActionParam = value;
        }

        /// <summary>
        /// Gets the action title (prioritizes ActionItem, falls back to ActionCommonItem)
        /// </summary>
        public string Title => _actionItem?.Title ?? _actionCommonItem.Title ?? string.Empty;

        /// <summary>
        /// Gets the action description
        /// </summary>
        public string Description => _actionItem?.Description ?? string.Empty;

        /// <summary>
        /// Gets the action icon (prioritizes ActionItem, falls back to ActionCommonItem)
        /// </summary>
        public string Icon => _actionItem?.Icon ?? _actionCommonItem.Icon ?? string.Empty;
    }
}
