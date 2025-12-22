using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace VscodeTools
{
    /// <summary>
    /// Helper class to open files from VSCode clipboard format (code/file-list)
    /// </summary>
    public static class VscodeFileOpener
    {
        private const string VscodeFileListFormat = "code/file-list";
        private const int WaitAfterCopyMs = 50;

        /// <summary>
        /// Check clipboard for code/file-list format and open the first file path
        /// If not found, send Ctrl+C and wait, then check again
        /// </summary>
        /// <returns>True if a file was opened successfully, false otherwise</returns>
        public static bool TryOpenFileFromClipboard()
        {
            try
            {
                // First, check if clipboard already contains the format
                string? filePath = GetFilePathFromClipboard();
                
                if (filePath == null)
                {
                    // Send Ctrl+C to copy
                    SendCtrlC();
                    
                    // Wait for clipboard to update
                    Thread.Sleep(WaitAfterCopyMs);
                    
                    // Check again
                    filePath = GetFilePathFromClipboard();
                }

                if (filePath != null)
                {
                    // Open the file using system default application
                    OpenFile(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Log error if needed
                System.Diagnostics.Debug.WriteLine($"Error in TryOpenFileFromClipboard: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get clipboard content in code/file-list format
        /// </summary>
        /// <returns>Formatted string representation of clipboard content, or null if not found</returns>
        public static string? GetClipboardFileListContent()
        {
            try
            {
                // Check for VSCode file-list format
                if (Clipboard.ContainsData(VscodeFileListFormat))
                {
                    var data = Clipboard.GetData(VscodeFileListFormat);
                    if (data is string[] paths && paths.Length > 0)
                    {
                        // Convert file:/// URIs to normal paths
                        var convertedPaths = paths.Select(path =>
                        {
                            var trimmed = path?.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && trimmed.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                            {
                                return ConvertFileUriToPath(trimmed);
                            }
                            return trimmed ?? path;
                        });
                        return string.Join(Environment.NewLine, convertedPaths);
                    }
                    else if (data is string text && !string.IsNullOrWhiteSpace(text))
                    {
                        // Convert file:/// URIs to normal paths
                        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        var convertedLines = lines.Select(line =>
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                            {
                                return ConvertFileUriToPath(trimmed);
                            }
                            return trimmed;
                        });
                        return string.Join(Environment.NewLine, convertedLines);
                    }
                    else if (data is MemoryStream ms)
                    {
                        // Read MemoryStream as text
                        try
                        {
                            var originalPosition = ms.Position;
                            ms.Position = 0;
                            
                            byte[] buffer = new byte[ms.Length];
                            int bytesRead = ms.Read(buffer, 0, buffer.Length);
                            ms.Position = originalPosition; // Restore original position
                            
                            if (bytesRead > 0)
                            {
                                // Try UTF-8 first
                                string content = null;
                                try
                                {
                                    content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                }
                                catch
                                {
                                    // If UTF-8 fails, try default encoding
                                    content = Encoding.Default.GetString(buffer, 0, bytesRead);
                                }
                                
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    // Convert file:/// URIs to normal paths
                                    var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                                    var convertedLines = lines.Select(line =>
                                    {
                                        var trimmed = line.Trim();
                                        if (trimmed.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                                        {
                                            return ConvertFileUriToPath(trimmed);
                                        }
                                        return trimmed;
                                    });
                                    return string.Join(Environment.NewLine, convertedLines);
                                }
                            }
                        }
                        catch
                        {
                            // If reading fails, continue to fallback
                        }
                    }
                }

                // Fallback: check clipboard text for file:/// paths
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        var fileLines = lines.Where(line => line.Trim().StartsWith("file:///", StringComparison.OrdinalIgnoreCase));
                        if (fileLines.Any())
                        {
                            return string.Join(Environment.NewLine, fileLines);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return $"Error reading clipboard: {ex.Message}";
            }
        }

        /// <summary>
        /// Get file path from clipboard, checking for code/file-list format
        /// </summary>
        private static string? GetFilePathFromClipboard()
        {
            try
            {
                // Check for VSCode file-list format
                if (Clipboard.ContainsData(VscodeFileListFormat))
                {
                    var data = Clipboard.GetData(VscodeFileListFormat);
                    if (data is string[] paths && paths.Length > 0)
                    {
                        var firstPath = paths[0]?.Trim();
                        if (!string.IsNullOrEmpty(firstPath))
                        {
                            // Convert file:/// URI to local path if needed
                            if (firstPath.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                            {
                                return ConvertFileUriToPath(firstPath);
                            }
                            return firstPath;
                        }
                    }
                    else if (data is string text && !string.IsNullOrWhiteSpace(text))
                    {
                        // Parse as newline-separated paths
                        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            var firstLine = lines[0].Trim();
                            // Convert file:/// URI to local path if needed
                            if (firstLine.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                            {
                                return ConvertFileUriToPath(firstLine);
                            }
                            return firstLine;
                        }
                    }
                    else if (data is MemoryStream ms)
                    {
                        // Read MemoryStream as text
                        try
                        {
                            var originalPosition = ms.Position;
                            ms.Position = 0;
                            
                            byte[] buffer = new byte[ms.Length];
                            int bytesRead = ms.Read(buffer, 0, buffer.Length);
                            ms.Position = originalPosition; // Restore original position
                            
                            if (bytesRead > 0)
                            {
                                // Try UTF-8 first
                                string content = null;
                                try
                                {
                                    content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                }
                                catch
                                {
                                    // If UTF-8 fails, try default encoding
                                    content = Encoding.Default.GetString(buffer, 0, bytesRead);
                                }
                                
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    // Parse as newline-separated paths
                                    var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                                    if (lines.Length > 0)
                                    {
                                        var firstLine = lines[0].Trim();
                                        // Convert file:/// URI to local path if needed
                                        if (firstLine.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                                        {
                                            return ConvertFileUriToPath(firstLine);
                                        }
                                        return firstLine;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // If reading fails, continue to fallback
                        }
                    }
                }

                // Fallback: check clipboard text for file:/// paths
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        var firstLine = lines.FirstOrDefault()?.Trim();
                        
                        if (firstLine != null && firstLine.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                        {
                            // Convert file:/// URI to local path
                            return ConvertFileUriToPath(firstLine);
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert file:/// URI to local file path
        /// </summary>
        private static string ConvertFileUriToPath(string uri)
        {
            try
            {
                // Remove file:/// prefix
                if (uri.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                {
                    var path = uri.Substring(8); // Remove "file:///"
                    
                    // Decode URI encoding if needed
                    path = Uri.UnescapeDataString(path);
                    
                    // Handle Windows path format (file:///C:/path -> C:/path)
                    if (path.Length > 1 && path[1] == ':')
                    {
                        return path;
                    }
                    
                    // Handle Unix-style paths (file:///path -> /path)
                    return path;
                }

                return uri;
            }
            catch
            {
                return uri;
            }
        }

        /// <summary>
        /// Send Ctrl+C keyboard shortcut
        /// </summary>
        private static void SendCtrlC()
        {
            try
            {
                // Use SendKeys to send Ctrl+C
                System.Windows.Forms.SendKeys.SendWait("^c");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending Ctrl+C: {ex.Message}");
            }
        }

        /// <summary>
        /// Open file using system default application
        /// </summary>
        private static void OpenFile(string filePath)
        {
            try
            {
                // Convert file:/// URI to local path if needed
                if (filePath.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = ConvertFileUriToPath(filePath);
                }

                // Use Process.Start to open file with default application
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening file: {ex.Message}");
                throw;
            }
        }
    }
}

