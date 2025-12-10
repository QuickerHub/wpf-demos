using System;

namespace BatchRenameTool.Template
{
    /// <summary>
    /// Attribute to mark template methods (similar to SemanticKernel's KernelFunction)
    /// Supports aliases, description, and parameter information for completion
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MethodAliasAttribute : Attribute
    {
        /// <summary>
        /// Method aliases (e.g., ["大写", "转大写"] for Upper method)
        /// </summary>
        public string[] Aliases { get; }

        /// <summary>
        /// Description of the method for completion tooltip
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the method requires parameters
        /// </summary>
        public bool HasParameters { get; set; }

        /// <summary>
        /// Constructor with aliases
        /// </summary>
        /// <param name="aliases">Method aliases (at least one required)</param>
        public MethodAliasAttribute(params string[] aliases)
        {
            if (aliases == null || aliases.Length == 0)
                throw new ArgumentException("At least one alias must be provided", nameof(aliases));

            Aliases = aliases;
        }
    }
}
