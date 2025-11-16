namespace WindowEdgeHide.Models
{
    /// <summary>
    /// Edge direction for window edge hiding
    /// </summary>
    public enum EdgeDirection
    {
        /// <summary>
        /// Left edge
        /// </summary>
        Left,

        /// <summary>
        /// Top edge
        /// </summary>
        Top,

        /// <summary>
        /// Right edge
        /// </summary>
        Right,

        /// <summary>
        /// Bottom edge
        /// </summary>
        Bottom,

        /// <summary>
        /// Automatically select the nearest edge based on current window position
        /// This option is processed during registration
        /// </summary>
        Nearest,

        /// <summary>
        /// Register edge hiding without automatically moving window to edge
        /// Window will only hide when manually moved to screen edge
        /// </summary>
        None
    }
}

