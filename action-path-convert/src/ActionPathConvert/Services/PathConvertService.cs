using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionPathConvert.Models;

namespace ActionPathConvert.Services
{
    /// <summary>
    /// Service for converting file paths in playlists
    /// </summary>
    public class PathConvertService
    {
        /// <summary>
        /// Process file paths and convert them to target directory paths
        /// </summary>
        /// <param name="inputFiles">Input file path list</param>
        /// <param name="searchDirectory">Target search directory</param>
        /// <param name="audioExtensions">Audio file extensions (e.g., "*.mp3,*.flac,*.mp4")</param>
        /// <param name="preferredExtension">Preferred extension (e.g., ".mp3")</param>
        /// <param name="removePathPrefix">Path prefix to remove for relative path conversion</param>
        /// <returns>Conversion result</returns>
        public PathConvertResult ProcessFilePaths(
            List<string> inputFiles,
            string searchDirectory,
            string audioExtensions,
            string preferredExtension,
            string removePathPrefix)
        {
            var result = new PathConvertResult();

            // Build dictionary of target files (recursive scan)
            var targetFileDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(searchDirectory))
            {
                var extensions = audioExtensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim())
                    .Where(ext => !string.IsNullOrEmpty(ext))
                    .ToArray();

                var allFiles = extensions.SelectMany(ext => Directory.GetFiles(searchDirectory, ext, SearchOption.AllDirectories));

                foreach (string filePath in allFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (!targetFileDict.ContainsKey(fileName))
                    {
                        targetFileDict[fileName] = filePath;
                    }
                }
            }

            // Build input file dictionary (group by filename without extension)
            var inputFileDict = inputFiles
                .GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Process each unique filename
            foreach (var kvp in inputFileDict)
            {
                string baseName = kvp.Key;
                var fileGroup = kvp.Value;
                string resultPath = "";

                // Check if file exists in dictionary
                if (targetFileDict.ContainsKey(baseName))
                {
                    resultPath = targetFileDict[baseName];

                    // Convert to relative path (if removePathPrefix is specified)
                    if (!string.IsNullOrEmpty(removePathPrefix) && resultPath.StartsWith(removePathPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        resultPath = resultPath.Substring(removePathPrefix.Length).TrimStart('\\', '/');
                    }

                    result.OutputFiles.Add(resultPath);
                }
                else
                {
                    // If not found in dictionary, select preferred file from input group
                    string selectedFile = fileGroup.FirstOrDefault(f =>
                        !string.IsNullOrEmpty(preferredExtension) && f.EndsWith(preferredExtension, StringComparison.OrdinalIgnoreCase))
                        ?? fileGroup.First();

                    result.NotFoundFiles.Add(selectedFile);
                }
            }

            return result;
        }
    }
}

