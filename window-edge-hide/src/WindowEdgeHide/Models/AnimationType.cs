namespace WindowEdgeHide.Models
{
    /// <summary>
    /// Animation type for window movement
    /// </summary>
    public enum AnimationType
    {
        /// <summary>
        /// No animation, direct movement
        /// </summary>
        None,

        /// <summary>
        /// Linear animation
        /// </summary>
        Linear,

        /// <summary>
        /// Ease-in-out cubic animation (smooth acceleration and deceleration)
        /// </summary>
        EaseInOut
    }
}

