using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BatchRenameTool.Services
{
    /// <summary>
    /// Executes batch rename operations with cycle detection and handling
    /// </summary>
    public class BatchRenameExecutor
    {
        /// <summary>
        /// Represents a rename operation
        /// </summary>
        public class RenameOperation
        {
            public string OriginalPath { get; set; } = string.Empty;
            public string OriginalName { get; set; } = string.Empty;
            public string NewName { get; set; } = string.Empty;
            public string Directory { get; set; } = string.Empty;
        }

        /// <summary>
        /// Result of rename execution
        /// </summary>
        public class RenameResult
        {
            public int SuccessCount { get; set; }
            public int SkippedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        /// <summary>
        /// Execute batch rename operations
        /// </summary>
        /// <param name="operations">List of rename operations</param>
        /// <returns>Rename result</returns>
        public RenameResult Execute(IEnumerable<RenameOperation> operations)
        {
            var result = new RenameResult();
            var operationList = operations.ToList();

            if (operationList.Count == 0)
                return result;

            // Build rename graph and detect cycles
            var (directRename, cycles) = DetectCycles(operationList);

            // Execute direct renames (no cycles)
            foreach (var op in directRename)
            {
                if (TryRename(op, result))
                {
                    result.SuccessCount++;
                }
            }

            // Execute cycle renames (using temporary names)
            foreach (var cycle in cycles)
            {
                ExecuteCycleRename(cycle, result);
            }

            return result;
        }

        /// <summary>
        /// Detect cycles in rename operations
        /// Returns: (direct renames, cycles)
        /// </summary>
        private (List<RenameOperation> directRename, List<List<RenameOperation>> cycles) DetectCycles(
            List<RenameOperation> operations)
        {
            var directRename = new List<RenameOperation>();
            var cycles = new List<List<RenameOperation>>();

            // Build name mapping: original full path -> operation
            var nameToOp = new Dictionary<string, RenameOperation>(StringComparer.OrdinalIgnoreCase);
            foreach (var op in operations)
            {
                var originalFullName = Path.Combine(op.Directory, op.OriginalName);
                nameToOp[originalFullName] = op;
            }

            // Track visited nodes and nodes in current path for cycle detection
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inCycle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var op in operations)
            {
                var originalFullName = Path.Combine(op.Directory, op.OriginalName);
                if (!visited.Contains(originalFullName))
                {
                    var path = new List<RenameOperation>();
                    var cycle = FindCycle(op, nameToOp, visited, new HashSet<string>(StringComparer.OrdinalIgnoreCase), path);
                    
                    if (cycle != null && cycle.Count > 1)
                    {
                        cycles.Add(cycle);
                        // Mark all nodes in cycle
                        foreach (var cycleOp in cycle)
                        {
                            inCycle.Add(Path.Combine(cycleOp.Directory, cycleOp.OriginalName));
                        }
                    }
                }
            }

            // Add operations that are not in cycles to direct rename list
            foreach (var op in operations)
            {
                var originalFullName = Path.Combine(op.Directory, op.OriginalName);
                if (!inCycle.Contains(originalFullName))
                {
                    directRename.Add(op);
                }
            }

            return (directRename, cycles);
        }

        /// <summary>
        /// Find cycle starting from given operation using DFS
        /// </summary>
        private List<RenameOperation>? FindCycle(
            RenameOperation startOp,
            Dictionary<string, RenameOperation> nameToOp,
            HashSet<string> visited,
            HashSet<string> currentPath,
            List<RenameOperation> path)
        {
            var originalFullName = Path.Combine(startOp.Directory, startOp.OriginalName);
            var newFullName = Path.Combine(startOp.Directory, startOp.NewName);

            // If new name is same as original, no cycle
            if (string.Equals(originalFullName, newFullName, StringComparison.OrdinalIgnoreCase))
            {
                visited.Add(originalFullName);
                return null;
            }

            // If new name doesn't exist in operations, no cycle
            if (!nameToOp.ContainsKey(newFullName))
            {
                visited.Add(originalFullName);
                return null;
            }

            // Check if we're already in current path (cycle detected)
            if (currentPath.Contains(originalFullName))
            {
                // Found a cycle, extract cycle from path
                var cycleStartIndex = path.FindIndex(op => 
                    string.Equals(Path.Combine(op.Directory, op.OriginalName), originalFullName, StringComparison.OrdinalIgnoreCase));
                if (cycleStartIndex >= 0)
                {
                    var cycle = path.Skip(cycleStartIndex).ToList();
                    cycle.Add(startOp); // Add current node to complete cycle
                    return cycle;
                }
                return new List<RenameOperation> { startOp };
            }

            // If already visited and not in cycle, no cycle from here
            if (visited.Contains(originalFullName))
            {
                return null;
            }

            // Mark as being processed
            currentPath.Add(originalFullName);
            path.Add(startOp);

            // Follow the chain
            var nextOp = nameToOp[newFullName];
            var result = FindCycle(nextOp, nameToOp, visited, currentPath, path);

            // Remove from current path
            currentPath.Remove(originalFullName);
            path.RemoveAt(path.Count - 1);
            visited.Add(originalFullName);

            return result;
        }

        /// <summary>
        /// Execute rename for a cycle using temporary names
        /// </summary>
        private void ExecuteCycleRename(List<RenameOperation> cycle, RenameResult result)
        {
            if (cycle.Count == 0)
                return;

            // Step 1: Rename all files in cycle to temporary names
            var tempNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tempOps = new List<(RenameOperation op, string tempName)>();

            foreach (var op in cycle)
            {
                var originalFullName = Path.Combine(op.Directory, op.OriginalName);
                var tempName = GenerateTempName(op.Directory, op.OriginalName, cycle);
                var tempFullName = Path.Combine(op.Directory, tempName);

                tempNames[originalFullName] = tempName;
                tempOps.Add((op, tempName));

                if (TryRenameFile(originalFullName, tempFullName))
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Failed to rename {op.OriginalName} to temporary name");
                    return; // Abort cycle rename if any step fails
                }
            }

            // Step 2: Rename from temporary names to final names
            foreach (var (op, tempName) in tempOps)
            {
                var tempFullName = Path.Combine(op.Directory, tempName);
                var finalFullName = Path.Combine(op.Directory, op.NewName);

                if (TryRenameFile(tempFullName, finalFullName))
                {
                    // Already counted in step 1, don't double count
                }
                else
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Failed to rename {tempName} to {op.NewName}");
                }
            }
        }

        /// <summary>
        /// Generate a temporary name that doesn't conflict with any target names
        /// Uses GUID to ensure uniqueness, avoiding loops
        /// </summary>
        private string GenerateTempName(string directory, string originalName, List<RenameOperation> cycle)
        {
            var allTargetNames = new HashSet<string>(cycle.Select(op => op.NewName), StringComparer.OrdinalIgnoreCase);
            var extension = Path.GetExtension(originalName);
            
            // Use GUID directly to ensure uniqueness, check once for conflicts
            string tempName;
            string tempFullName;
            int attempts = 0;
            const int maxAttempts = 10; // Safety limit, though GUID collision is extremely rare

            do
            {
                var guid = Guid.NewGuid().ToString("N"); // 32-character hex string
                tempName = $".temp_{guid}{extension}";
                tempFullName = Path.Combine(directory, tempName);
                attempts++;
            }
            while (attempts < maxAttempts && 
                   (allTargetNames.Contains(tempName) || File.Exists(tempFullName)));

            // If still conflicts after max attempts (extremely unlikely), use timestamp
            if (attempts >= maxAttempts && (allTargetNames.Contains(tempName) || File.Exists(tempFullName)))
            {
                var timestamp = DateTime.Now.Ticks;
                tempName = $".temp_{timestamp}{extension}";
            }

            return tempName;
        }

        /// <summary>
        /// Try to rename a single operation (direct rename, no cycle)
        /// </summary>
        private bool TryRename(RenameOperation op, RenameResult result)
        {
            var originalFullName = Path.Combine(op.Directory, op.OriginalName);
            var newFullName = Path.Combine(op.Directory, op.NewName);

            // Skip if names are the same
            if (string.Equals(originalFullName, newFullName, StringComparison.OrdinalIgnoreCase))
            {
                result.SkippedCount++;
                return false;
            }

            // Check if target already exists
            if (File.Exists(newFullName))
            {
                result.SkippedCount++;
                result.Errors.Add($"Target file already exists: {op.NewName}");
                return false;
            }

            return TryRenameFile(originalFullName, newFullName);
        }

        /// <summary>
        /// Try to rename a file
        /// </summary>
        private bool TryRenameFile(string sourcePath, string targetPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                    return false;

                File.Move(sourcePath, targetPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
