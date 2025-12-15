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
        private class RenameNode
        {
            public RenameOperation Operation { get; set; } = null!;
            public RenameNode? Next { get; set; }
            public int InDegree { get; set; }
            public bool Visited { get; set; }
            public string OriginalFull => Path.Combine(Operation.Directory, Operation.OriginalName);
            public string TargetFull => Path.Combine(Operation.Directory, Operation.NewName);
        }

        /// <summary>
        /// Result status for a rename operation
        /// </summary>
        public enum OperationStatus
        {
            Pending,
            Success,
            Skipped,
            Error
        }

        /// <summary>
        /// Result information for a rename operation
        /// </summary>
        public class OperationResult
        {
            public OperationStatus Status { get; set; } = OperationStatus.Pending;
            public string? ErrorReason { get; set; }

            public static OperationResult Success() => new() { Status = OperationStatus.Success };
            public static OperationResult Skipped() => new() { Status = OperationStatus.Skipped };
            public static OperationResult Error(string reason) => new() { Status = OperationStatus.Error, ErrorReason = reason };
        }

        /// <summary>
        /// Represents a rename operation
        /// </summary>
        public class RenameOperation
        {
            public string OriginalPath { get; }
            public string NewName { get; }
            public OperationResult Result { get; set; } = new OperationResult();

            public RenameOperation(string originalFullPath, string newName)
            {
                OriginalPath = originalFullPath;
                NewName = newName;
            }

            // Computed properties from OriginalPath
            public string Directory => Path.GetDirectoryName(OriginalPath) ?? "";
            public string OriginalName => Path.GetFileName(OriginalPath);
        }

        /// <summary>
        /// Result of rename execution
        /// </summary>
        public class RenameResult
        {
            public List<RenameOperation> Operations { get; }
            public int SuccessCount { get; }
            public int SkippedCount { get; }
            public int ErrorCount { get; }
            public List<string> Errors { get; }

            public RenameResult(List<RenameOperation>? operations = null)
            {
                Operations = operations ?? new List<RenameOperation>();
                
                // 统计一次
                SuccessCount = Operations.Count(o => o.Result.Status == OperationStatus.Success);
                SkippedCount = Operations.Count(o => o.Result.Status == OperationStatus.Skipped);
                ErrorCount = Operations.Count(o => o.Result.Status == OperationStatus.Error);
                
                Errors = Operations
                    .Where(o => o.Result.Status == OperationStatus.Error)
                    .Select(o => $"{o.OriginalName} -> {o.NewName}: {o.Result.ErrorReason ?? "未知错误"}")
                    .ToList();
            }
        }

        /// <summary>
        /// Exception thrown when validation fails before rename execution
        /// </summary>
        public class RenameValidationException : Exception
        {
            public RenameValidationException(string message) : base(message) { }
        }

        /// <summary>
        /// Execute batch rename operations
        /// </summary>
        /// <param name="operations">List of rename operations</param>
        /// <param name="progress">Progress reporter (current index, total count, current file name)</param>
        /// <returns>Rename result</returns>
        public RenameResult Execute(IEnumerable<RenameOperation> operations, IProgress<(int current, int total, string fileName)>? progress = null)
        {
            var allOperations = new List<RenameOperation>();
            var opList = new List<RenameOperation>();
            foreach (var op in operations)
            {
                allOperations.Add(op);
                var src = Path.Combine(op.Directory, op.OriginalName);
                var dst = Path.Combine(op.Directory, op.NewName);
                if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
                {
                    op.Result = OperationResult.Skipped();
                    continue;
                }
                opList.Add(op);
            }

            if (opList.Count == 0)
                return new RenameResult(allOperations);

            // Validate operations before execution
            ValidateOperations(opList);

            // 构建节点映射
            var nodeMap = new Dictionary<string, RenameNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var op in opList)
            {
                var originalFull = Path.Combine(op.Directory, op.OriginalName);
                nodeMap[originalFull] = new RenameNode { Operation = op };
            }

            // 连接 next 并计算入度
            foreach (var node in nodeMap.Values)
            {
                if (nodeMap.TryGetValue(node.TargetFull, out var next))
                {
                    node.Next = next;
                    next.InDegree++;
                }
            }

            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int currentIndex = 0;
            int totalCount = opList.Count;

            // 处理链：入度为 0 的节点作为 head
            foreach (var node in nodeMap.Values.Where(n => n.InDegree == 0))
            {
                var chain = new List<RenameOperation>();
                var cur = node;
                while (cur != null && !cur.Visited)
                {
                    chain.Add(cur.Operation);
                    cur.Visited = true;
                    processed.Add(cur.OriginalFull);
                    cur = cur.Next;
                }
                ProcessChain(chain, progress, ref currentIndex, totalCount);
            }

            // 处理剩余环
            foreach (var node in nodeMap.Values.Where(n => !n.Visited))
            {
                if (node.Visited)
                    continue;

                var order = new List<RenameNode>();
                var inPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var cur = node;
                while (cur != null && !cur.Visited)
                {
                    order.Add(cur);
                    inPath[cur.OriginalFull] = order.Count - 1;
                    cur.Visited = true;

                    if (cur.Next == null)
                    {
                        // 非闭合，按链处理
                        var chainOps = order.Select(n => n.Operation).ToList();
                        ProcessChain(chainOps, progress, ref currentIndex, totalCount);
                        break;
                    }

                    if (inPath.TryGetValue(cur.Next.OriginalFull, out var idx))
                    {
                        // 找到环
                        var cycleOps = order.Skip(idx).Select(n => n.Operation).ToList();
                        ProcessCycle(cycleOps, progress, ref currentIndex, totalCount);
                        break;
                    }

                    cur = cur.Next;
                }
            }

            return new RenameResult(allOperations);
        }

        private void ProcessChain(List<RenameOperation> chain, IProgress<(int current, int total, string fileName)>? progress, ref int currentIndex, int totalCount)
        {
            if (chain.Count == 0)
                return;

            // 若尾节点目标已被占用（且不在操作列表中），先腾空到临时文件，避免占用
            var tail = chain[chain.Count - 1];
            var tailTargetFull = Path.Combine(tail.Directory, tail.NewName);
            if (File.Exists(tailTargetFull))
            {
                var tempFull = GenerateTempFullPath(tail.Directory, tail.NewName);
                progress?.Report((currentIndex, totalCount, tail.NewName));
                if (!TryRenameFile(tailTargetFull, tempFull, out var tempErr))
                {
                    // 腾空失败，标记所有链中的操作为错误
                    foreach (var op in chain)
                    {
                        op.Result = OperationResult.Error($"腾空目标失败: {tempErr ?? "未知错误"}");
                    }
                    return;
                }
                currentIndex++;
            }

            // 反向执行：tail->...->head
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                var op = chain[i];
                progress?.Report((currentIndex, totalCount, op.OriginalName));
                TryRename(op);
                currentIndex++;
            }
        }

        private void ProcessCycle(List<RenameOperation> cycle, IProgress<(int current, int total, string fileName)>? progress, ref int currentIndex, int totalCount)
        {
            if (cycle.Count == 0)
                return;

            var first = cycle[0];
            var firstSrcFull = Path.Combine(first.Directory, first.OriginalName);
            var tempName = $".temp_{Guid.NewGuid():N}{Path.GetExtension(first.OriginalName)}";
            var tempFull = Path.Combine(first.Directory, tempName);

            // 1) first -> temp
            progress?.Report((currentIndex, totalCount, first.OriginalName));
            if (!TryRenameFile(firstSrcFull, tempFull, out var tempErr))
            {
                // 失败，标记所有环中的操作为错误
                foreach (var op in cycle)
                {
                    op.Result = OperationResult.Error($"重命名为临时文件失败: {tempErr ?? "未知错误"}");
                }
                return;
            }
            currentIndex++;

            // 2) 反向执行剩余节点（跳过 first）
            for (int i = cycle.Count - 1; i >= 1; i--)
            {
                var op = cycle[i];
                progress?.Report((currentIndex, totalCount, op.OriginalName));
                TryRename(op);
                currentIndex++;
            }

            // 3) temp -> first target
            var finalFull = Path.Combine(first.Directory, first.NewName);
            progress?.Report((currentIndex, totalCount, first.OriginalName));
            if (TryRenameFile(tempFull, finalFull, out var finalErr))
            {
                first.Result = OperationResult.Success();
            }
            else
            {
                first.Result = OperationResult.Error($"从临时文件重命名失败: {finalErr ?? "未知错误"}");
            }
            currentIndex++;
        }

        /// <summary>
        /// Validate operations before execution
        /// Throws RenameValidationException if validation fails
        /// </summary>
        private void ValidateOperations(List<RenameOperation> operations)
        {
            // 1. Check for duplicate target names
            var duplicateGroups = operations
                .GroupBy(op => Path.Combine(op.Directory, op.NewName), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateGroups.Count > 0)
            {
                var duplicateCount = duplicateGroups.Sum(g => g.Count());
                throw new RenameValidationException($"存在 {duplicateCount} 个重复的目标文件名，请调整后再重试。");
            }

            // 2. Check for conflicts with existing files on disk
            var sourceSet = new HashSet<string>(operations.Select(op => 
                Path.Combine(op.Directory, op.OriginalName)), StringComparer.OrdinalIgnoreCase);

            var conflicts = new List<(string OriginalName, string NewName)>();
            foreach (var op in operations)
            {
                var targetFull = Path.Combine(op.Directory, op.NewName);
                // If target file exists and is not in the source set (not part of the rename chain/cycle), it's a conflict
                if (!sourceSet.Contains(targetFull) && File.Exists(targetFull))
                {
                    conflicts.Add((op.OriginalName, op.NewName));
                }
            }

            if (conflicts.Count > 0)
            {
                var sampleNames = conflicts.Take(5)
                    .Select(x => $"{x.OriginalName} -> {x.NewName}");
                var message = conflicts.Count <= 5
                    ? $"目标文件已存在，无法覆盖：{string.Join("，", sampleNames)}。请调整名称后重试。"
                    : $"目标文件已存在，无法覆盖（例如：{string.Join("，", sampleNames)} 等 {conflicts.Count} 个文件）。请调整名称后重试。";
                throw new RenameValidationException(message);
            }
        }

        private string GenerateTempFullPath(string directory, string targetName)
        {
            var ext = Path.GetExtension(targetName);
            string tempFull;
            do
            {
                tempFull = Path.Combine(directory, $".temp_{Guid.NewGuid():N}{ext}");
            } while (File.Exists(tempFull));
            return tempFull;
        }

        /// <summary>
        /// Try to rename a single operation (direct rename, no cycle)
        /// </summary>
        private bool TryRename(RenameOperation op)
        {
            var originalFullName = Path.Combine(op.Directory, op.OriginalName);
            var newFullName = Path.Combine(op.Directory, op.NewName);

            // Skip if names are the same
            if (string.Equals(originalFullName, newFullName, StringComparison.OrdinalIgnoreCase))
            {
                op.Result = OperationResult.Skipped();
                return false;
            }

            // Check if target already exists
            if (File.Exists(newFullName))
            {
                op.Result = OperationResult.Error("目标文件已存在");
                return false;
            }

            if (TryRenameFile(originalFullName, newFullName, out string? errorReason))
            {
                op.Result = OperationResult.Success();
                return true;
            }
            else
            {
                op.Result = OperationResult.Error(errorReason ?? "未知错误");
                return false;
            }
        }

        /// <summary>
        /// Try to rename a file
        /// </summary>
        private bool TryRenameFile(string sourcePath, string targetPath, out string? errorReason)
        {
            errorReason = null;
            try
            {
                if (!File.Exists(sourcePath))
                {
                    errorReason = "源文件不存在";
                    System.Diagnostics.Debug.WriteLine($"Source file does not exist: {sourcePath}");
                    return false;
                }

                // Check if target already exists
                if (File.Exists(targetPath))
                {
                    errorReason = "目标文件已存在";
                    System.Diagnostics.Debug.WriteLine($"Target file already exists: {targetPath}");
                    return false;
                }

                File.Move(sourcePath, targetPath);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errorReason = $"权限不足: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Rename failed (UnauthorizedAccess): {sourcePath} -> {targetPath}, Error: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                errorReason = $"IO错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Rename failed (IOException): {sourcePath} -> {targetPath}, Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                errorReason = $"错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Rename failed: {sourcePath} -> {targetPath}, Error: {ex.Message}");
                return false;
            }
        }
    }
}
