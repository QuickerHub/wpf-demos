using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace BatchRenameTool.Controls;

/// <summary>
/// Extension methods for TextDocument operations
/// </summary>
internal static class TextDocumentExtensions
{
    /// <summary>
    /// Safely replace text in a range
    /// </summary>
    public static bool SafeReplace(this TextDocument doc, int offset, int length, string replacement)
    {
        if (doc == null || replacement == null)
            return false;

        // Allow insertion at end of document (offset == doc.TextLength)
        if (offset < 0 || offset > doc.TextLength)
            return false;

        if (length < 0)
            return false;

        // Calculate max length that can be replaced
        // If offset is at end (offset == doc.TextLength), maxLength is 0 (pure insertion)
        var maxLength = doc.TextLength - offset;
        var actualLength = Math.Min(length, maxLength);

        // actualLength can be 0 for pure insertion at end
        if (actualLength < 0)
            return false;

        try
        {
            doc.Replace(offset, actualLength, replacement);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely insert text at a position
    /// </summary>
    public static bool SafeInsert(this TextDocument doc, int offset, string text)
    {
        return doc.SafeReplace(offset, 0, text);
    }

    /// <summary>
    /// Safely remove text in a range
    /// </summary>
    public static bool SafeRemove(this TextDocument doc, int offset, int length)
    {
        return doc.SafeReplace(offset, length, string.Empty);
    }

    /// <summary>
    /// Safely remove a segment
    /// </summary>
    public static bool SafeRemove(this TextDocument doc, ISegment segment)
    {
        if (doc == null || segment == null)
            return false;

        return doc.SafeRemove(segment.Offset, segment.Length);
    }
}

/// <summary>
/// Extension methods for TextArea operations
/// </summary>
internal static class TextAreaExtensions
{
    /// <summary>
    /// Safely set caret offset
    /// </summary>
    public static bool SafeSetCaretOffset(this TextArea textArea, int offset)
    {
        if (textArea == null || textArea.Document == null)
            return false;

        var doc = textArea.Document;
        if (offset < 0 || offset > doc.TextLength)
            return false;

        try
        {
            textArea.Caret.Offset = offset;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Undo AvalonEdit's automatic insertion and return the offset after removal
    /// </summary>
    public static int UndoAvalonEditInsertion(this TextArea textArea, ISegment completionSegment)
    {
        if (textArea?.Document == null)
            return textArea?.Caret.Offset ?? 0;

        if (completionSegment != null && completionSegment.Length > 0)
        {
            var offset = completionSegment.Offset;
            textArea.Document.SafeRemove(completionSegment);
            return offset;
        }

        return textArea.Caret.Offset;
    }

    /// <summary>
    /// Replace text range and set cursor position
    /// </summary>
    public static bool ReplaceAndSetCursor(this TextArea textArea, int startOffset, int endOffset, string replacement, int? cursorOffset = null)
    {
        if (textArea?.Document == null)
            return false;

        var doc = textArea.Document;
        var replaceLength = Math.Max(0, endOffset - startOffset);

        if (!doc.SafeReplace(startOffset, replaceLength, replacement))
            return false;

        // Set cursor position
        var newOffset = cursorOffset ?? (startOffset + replacement.Length);
        return textArea.SafeSetCaretOffset(newOffset);
    }
}
