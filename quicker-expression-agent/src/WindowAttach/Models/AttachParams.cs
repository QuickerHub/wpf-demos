namespace WindowAttach.Models
{
    /// <summary>
    /// Parameters for window attachment
    /// </summary>
    public class AttachParams
    {
        /// <summary>
        /// Placement position (default: RightTop)
        /// </summary>
        public WindowPlacement Placement { get; set; } = WindowPlacement.RightTop;

        /// <summary>
        /// Horizontal offset (default: 0)
        /// </summary>
        public double OffsetX { get; set; } = 0;

        /// <summary>
        /// Vertical offset (default: 0)
        /// </summary>
        public double OffsetY { get; set; } = 0;

        /// <summary>
        /// Whether to restrict window2 to the same screen as window1 (default: false)
        /// </summary>
        public bool RestrictToSameScreen { get; set; } = false;

        /// <summary>
        /// Whether to automatically optimize position by trying all placement options and selecting the one with maximum visible area (default: false)
        /// When enabled, the system will try all placement positions and choose the best one that maximizes visible area, excluding overlap with window1
        /// </summary>
        public bool AutoOptimizePosition { get; set; } = false;

        /// <summary>
        /// Callback action to execute when window1 is closed (default: null)
        /// </summary>
        public Action? CallbackAction { get; set; } = null;

        /// <summary>
        /// Whether to prevent window2 from being activated when clicked (default: false)
        /// When true, sets WS_EX_NOACTIVATE style on window2
        /// </summary>
        public bool PreventActivation { get; set; } = false;
    }
}

