using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActionPathConvert.Services
{
    /// <summary>
    /// Helper class for processing playlist files
    /// </summary>
    public static class PlaylistFileHelper
    {
        /// <summary>
        /// Extract file paths from playlist content, filtering out metadata tags and invalid entries
        /// Supports M3U, M3U8, DPL (Daum PotPlayer), and other playlist formats
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="sourceFile">Source file path (for resolving relative paths)</param>
        /// <returns>List of extracted file paths</returns>
        public static List<string> ExtractFilePaths(string content, string sourceFile)
        {
            var paths = new List<string>();
            var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var sourceDirectory = Path.GetDirectoryName(sourceFile) ?? "";
            
            // Detect file format based on content
            bool isDplFormat = content.Contains("DAUMPLAYLIST", StringComparison.OrdinalIgnoreCase);
            bool isM3UFormat = content.Contains("#EXTM3U") || content.Contains("#EXTINF");

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Handle DPL format (Daum PotPlayer)
                if (isDplFormat)
                {
                    // Skip DPL metadata lines
                    if (trimmedLine.Equals("DAUMPLAYLIST", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("playtime=", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("topindex=", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("saveplaypos=", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Contains("*played*") ||
                        trimmedLine.Contains("*duration"))
                    {
                        continue;
                    }
                    
                    // DPL format: file path is usually on its own line, may contain | separator
                    // Format: "file_path|title" or just "file_path"
                    if (trimmedLine.Contains("|"))
                    {
                        trimmedLine = trimmedLine.Split('|')[0].Trim();
                    }
                }
                else
                {
                    // Handle M3U/M3U8 format
                    // Skip all metadata tags (lines starting with #)
                    if (trimmedLine.StartsWith("#"))
                        continue;
                }

                // Skip lines that look like URLs (http://, https://, ftp://, etc.)
                if (trimmedLine.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Skip lines that don't look like file paths at all
                // Check for common non-path patterns
                if (trimmedLine.Contains("=") && !trimmedLine.Contains("\\") && !trimmedLine.Contains("/"))
                    continue; // Likely metadata like "playtime=5157"
                
                if (trimmedLine.Contains("*") && !trimmedLine.Contains("\\") && !trimmedLine.Contains("/"))
                    continue; // Likely metadata like "1*played*1"

                // Remove quotes if present
                trimmedLine = trimmedLine.Trim('"', '\'');

                // Skip if still empty after trimming quotes
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // URL decode if needed
                try
                {
                    var decoded = Uri.UnescapeDataString(trimmedLine);
                    if (decoded != trimmedLine && !decoded.Contains("://"))
                    {
                        trimmedLine = decoded;
                    }
                }
                catch
                {
                    // If URL decoding fails, use original string
                }

                // Validate that it looks like a file path
                // Must contain path separators (backslash or forward slash) or be a valid drive letter path
                bool looksLikePath = false;
                
                // Check for Windows absolute path (C:\, D:\, etc.)
                if (trimmedLine.Length >= 3 && 
                    char.IsLetter(trimmedLine[0]) && 
                    trimmedLine[1] == ':' && 
                    (trimmedLine[2] == '\\' || trimmedLine[2] == '/'))
                {
                    looksLikePath = true;
                }
                // Check for UNC path (\\server\share)
                else if (trimmedLine.StartsWith("\\\\"))
                {
                    looksLikePath = true;
                }
                // Check for relative path with separators
                else if (trimmedLine.Contains("\\") || trimmedLine.Contains("/"))
                {
                    looksLikePath = true;
                }
                // Check for file extension (common audio/video extensions)
                else if (Path.HasExtension(trimmedLine))
                {
                    var ext = Path.GetExtension(trimmedLine).ToLowerInvariant();
                    var validExtensions = new[] { ".mp3", ".flac", ".wav", ".mp4", ".m4a", ".aac", ".ogg", ".wma", ".m3u", ".m3u8" };
                    if (validExtensions.Contains(ext))
                    {
                        looksLikePath = true;
                    }
                }
                
                if (!looksLikePath)
                    continue;

                // Resolve relative paths to absolute paths if needed
                string finalPath = trimmedLine;
                if (!Path.IsPathRooted(trimmedLine) && !string.IsNullOrEmpty(sourceDirectory))
                {
                    try
                    {
                        finalPath = Path.Combine(sourceDirectory, trimmedLine);
                        finalPath = Path.GetFullPath(finalPath);
                    }
                    catch
                    {
                        finalPath = trimmedLine;
                    }
                }

                // Final validation: must have a file extension or be a valid path structure
                if (!string.IsNullOrWhiteSpace(finalPath))
                {
                    // Additional check: ensure it looks like a file path (not just a directory or metadata)
                    // Must have an extension or be a valid file path structure
                    if (Path.HasExtension(finalPath) || finalPath.Contains("\\") || finalPath.Contains("/"))
                    {
                        paths.Add(finalPath);
                    }
                }
            }

            return paths;
        }

        /// <summary>
        /// Save output M3U file for a specific input file
        /// </summary>
        /// <param name="inputFile">Input file path</param>
        /// <param name="outputPaths">Output file paths for this input file</param>
        /// <returns>Path to the saved M3U file, or empty string if failed</returns>
        public static string SaveOutputM3uFile(string inputFile, List<string> outputPaths)
        {
            if (outputPaths == null || outputPaths.Count == 0)
                return string.Empty;

            try
            {
                var inputDirectory = Path.GetDirectoryName(inputFile);
                if (string.IsNullOrEmpty(inputDirectory))
                    return string.Empty;

                var inputFileName = Path.GetFileNameWithoutExtension(inputFile);
                var outputM3uPath = Path.Combine(inputDirectory, $"{inputFileName}.m3u");

                // Create simple M3U format content (only file paths, no metadata)
                var m3uContent = string.Join(Environment.NewLine, outputPaths);

                File.WriteAllText(outputM3uPath, m3uContent, System.Text.Encoding.UTF8);
                
                return outputM3uPath;
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                System.Diagnostics.Debug.WriteLine($"Failed to save M3U file for {inputFile}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Read playlist file content
        /// </summary>
        /// <param name="filePath">Path to the playlist file</param>
        /// <returns>File content, or empty string if file doesn't exist or read fails</returns>
        public static string ReadPlaylistFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read playlist file {filePath}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}

