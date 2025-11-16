using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace QuickerActionPanel.Utils.Extension
{
    /// <summary>
    /// Enum extension methods
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Get DisplayAttribute from enum value
        /// </summary>
        public static T? GetAttribute<T>(this Enum enumValue) where T : Attribute
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()?
                .GetCustomAttribute<T>();
        }

        /// <summary>
        /// Get display name from enum value
        /// </summary>
        public static string? GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetAttribute<DisplayAttribute>()?.Name;
        }

        /// <summary>
        /// Get display description from enum value
        /// </summary>
        public static string? GetDisplayDescription(this Enum enumValue)
        {
            return enumValue.GetAttribute<DisplayAttribute>()?.Description;
        }

        /// <summary>
        /// Get all browsable enum values
        /// </summary>
        public static IEnumerable<object> GetValuesOfBrowsable(Type type)
        {
            foreach (var item in Enum.GetValues(type).OfType<Enum>())
            {
                var browsable = item.GetAttribute<BrowsableAttribute>()?.Browsable ?? true;
                if (browsable)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Get all browsable enum values for this enum type
        /// </summary>
        public static IEnumerable<object> GetValuesByBrowsable(this Enum enumValue)
        {
            return GetValuesOfBrowsable(enumValue.GetType());
        }
    }
}

