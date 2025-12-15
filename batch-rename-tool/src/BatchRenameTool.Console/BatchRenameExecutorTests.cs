using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatchRenameTool.Services;

namespace BatchRenameTool
{
    /// <summary>
    /// Tests for BatchRenameExecutor
    /// </summary>
    public class BatchRenameExecutorTests
    {
        private readonly string _baseDir = Path.Combine(Path.GetTempPath(), "BatchRenameExecutorTests");

        public void RunAllTests()
        {
            Console.WriteLine("=== Batch Rename Executor Test ===");
            Console.WriteLine();

            try
            {
                TestDirectRename();
                TestReverseChain();
                TestCycleRename();
                TestComplexCycle();
                TestMixed();
                TestShift123To234();

                Console.WriteLine("All tests completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private string PrepareDirectory(string name, params string[] files)
        {
            var dir = Path.Combine(_baseDir, name);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);

            foreach (var file in files)
            {
                File.WriteAllText(Path.Combine(dir, file), $"Content of {file}");
            }

            return dir;
        }

        private void TestDirectRename()
        {
            Console.WriteLine("Test 1: Direct rename (no cycle)");
            var dir = PrepareDirectory("direct", "1.txt", "2.txt");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new(Path.Combine(dir, "1.txt"), "1_renamed.txt"),
                new(Path.Combine(dir, "2.txt"), "2_renamed.txt")
            };

            PrintOperations(operations);
            var result = executor.Execute(operations);
            PrintResult(result);
            VerifyFiles(operations);
            Console.WriteLine();
        }

        private void TestReverseChain()
        {
            Console.WriteLine("Test 2: Reverse chain (i+1 -> i)");
            var dir = PrepareDirectory("reverse_chain", "1.txt", "2.txt", "3.txt");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new(Path.Combine(dir, "2.txt"), "1.txt"),
                new(Path.Combine(dir, "3.txt"), "2.txt")
            };

            PrintOperations(operations);
            var result = executor.Execute(operations);
            PrintResult(result);
            VerifyFiles(operations);
            Console.WriteLine();
        }

        private void TestCycleRename()
        {
            Console.WriteLine("Test 3: Cycle rename (1->2, 2->1)");
            var dir = PrepareDirectory("cycle", "1.txt", "2.txt");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new(Path.Combine(dir, "1.txt"), "2.txt"),
                new(Path.Combine(dir, "2.txt"), "1.txt")
            };

            PrintOperations(operations);
            var result = executor.Execute(operations);
            PrintResult(result);
            VerifyFiles(operations);
            Console.WriteLine();
        }

        private void TestComplexCycle()
        {
            Console.WriteLine("Test 4: Complex cycle (1->2, 2->3, 3->1)");
            var dir = PrepareDirectory("complex", "1.txt", "2.txt", "3.txt");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new(Path.Combine(dir, "1.txt"), "2.txt"),
                new(Path.Combine(dir, "2.txt"), "3.txt"),
                new(Path.Combine(dir, "3.txt"), "1.txt")
            };

            PrintOperations(operations);
            var result = executor.Execute(operations);
            PrintResult(result);
            VerifyFiles(operations);
            Console.WriteLine();
        }

        private void TestMixed()
        {
            Console.WriteLine("Test 5: Mixed (direct + cycle)");
            var dir = PrepareDirectory("mixed", "1.txt", "2.txt", "3.txt", "4.txt");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new(Path.Combine(dir, "1.txt"), "1_direct.txt"),
                new(Path.Combine(dir, "2.txt"), "3.txt"),
                new(Path.Combine(dir, "3.txt"), "2.txt")
            };

            PrintOperations(operations);
            var result = executor.Execute(operations);
            PrintResult(result);
            VerifyFiles(operations);
            Console.WriteLine();
        }

        private void TestShift123To234()
        {
            Console.WriteLine("Test 6: Shift chain 1->2, 2->3, 3->4");
            var dir = PrepareDirectory("shift123to234", "1.txt", "2.txt", "3.txt");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new(Path.Combine(dir, "1.txt"), "2.txt"),
                new(Path.Combine(dir, "2.txt"), "3.txt"),
                new(Path.Combine(dir, "3.txt"), "4.txt")
            };

            PrintOperations(operations);
            var result = executor.Execute(operations);
            PrintResult(result);
            VerifyFiles(operations);
            Console.WriteLine();
        }

        private static void PrintOperations(IEnumerable<BatchRenameExecutor.RenameOperation> operations)
        {
            Console.WriteLine("Operations:");
            foreach (var op in operations)
            {
                Console.WriteLine($"  {op.OriginalName} -> {op.NewName}");
            }
        }

        private static void PrintResult(BatchRenameExecutor.RenameResult result)
        {
            Console.WriteLine($"Result: Success={result.SuccessCount}, Skipped={result.SkippedCount}, Errors={result.ErrorCount}");
            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }
        }

        private static void VerifyFiles(List<BatchRenameExecutor.RenameOperation> operations)
        {
            Console.WriteLine("Verification:");
            foreach (var op in operations)
            {
                var originalPath = Path.Combine(op.Directory, op.OriginalName);
                var newPath = Path.Combine(op.Directory, op.NewName);

                var originalExists = File.Exists(originalPath);
                var newExists = File.Exists(newPath);

                Console.WriteLine($"  {op.OriginalName}: exists={originalExists}");
                Console.WriteLine($"  {op.NewName}: exists={newExists}");

                if (newExists)
                {
                    var content = File.ReadAllText(newPath);
                    var preview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
                    Console.WriteLine($"    Content: {preview}");
                }
            }
        }
    }
}
