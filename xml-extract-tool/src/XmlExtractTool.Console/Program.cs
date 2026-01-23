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
        static void Main(string[] args)
        {
            Console.WriteLine("XML 四元数检测工具 - 控制台测试");
            Console.WriteLine("=====================================\n");

            // Find data folder by searching upward from current directory
            // or use command line argument if provided
            string? dataDir;
            
            if (args.Length > 0 && Directory.Exists(args[0]))
            {
                dataDir = Path.GetFullPath(args[0]);
            }
            else
            {
                // Start from current working directory (usually project root when run from IDE)
                var startDir = Directory.GetCurrentDirectory();
                dataDir = FindDataFolder(startDir);
                
                if (dataDir == null)
                {
                    // Try from executable directory
                    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    dataDir = FindDataFolder(exeDir);
                }
            }

            if (dataDir == null || !Directory.Exists(dataDir))
            {
                Console.WriteLine("错误: 找不到 data 文件夹");
                Console.WriteLine("\n使用方法:");
                Console.WriteLine("  1. 在项目根目录 (xml-extract-tool) 运行程序");
                Console.WriteLine("  2. 或通过命令行参数指定 data 文件夹路径:");
                Console.WriteLine("     XmlExtractTool.Console.exe <data文件夹路径>");
                Console.WriteLine("\n示例:");
                Console.WriteLine("  XmlExtractTool.Console.exe \"D:\\source\\repos\\quicker\\wpf-demos\\xml-extract-tool\\data\"");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"使用 data 文件夹: {dataDir}\n");

            var checker = new XmlQuaternionChecker();

            // Test 0度 folder
            var zeroDegreeDir = Path.Combine(dataDir, "0度");
            if (Directory.Exists(zeroDegreeDir))
            {
                Console.WriteLine("测试 0度 文件夹:");
                Console.WriteLine("-------------------");
                TestFolder(checker, zeroDegreeDir);
                Console.WriteLine();
            }

            // Test 90度 folder
            var ninetyDegreeDir = Path.Combine(dataDir, "90度");
            if (Directory.Exists(ninetyDegreeDir))
            {
                Console.WriteLine("测试 90度 文件夹:");
                Console.WriteLine("-------------------");
                TestFolder(checker, ninetyDegreeDir);
                Console.WriteLine();
            }

            Console.WriteLine("测试完成！按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// Find data folder by searching upward from the given directory
        /// </summary>
        static string? FindDataFolder(string startDir)
        {
            var current = new DirectoryInfo(startDir);
            
            // Search up to 10 levels
            for (int i = 0; i < 10 && current != null; i++)
            {
                var dataPath = Path.Combine(current.FullName, "data");
                if (Directory.Exists(dataPath))
                {
                    // Verify it contains the expected subdirectories
                    var zeroDegreePath = Path.Combine(dataPath, "0度");
                    var ninetyDegreePath = Path.Combine(dataPath, "90度");
                    if (Directory.Exists(zeroDegreePath) || Directory.Exists(ninetyDegreePath))
                    {
                        return dataPath;
                    }
                }
                
                current = current.Parent;
            }
            
            return null;
        }

        static void TestFolder(XmlQuaternionChecker checker, string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.upe", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                Console.WriteLine($"  未找到 .upe 文件");
                return;
            }

            foreach (var file in files)
            {
                Console.WriteLine($"\n  文件: {Path.GetFileName(file)}");
                try
                {
                    var invalidNodes = checker.CheckQuaternions(file);
                    var allNodes = checker.ParseNodes(file);

                    Console.WriteLine($"    总节点数: {allNodes.Count}");
                    Console.WriteLine($"    不符合 90 度旋转条件的节点数: {invalidNodes.Count}");
                    Console.WriteLine();

                    // Show all nodes with their rotation angles
                    Console.WriteLine($"    所有节点的旋转角度:");
                    foreach (var nodeInfo in allNodes)
                    {
                        var is90Degree = nodeInfo.Is90DegreeRotation;
                        var statusIcon = is90Degree ? "[OK]" : "[X]";
                        
                        // Calculate and display rotation angle
                        string angleStr = "N/A";
                        if (nodeInfo.Quaternion.HasValue)
                        {
                            var angle = nodeInfo.Quaternion.Value.GetRotationAngleDegrees();
                            if (!double.IsNaN(angle))
                            {
                                angleStr = $"{angle:F2}度";
                            }
                        }
                        
                        // Highlight invalid nodes
                        if (!is90Degree)
                        {
                            Console.WriteLine($"      {statusIcon} [{nodeInfo.Name}] -> {angleStr} (不符合条件)");
                        }
                        else
                        {
                            Console.WriteLine($"      {statusIcon} {nodeInfo.Name} -> {angleStr}");
                        }
                    }
                    
                    // Show summary statistics
                    var validCount = allNodes.Count(n => n.Is90DegreeRotation);
                    var invalidCount = allNodes.Count(n => !n.Is90DegreeRotation);
                    Console.WriteLine($"\n    统计: {validCount} 个符合条件 (90°), {invalidCount} 个不符合条件 (非90°)");
                    
                    if (invalidNodes.Count > 0)
                    {
                        Console.WriteLine($"\n    不符合条件的节点名称列表:");
                        foreach (var nodeName in invalidNodes)
                        {
                            Console.WriteLine($"      - {nodeName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    错误: {ex.Message}");
                }
            }
        }
    }
}
