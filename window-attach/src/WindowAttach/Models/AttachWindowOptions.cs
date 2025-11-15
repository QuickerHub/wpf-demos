using System;

namespace WindowAttach.Models
{
    /// <summary>
    /// Options for attaching windows
    /// </summary>
    public class AttachWindowOptions
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
        /// Whether to automatically adjust position to maximize visible area when window is not fully visible (default: false)
        /// </summary>
        public bool AutoAdjustToScreen { get; set; } = false;

        /// <summary>
        /// If true, toggle behavior (attach if not attached, detach if attached). If false, only register (no toggle) (default: false)
        /// </summary>
        public bool AutoUnregister { get; set; } = false;

        /// <summary>
        /// Callback action to execute when window1 is closed (default: null)
        /// </summary>
        public Action? CallbackAction { get; set; } = null;

        /// <summary>
        /// Whether to create popup window for detaching (default: true)
        /// </summary>
        public bool CreatePopup { get; set; } = true;
    }
}

