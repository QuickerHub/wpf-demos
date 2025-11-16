using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerActionPanel.Models
{
    /// <summary>
    /// Common item for storing action information with parameters
    /// </summary>
    public partial class ActionCommonItem : ObservableObject
    {
        /// <summary>
        /// Gets or sets the action ID
        /// </summary>
        [ObservableProperty]
        public partial string ActionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the action parameters for running the action
        /// </summary>
        [ObservableProperty]
        public partial string ActionParam { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stored icon (fallback when ActionItem is not available)
        /// </summary>
        [ObservableProperty]
        public partial string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stored title (fallback when ActionItem is not available)
        /// </summary>
        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;
    }
}
