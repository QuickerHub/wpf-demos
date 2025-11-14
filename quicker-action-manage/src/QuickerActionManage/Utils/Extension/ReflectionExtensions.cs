using System.ComponentModel;
using System.Reflection;

namespace QuickerActionManage.Utils.Extension
{
    /// <summary>
    /// Reflection extension methods
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Get display name from property
        /// </summary>
        public static string? GetDisplayName(this PropertyInfo property)
        {
            return property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        }

        /// <summary>
        /// Get description from property
        /// </summary>
        public static string? GetDescription(this PropertyInfo property)
        {
            return property?.GetCustomAttribute<DescriptionAttribute>()?.Description;
        }
    }
}

