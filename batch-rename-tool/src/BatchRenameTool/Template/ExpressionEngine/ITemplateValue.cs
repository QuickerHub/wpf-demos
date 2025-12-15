using System.Collections.Generic;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Base interface for all template value types
    /// Represents a value in the template expression system
    /// </summary>
    public interface ITemplateValue
    {
        /// <summary>
        /// Convert value to string with optional format
        /// </summary>
        string ToString(string? format = null);

        /// <summary>
        /// Invoke a method on this value
        /// </summary>
        ITemplateValue InvokeMethod(string methodName, IReadOnlyList<ITemplateValue> arguments);

        /// <summary>
        /// Check if this value type supports a method
        /// </summary>
        bool HasMethod(string methodName);

        /// <summary>
        /// Get the underlying value (for type checking and conversion)
        /// </summary>
        object? GetValue();
    }
}

