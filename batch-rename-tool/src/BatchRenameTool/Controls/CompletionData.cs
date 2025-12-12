using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace BatchRenameTool.Controls;

/// <summary>
/// Simplified completion data implementation based on QuickerTools pattern
/// Uses replaceOffset to adjust replacement start and completeOffset for cursor positioning
/// </summary>
internal class CompletionData : ICompletionData
{
    public string Text { get; set; } = string.Empty;
    public object Content { get; set; } = string.Empty;
    public object Description { get; set; } = string.Empty;
    public double Priority => 0;
    public System.Windows.Media.ImageSource? Image => null;

    /// <summary>
    /// Actual text to insert (defaults to Text if not set)
    /// </summary>
    public string ActualText { get; set; } = string.Empty;

    /// <summary>
    /// Offset to adjust replacement start position
    /// Used when user has typed filter text (e.g., ".repl" -> replaceOffset = 4)
    /// Replacement will start at completionSegment.Offset - replaceOffset
    /// </summary>
    public int ReplaceOffset { get; set; } = 0;

    /// <summary>
    /// Offset to adjust cursor position after completion
    /// Negative values move cursor backward, positive forward
    /// </summary>
    public int CompleteOffset { get; set; } = 0;

    /// <summary>
    /// Metadata for completion (e.g., method info for adding parentheses)
    /// </summary>
    public object? Metadata { get; set; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        if (textArea?.Document == null || completionSegment == null)
            return;

        var doc = textArea.Document;

        // Use actualText if set, otherwise use Text
        var actualText = string.IsNullOrEmpty(ActualText) ? Text : ActualText;

        // Calculate replacement segment: adjust start by replaceOffset
        // completionSegment.Offset is the StartOffset (FilterStartOffset)
        // We need to replace from ReplaceStartOffset, which is ReplaceOffset characters before FilterStartOffset
        var replaceStart = completionSegment.Offset - ReplaceOffset;
        
        // Ensure replaceStart is not negative and within document bounds
        if (replaceStart < 0)
        {
            replaceStart = 0;
        }
        if (replaceStart > doc.TextLength)
        {
            replaceStart = doc.TextLength;
        }

        // Get replaceEnd from completionSegment
        var replaceEnd = completionSegment.EndOffset;
        
        // Ensure replaceEnd is within document bounds
        if (replaceEnd < 0)
        {
            replaceEnd = 0;
        }
        if (replaceEnd > doc.TextLength)
        {
            replaceEnd = doc.TextLength;
        }
        
        // Ensure replaceEnd is not less than replaceStart
        if (replaceEnd < replaceStart)
        {
            replaceEnd = replaceStart;
        }

        var replaceSegment = new SelectionSegment(replaceStart, replaceEnd);

        // Perform replacement
        doc.Replace(replaceSegment, actualText);

        // Calculate new cursor position after replacement
        var newCursorOffset = replaceStart + actualText.Length;

        // Adjust cursor position by CompleteOffset (add to move forward, subtract to move backward)
        newCursorOffset += CompleteOffset;

        // Ensure cursor offset is within document bounds
        newCursorOffset = Math.Max(0, Math.Min(newCursorOffset, doc.TextLength));

        // Set cursor position
        textArea.Caret.Offset = newCursorOffset;

        // If this is a method with parameters, add parentheses and move cursor inside
        if (Metadata is TemplateCompletionService.MethodInfo methodInfo && methodInfo.HasParameters)
        {
            var methodEndOffset = textArea.Caret.Offset;
            CodeTextBoxExtensions.InsertParenthesesAndMoveCursor(textArea, methodEndOffset);
        }
    }
}
