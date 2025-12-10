using System.Collections.Generic;

namespace BatchRenameTool.Controls;

/// <summary>
/// Completion type enumeration
/// </summary>
public enum CompletionType
{
    Variable,   // Variable completion: {name}
    Format,     // Format completion: {i:00}
    Method      // Method completion: {name.upper}
}

/// <summary>
/// Represents completion context information
/// </summary>
public class CompletionContext
{
    /// <summary>
    /// Type of completion
    /// </summary>
    public CompletionType Type { get; set; }

    /// <summary>
    /// List of completion items (filtered)
    /// </summary>
    public List<CompletionItem> Items { get; set; } = new();

    /// <summary>
    /// Original list of all completion items (before filtering)
    /// Used for re-filtering when text changes
    /// </summary>
    public List<CompletionItem> OriginalItems { get; set; } = new();

    /// <summary>
    /// Start offset for replacement (inclusive)
    /// </summary>
    public int ReplaceStartOffset { get; set; }

    /// <summary>
    /// End offset for replacement (exclusive)
    /// </summary>
    public int ReplaceEndOffset { get; set; }

    /// <summary>
    /// Filter start offset (for AvalonEdit CompletionWindow.StartOffset)
    /// </summary>
    public int FilterStartOffset { get; set; }
}

/// <summary>
/// Interface for completion service
/// Handles all completion logic including syntax checking
/// </summary>
public interface ICompletionService
{
    /// <summary>
    /// Check if completion should be triggered at the given position
    /// </summary>
    /// <param name="documentText">Full document text</param>
    /// <param name="caretOffset">Current caret offset</param>
    /// <param name="triggerChar">Character that triggered completion (e.g., '{', '.')</param>
    /// <returns>Completion context if completion should be shown, null otherwise</returns>
    CompletionContext? GetCompletionContext(string documentText, int caretOffset, char triggerChar);

    /// <summary>
    /// Update completion context when text changes (for filtering)
    /// </summary>
    /// <param name="context">Previous completion context</param>
    /// <param name="documentText">Current document text</param>
    /// <param name="caretOffset">Current caret offset</param>
    /// <returns>Updated completion context, or null if completion should be closed</returns>
    CompletionContext? UpdateCompletionContext(CompletionContext context, string documentText, int caretOffset);

    /// <summary>
    /// Check if cursor is inside braces {} and get the opening brace position
    /// </summary>
    /// <param name="documentText">Full document text</param>
    /// <param name="caretOffset">Current caret offset</param>
    /// <returns>Opening brace position if cursor is inside braces, -1 otherwise</returns>
    int IsInsideBraces(string documentText, int caretOffset);
}
