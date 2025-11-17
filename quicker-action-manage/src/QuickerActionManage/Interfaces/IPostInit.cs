namespace QuickerActionManage.Interfaces
{
    /// <summary>
    /// Interface for objects that need post-initialization setup
    /// </summary>
    public interface IPostInit
    {
        /// <summary>
        /// Called after initialization is complete to set up event handlers and other post-init logic
        /// </summary>
        void PostInit();
    }
}

