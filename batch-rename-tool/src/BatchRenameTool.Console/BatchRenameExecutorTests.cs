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
        private const string TestDirectory = @"C:\Users\ldy\Desktop\cmm";

        public void RunAllTests()
        {
            Console.WriteLine("=== Batch Rename Executor Test ===");
            Console.WriteLine();

            try
            {
                // Setup test files
                SetupTestFiles();

                // Test 1: Direct rename (no cycle)
                Console.WriteLine("Test 1: Direct rename (no cycle)");
                TestDirectRename();
                Console.WriteLine();

                // Test 2: Cycle rename (A->B, B->A)
                Console.WriteLine("Test 2: Cycle rename (A->B, B->A)");
                TestCycleRename();
                Console.WriteLine();

                // Test 3: Complex cycle (A->B, B->C, C->A)
                Console.WriteLine("Test 3: Complex cycle (A->B, B->C, C->A)");
                TestComplexCycle();
                Console.WriteLine();

                // Test 4: Mixed (some direct, some cycle)
                Console.WriteLine("Test 4: Mixed (some direct, some cycle)");
                TestMixed();
                Console.WriteLine();

                Console.WriteLine("All tests completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void SetupTestFiles()
        {
            Console.WriteLine($"Setting up test files in {TestDirectory}...");

            // Clean up existing test files
            if (Directory.Exists(TestDirectory))
            {
                var existingFiles = Directory.GetFiles(TestDirectory, "test_*.txt");
                foreach (var file in existingFiles)
                {
                    try { File.Delete(file); } catch { }
                }
            }
            else
            {
                Directory.CreateDirectory(TestDirectory);
            }

            // Create test files
            var testFiles = new[] { "test_A.txt", "test_B.txt", "test_C.txt", "test_D.txt", "test_E.txt" };
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(TestDirectory, fileName);
                File.WriteAllText(filePath, $"Content of {fileName}");
            }

            Console.WriteLine($"Created {testFiles.Length} test files.");
        }

        private void TestDirectRename()
        {
            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_D.txt",
                    NewName = "test_D_renamed.txt"
                },
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_E.txt",
                    NewName = "test_E_renamed.txt"
                }
            };

            Console.WriteLine("Operations:");
            foreach (var op in operations)
            {
                Console.WriteLine($"  {op.OriginalName} -> {op.NewName}");
            }

            var result = executor.Execute(operations);

            Console.WriteLine($"Result: Success={result.SuccessCount}, Skipped={result.SkippedCount}, Errors={result.ErrorCount}");
            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }

            // Verify
            VerifyFiles(operations);
        }

        private void TestCycleRename()
        {
            // Recreate test files for this test
            var fileA = Path.Combine(TestDirectory, "test_A.txt");
            var fileB = Path.Combine(TestDirectory, "test_B.txt");
            if (!File.Exists(fileA)) File.WriteAllText(fileA, "Content A");
            if (!File.Exists(fileB)) File.WriteAllText(fileB, "Content B");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_A.txt",
                    NewName = "test_B.txt"
                },
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_B.txt",
                    NewName = "test_A.txt"
                }
            };

            Console.WriteLine("Operations (cycle):");
            foreach (var op in operations)
            {
                Console.WriteLine($"  {op.OriginalName} -> {op.NewName}");
            }

            var result = executor.Execute(operations);

            Console.WriteLine($"Result: Success={result.SuccessCount}, Skipped={result.SkippedCount}, Errors={result.ErrorCount}");
            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }

            // Verify - files should be swapped (reuse fileA and fileB variables)
            Console.WriteLine($"  File A exists: {File.Exists(fileA)}");
            Console.WriteLine($"  File B exists: {File.Exists(fileB)}");
        }

        private void TestComplexCycle()
        {
            // First recreate test files
            var fileA = Path.Combine(TestDirectory, "test_A.txt");
            var fileB = Path.Combine(TestDirectory, "test_B.txt");
            var fileC = Path.Combine(TestDirectory, "test_C.txt");

            if (!File.Exists(fileA)) File.WriteAllText(fileA, "Content A");
            if (!File.Exists(fileB)) File.WriteAllText(fileB, "Content B");
            if (!File.Exists(fileC)) File.WriteAllText(fileC, "Content C");

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_A.txt",
                    NewName = "test_B.txt"
                },
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_B.txt",
                    NewName = "test_C.txt"
                },
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_C.txt",
                    NewName = "test_A.txt"
                }
            };

            Console.WriteLine("Operations (complex cycle):");
            foreach (var op in operations)
            {
                Console.WriteLine($"  {op.OriginalName} -> {op.NewName}");
            }

            var result = executor.Execute(operations);

            Console.WriteLine($"Result: Success={result.SuccessCount}, Skipped={result.SkippedCount}, Errors={result.ErrorCount}");
            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }

            // Verify
            Console.WriteLine($"  File A exists: {File.Exists(fileA)}");
            Console.WriteLine($"  File B exists: {File.Exists(fileB)}");
            Console.WriteLine($"  File C exists: {File.Exists(fileC)}");
        }

        private void TestMixed()
        {
            // Recreate test files
            var testFiles = new[] { "test_D.txt", "test_E.txt", "test_F.txt", "test_G.txt" };
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(TestDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, $"Content of {fileName}");
                }
            }

            var executor = new BatchRenameExecutor();
            var operations = new List<BatchRenameExecutor.RenameOperation>
            {
                // Direct rename
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_D.txt",
                    NewName = "test_D_direct.txt"
                },
                // Cycle
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_F.txt",
                    NewName = "test_G.txt"
                },
                new BatchRenameExecutor.RenameOperation
                {
                    Directory = TestDirectory,
                    OriginalName = "test_G.txt",
                    NewName = "test_F.txt"
                }
            };

            Console.WriteLine("Operations (mixed):");
            foreach (var op in operations)
            {
                Console.WriteLine($"  {op.OriginalName} -> {op.NewName}");
            }

            var result = executor.Execute(operations);

            Console.WriteLine($"Result: Success={result.SuccessCount}, Skipped={result.SkippedCount}, Errors={result.ErrorCount}");
            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }

            // Verify
            VerifyFiles(operations);
        }

        private void VerifyFiles(List<BatchRenameExecutor.RenameOperation> operations)
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
                    Console.WriteLine($"    Content: {content.Substring(0, Math.Min(50, content.Length))}...");
                }
            }
        }
    }
}
