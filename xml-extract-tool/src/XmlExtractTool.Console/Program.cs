using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XmlExtractTool.Models;
using XmlExtractTool.Services;

namespace XmlExtractTool
{
    class Program
    {
        /// <summary>
        /// 从 data 子目录名称解析期望结果：
        /// 含「正确」或「_正确」→ 期望 0 项不符合（检测通过）；
        /// 含「错误」或「_错误」→ 期望至少 1 项不符合（检测应发现问题）。
        /// </summary>
        static bool? GetExpectedPassFromFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return null;
            if (folderName.Contains("正确")) return true;   // 期望：无错误
            if (folderName.Contains("错误")) return false;  // 期望：有错误
            return null;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("XML 节点检测工具 - 控制台测试 (data 目录)");
            Console.WriteLine("==========================================\n");

            string? dataDir = ResolveDataDir(args);
            if (dataDir == null || !Directory.Exists(dataDir))
            {
                Console.WriteLine("错误: 找不到 data 文件夹");
                Console.WriteLine("\n使用方法:");
                Console.WriteLine("  1. 在 xml-extract-tool 目录下运行程序");
                Console.WriteLine("  2. 或通过命令行参数指定 data 路径:");
                Console.WriteLine("     XmlExtractTool.Console.exe <data路径>");
                Console.WriteLine("\n示例:");
                Console.WriteLine("  XmlExtractTool.Console.exe \"D:\\...\\xml-extract-tool\\data\"");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"data 目录: {dataDir}\n");
            Console.WriteLine("说明: 目录名含「正确」期望 0 项不符合；含「错误」期望有不符合项。\n");

            var settings = new CheckerSettings();
            try { settings.Load(); } catch { /* use defaults */ }
            var checker = new XmlNodeChecker(settings);

            var subDirs = Directory.GetDirectories(dataDir).OrderBy(d => Path.GetFileName(d)).ToList();
            if (subDirs.Count == 0)
            {
                Console.WriteLine("data 下没有子文件夹，将直接检测 data 目录内的文件。\n");
                subDirs.Add(dataDir);
            }

            int testPass = 0, testFail = 0;
            var allResults = new List<(string FolderName, bool? ExpectPass, List<CheckResultItem> Items)>();

            foreach (var subDir in subDirs)
            {
                var folderName = Path.GetFileName(subDir);
                bool? expectPass = GetExpectedPassFromFolderName(folderName);
                var items = checker.CheckFolder(subDir);
                allResults.Add((folderName, expectPass, items));
            }

            foreach (var (folderName, expectPass, items) in allResults)
            {
                bool actualPass = items.Count == 0;
                bool? expectation = expectPass;
                bool testOk = !expectation.HasValue
                    ? actualPass
                    : (expectation.Value ? actualPass : !actualPass);

                if (testOk) testPass++; else testFail++;

                Console.WriteLine($"【{folderName}】");
                string expectStr = expectation == null ? "未标注" : (expectation.Value ? "期望通过" : "期望不通过");
                Console.WriteLine($"  期望: {expectStr}");
                Console.WriteLine(actualPass ? "  实际: 通过 (0 项不符合)" : $"  实际: 不通过 (共 {items.Count} 项不符合)");
                Console.WriteLine(testOk ? "  测试: 通过" : "  测试: 失败 (与期望不符)");
                if (items.Count > 0)
                {
                    foreach (var r in items)
                        Console.WriteLine($"    ---\n    文件名: {r.FileName}\n    Node Name: {r.NodeName}\n    Parent: {r.Parent}");
                    Console.WriteLine();
                }
                Console.WriteLine();
            }

            Console.WriteLine("==========================================");
            Console.WriteLine($"汇总: 测试通过 {testPass} 个目录, 测试失败 {testFail} 个目录 (与目录名期望对比)");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static string? ResolveDataDir(string[] args)
        {
            if (args.Length > 0 && Directory.Exists(args[0]))
                return Path.GetFullPath(args[0]);

            var startDir = Directory.GetCurrentDirectory();
            var dataPath = FindDataFolder(startDir);
            if (dataPath != null) return dataPath;

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return FindDataFolder(exeDir);
        }

        static string? FindDataFolder(string startDir)
        {
            var current = new DirectoryInfo(startDir);
            for (int i = 0; i < 10 && current != null; i++)
            {
                var dataPath = Path.Combine(current.FullName, "data");
                if (Directory.Exists(dataPath))
                    return dataPath;
                current = current.Parent;
            }
            return null;
        }
    }
}
