namespace WindowAttach.Models
{
    /// <summary>
    /// Window placement options relative to the target window
    /// </summary>
    public enum WindowPlacement
    {
        /// <summary>
        /// Left top corner
        /// </summary>
        LeftTop,
        
        /// <summary>
        /// Left center
        /// </summary>
        LeftCenter,
        
        /// <summary>
        /// Left bottom corner
        /// </summary>
        LeftBottom,
        
        /// <summary>
        /// Top left corner
        /// </summary>
        TopLeft,
        
        /// <summary>
        /// Top center
        /// </summary>
        TopCenter,
        
        /// <summary>
        /// Top right corner
        /// </summary>
        TopRight,
        
        /// <summary>
        /// Right top corner
        /// </summary>
        RightTop,
        
        /// <summary>
        /// Right center
        /// </summary>
        RightCenter,
        
        /// <summary>
        /// Right bottom corner
        /// </summary>
        RightBottom,
        
        /// <summary>
        /// Bottom left corner
        /// </summary>
        BottomLeft,
        
        /// <summary>
        /// Bottom center
        /// </summary>
        BottomCenter,
        
        /// <summary>
        /// Bottom right corner
        /// </summary>
        BottomRight,
        
        /// <summary>
        /// Automatically select the nearest placement based on current window positions
        /// This option is only used during attachment creation
        /// </summary>
        Nearest
    }
}

