using System;
using System.Diagnostics;
using System.Linq;
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
                        return paths[0];
                    }
                    else if (data is string text && !string.IsNullOrWhiteSpace(text))
                    {
                        // Parse as newline-separated paths
                        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            return lines[0];
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

