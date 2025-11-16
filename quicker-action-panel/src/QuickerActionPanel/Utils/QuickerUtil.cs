using System.Reflection;
using Quicker.Common;
using Quicker.Domain;

namespace QuickerActionPanel.Utils
{
    /// <summary>
    /// Utility class for Quicker operations
    /// </summary>
    public static class QuickerUtil
    {
        private static readonly bool IsInQuicker;

        static QuickerUtil()
        {
            IsInQuicker = Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";
        }

        /// <summary>
        /// Gets an action item by ID
        /// </summary>
        /// <param name="actionId">The action ID</param>
        /// <returns>The action item if found; otherwise, null</returns>
        public static ActionItem? GetActionById(string? actionId)
        {
            if (IsInQuicker)
            {
                try
                {
                    return AppState.DataService.GetActionById(actionId).action;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}

