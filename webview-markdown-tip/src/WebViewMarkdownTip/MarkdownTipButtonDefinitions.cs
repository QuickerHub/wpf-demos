using System;
using System.Collections.Generic;
using System.Linq;
using Quicker.Public.Entities;

namespace WebViewMarkdownTip
{
    /// <summary>
    /// Maps button definition lines to WPF footer items (same parsing as CeaQuickerTools ViewRunner.GenerateItems / MessageBox3md custom buttons).
    /// </summary>
    public static class MarkdownTipButtonDefinitions
    {
        /// <summary>
        /// Parse <paramref name="definitions"/> with <see cref="CommonOperationItem.ParseLines(IList{string})"/>.
        /// </summary>
        public static IReadOnlyList<MarkdownTipWebButton> Parse(IList<string>? definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                return Array.Empty<MarkdownTipWebButton>();
            }

            var items = CommonOperationItem.ParseLines(definitions);
            return items
                .Where(static i => !i.IsSeparator)
                .Select(static i => new MarkdownTipWebButton
                {
                    Title = i.Title ?? string.Empty,
                    Data = i.Data ?? string.Empty,
                    Icon = i.Icon,
                    Description = i.Description,
                })
                .ToList();
        }
    }

    /// <summary>
    /// Footer button row entry (WPF ItemsControl / optional future host bridge).
    /// </summary>
    public sealed class MarkdownTipWebButton
    {
        public string Title { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public string? Description { get; set; }
    }
}
