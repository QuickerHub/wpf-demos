using System;
using System.IO;

namespace BatchRenameTool.Template.Evaluator;

/// <summary>
/// Interface for template evaluation context
/// All properties are read-only to support lazy evaluation
/// </summary>
public interface IEvaluationContext
{
    string Name { get; }           // File name without extension
    string Ext { get; }            // Extension without dot
    string FullName { get; }       // Full file name
    string FullPath { get; }       // Full file path
    string DirName { get; }         // Directory name (folder name containing the file)
    int Index { get; }              // Index for {i} variable
    int TotalCount { get; }         // Total count for {iv} variable (reverse index)
    DateTime Today { get; }         // Current date for {today} variable
    DateTime Now { get; }           // Current date/time for {now} variable
    
    // Lazy-loaded objects
    IImageInfo Image { get; }      // Image information (lazy loaded)
    FileInfo File { get; }          // File information (System.IO.FileInfo, lazy loaded)
    long Size { get; }              // File size (convenience property, same as File.Length)
}
