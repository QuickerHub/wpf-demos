using System.Collections.Generic;

namespace BatchRenameTool.Controls;

/// <summary>
/// Interface for providing completion items based on context
/// </summary>
public interface ICompletionProvider
{
    /// <summary>
    /// Gets completion items for the given context
    /// </summary>
    /// <param name="context">The completion context (e.g., text after '{' for variable completion)</param>
    /// <returns>List of completion items</returns>
    IEnumerable<CompletionItem> GetCompletionItems(string context);
}

/// <summary>
/// Represents a completion item
/// </summary>
public class CompletionItem
{
    public string Text { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional metadata for completion (e.g., method info)
    /// </summary>
    public object? Metadata { get; set; }
    
    /// <summary>
    /// Cursor offset after completion (relative to replacement end).
    /// Negative values move cursor backward, positive forward, 0 means after replacement.
    /// Example: -1 means cursor will be positioned 1 character before the replacement end.
    /// </summary>
    public int CursorOffset { get; set; } = 0;
    
    /// <summary>
    /// Replacement text. If null, uses DisplayText or Text.
    /// </summary>
    public string? ReplacementText { get; set; }
}
