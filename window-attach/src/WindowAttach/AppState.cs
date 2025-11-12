using WindowAttach.Services;

namespace WindowAttach
{
    /// <summary>
    /// Application state management - stores singleton instances of services
    /// </summary>
    public static class AppState
    {
        private static WindowAttachManagerService? _managerService;

        /// <summary>
        /// Get or create the singleton instance of WindowAttachManagerService
        /// </summary>
        public static WindowAttachManagerService ManagerService
        {
            get
            {
                if (_managerService == null)
                {
                    _managerService = new WindowAttachManagerService();
                }
                return _managerService;
            }
        }

        /// <summary>
        /// Initialize the application state
        /// </summary>
        public static void Initialize()
        {
            // Ensure ManagerService is created
            _ = ManagerService;
        }

        /// <summary>
        /// Cleanup and dispose resources
        /// </summary>
        public static void Cleanup()
        {
            _managerService?.Dispose();
            _managerService = null;
        }
    }
}

