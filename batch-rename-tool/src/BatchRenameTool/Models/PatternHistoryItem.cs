using System;

namespace BatchRenameTool.Models
{
    /// <summary>
    /// Pattern history item with title and pattern string
    /// </summary>
    public class PatternHistoryItem
    {
        /// <summary>
        /// Title for the pattern (optional, can be empty)
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Pattern string
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the pattern was used
        /// </summary>
        public DateTime UsedAt { get; set; } = DateTime.Now;
    }
}
