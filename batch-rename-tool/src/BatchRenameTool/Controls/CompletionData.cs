using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace BatchRenameTool.Controls;

/// <summary>
/// Unified completion data implementation
/// All completion types use this single class, with cursor position controlled by CursorOffset
/// </summary>
internal class CompletionData : ICompletionData
{
    public string Text { get; set; } = string.Empty;
    public object Content { get; set; } = string.Empty;
    public object Description { get; set; } = string.Empty;
    public int ReplaceStartOffset { get; set; }
    public int ReplaceEndOffset { get; set; }
    public int CursorOffset { get; set; } = 0; // Relative offset from replacement end
    public string ReplacementText { get; set; } = string.Empty; // Actual text to insert
    public double Priority => 0;
    public System.Windows.Media.ImageSource? Image => null;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var doc = textArea.Document;
        if (doc == null)
            return;

        // Undo AvalonEdit's automatic insertion
        textArea.UndoAvalonEditInsertion(completionSegment);
        
        // Use ReplaceEndOffset provided by service (original cursor position before insertion)
        var replaceEndOffset = ReplaceEndOffset;
        
        // Ensure ReplaceStartOffset is valid (allow insertion at end of document)
        if (ReplaceStartOffset < 0 || ReplaceStartOffset > doc.TextLength)
        {
            return; // Invalid start offset
        }
        
        // Clamp replaceEndOffset to valid range
        replaceEndOffset = Math.Min(replaceEndOffset, doc.TextLength);
        replaceEndOffset = Math.Max(ReplaceStartOffset, replaceEndOffset);
        
        // Perform replacement (allow insertion at end: replaceLength can be 0)
        var replaceLength = replaceEndOffset - ReplaceStartOffset;
        if (!doc.SafeReplace(ReplaceStartOffset, replaceLength, ReplacementText))
        {
            return; // Replacement failed
        }
        
        // Calculate cursor position after replacement: replacement end + CursorOffset
        var replacementEnd = ReplaceStartOffset + ReplacementText.Length;
        var cursorOffset = replacementEnd + CursorOffset;
        
        // Ensure cursor offset is within valid bounds
        var currentDocLength = doc.TextLength;
        cursorOffset = Math.Max(ReplaceStartOffset, cursorOffset);
        cursorOffset = Math.Min(cursorOffset, currentDocLength);
        
        // Set cursor position
        textArea.SafeSetCaretOffset(cursorOffset);
    }
}
