using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatchRenameTool.Template.Compiler;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;

namespace BatchRenameTool.Services
{
    /// <summary>
    /// Service for batch renaming files using template patterns
    /// </summary>
    public class BatchRenameService
    {
        private readonly TemplateParser _parser;
        private readonly TemplateCompiler _compiler;
        private readonly BatchRenameExecutor _executor;

        public BatchRenameService()
        {
            _parser = new TemplateParser(new List<Type>());
            _compiler = new TemplateCompiler();
            _executor = new BatchRenameExecutor();
        }

        /// <summary>
        /// Rename files using a template pattern
        /// </summary>
        /// <param name="filePaths">List of file paths to rename</param>
        /// <param name="pattern">Template pattern (e.g., "{name.upper()}", "{i:000}")</param>
        /// <param name="progress">Progress reporter (current index, total count, current file name)</param>
        /// <returns>Rename result</returns>
        public BatchRenameExecutor.RenameResult RenameFiles(
            IEnumerable<string> filePaths, 
            string pattern,
            IProgress<(int current, int total, string fileName)>? progress = null)
        {
            var fileList = filePaths?.ToList() ?? new List<string>();
            
            if (fileList.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            // Filter valid files
            var validFiles = fileList.Where(File.Exists).ToList();
            if (validFiles.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            // Generate new names using Select
            IEnumerable<string> newNames;
            if (string.IsNullOrWhiteSpace(pattern))
            {
                // Empty pattern means no rename - use original file names
                newNames = validFiles.Select(filePath => Path.GetFileName(filePath));
            }
            else
            {
                // Parse and compile template
                var templateNode = _parser.Parse(pattern);
                var compiledFunction = _compiler.Compile(templateNode);
                int totalCount = validFiles.Count;

                // Generate new names using Select with compiled function
                newNames = validFiles.Select((filePath, i) =>
                {
                    var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                    var originalName = Path.GetFileName(filePath);
                    var extension = Path.GetExtension(originalName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(originalName);

                    // Create evaluation context
                    var context = new EvaluationContext(
                        name: nameWithoutExt,
                        ext: extension.TrimStart('.'),
                        fullName: originalName,
                        fullPath: filePath,
                        index: i,
                        totalCount: totalCount);

                    // Evaluate using compiled function
                    var newName = compiledFunction(context);

                    // Auto-add extension if template doesn't include it
                    if (!string.IsNullOrEmpty(extension) && !newName.Contains("."))
                    {
                        bool templateHasExt = pattern.Contains("{ext}", StringComparison.OrdinalIgnoreCase);
                        if (!templateHasExt)
                        {
                            newName += extension;
                        }
                    }

                    return newName;
                });
            }

            // Call the other overload with generated new names
            return RenameFiles(validFiles, newNames, progress);
        }

        /// <summary>
        /// Rename files using provided new names
        /// </summary>
        /// <param name="filePaths">List of original file paths</param>
        /// <param name="newNames">List of new file names (must match filePaths count)</param>
        /// <param name="progress">Progress reporter (current index, total count, current file name)</param>
        /// <returns>Rename result</returns>
        public BatchRenameExecutor.RenameResult RenameFiles(
            IEnumerable<string> filePaths,
            IEnumerable<string> newNames,
            IProgress<(int current, int total, string fileName)>? progress = null)
        {
            var fileList = filePaths?.ToList() ?? new List<string>();
            var nameList = newNames?.ToList() ?? new List<string>();

            if (fileList.Count == 0 || nameList.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            if (fileList.Count != nameList.Count)
            {
                throw new ArgumentException($"File paths count ({fileList.Count}) does not match new names count ({nameList.Count})");
            }

            // Filter valid files
            var validPairs = new List<(string filePath, string newName)>();
            for (int i = 0; i < fileList.Count; i++)
            {
                if (File.Exists(fileList[i]))
                {
                    validPairs.Add((fileList[i], nameList[i]));
                }
            }

            if (validPairs.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            // Generate rename operations
            var operations = validPairs.Select(pair =>
                new BatchRenameExecutor.RenameOperation(pair.filePath, pair.newName)).ToList();

            // Execute rename
            return _executor.Execute(operations, progress);
        }
    }
}

