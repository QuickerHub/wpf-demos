namespace WindowEdgeHide.Models
{
    /// <summary>
    /// Window activation strategy when showing from edge hide
    /// </summary>
    public enum ActivationStrategy
    {
        /// <summary>
        /// Auto-activate window when showing. For noactive windows, automatically fallback to Topmost
        /// </summary>
        AutoActivate = 0,

        /// <summary>
        /// Set window to topmost without activation
        /// </summary>
        Topmost = 1,

        /// <summary>
        /// Do nothing (no activation, no topmost)
        /// </summary>
        None = 2
    }
}

