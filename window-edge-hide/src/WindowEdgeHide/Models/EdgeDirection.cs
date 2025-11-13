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
        Nearest
    }
}

