using System;
using System.Diagnostics;
using System.Linq;
using BatchRenameTool.Template.Compiler;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;

namespace BatchRenameTool
{
    /// <summary>
    /// Performance tests for compiled template execution
    /// </summary>
    public class PerformanceTests
    {
        private readonly TemplateParser _parser;
        private readonly TemplateEvaluator _evaluator;
        private readonly TemplateCompiler _compiler;
        private const int Iterations = 10000;

        public PerformanceTests()
        {
            _parser = new TemplateParser(Enumerable.Empty<Type>());
            _evaluator = new TemplateEvaluator();
            _compiler = new TemplateCompiler();
        }

        public void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("模板编译性能测试");
            Console.WriteLine("========================================");
            Console.WriteLine($"执行次数: {Iterations:N0}");
            Console.WriteLine();

            // Test different template patterns
            var testCases = new[]
            {
                new { Name = "简单变量", Template = "{name}.{ext}" },
                new { Name = "格式化索引", Template = "{name}_{i:001}.{ext}" },
                new { Name = "表达式", Template = "{name}_{2i+1:00}.{ext}" },
                new { Name = "方法调用", Template = "{name.upper()}_{i:00}.{ext}" },
                new { Name = "切片", Template = "{name[:5].upper()}_{i:00}.{ext}" },
                new { Name = "复杂组合", Template = "prefix_{name.replace('_','-').upper()}_{2i+1:000}.{ext}" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"测试: {testCase.Name}");
                Console.WriteLine($"模板: {testCase.Template}");
                TestTemplate(testCase.Template);
                Console.WriteLine();
            }

            Console.WriteLine("========================================");
            Console.WriteLine("测试完成");
            Console.WriteLine("========================================");
        }

        private void TestTemplate(string template)
        {
            // Parse template
            var templateNode = _parser.Parse(template);

            // Create context
            var context = CreateContext("test_file", "txt", 5, 100);

            // Compile once
            var compiledFunc = _compiler.Compile(templateNode);

            // Warm up (run multiple times to ensure JIT compilation)
            for (int i = 0; i < 100; i++)
            {
                _evaluator.Evaluate(templateNode, context);
                compiledFunc(context);
            }

            // Run multiple rounds and take average
            const int rounds = 5;
            long totalEvaluatorTime = 0;
            long totalCompilerTime = 0;

            for (int round = 0; round < rounds; round++)
            {
                // Test evaluator (non-compiled)
                var evaluatorStopwatch = Stopwatch.StartNew();
                for (int i = 0; i < Iterations; i++)
                {
                    _evaluator.Evaluate(templateNode, context);
                }
                evaluatorStopwatch.Stop();
                totalEvaluatorTime += evaluatorStopwatch.ElapsedMilliseconds;

                // Test compiler (compiled)
                var compilerStopwatch = Stopwatch.StartNew();
                for (int i = 0; i < Iterations; i++)
                {
                    compiledFunc(context);
                }
                compilerStopwatch.Stop();
                totalCompilerTime += compilerStopwatch.ElapsedMilliseconds;
            }

            // Calculate average results
            var avgEvaluatorTime = totalEvaluatorTime / (double)rounds;
            var avgCompilerTime = totalCompilerTime / (double)rounds;
            var speedup = avgEvaluatorTime > 0 ? avgEvaluatorTime / avgCompilerTime : 0;

            // Display results
            Console.WriteLine($"  评估器 (未编译): {avgEvaluatorTime,8:F2} ms ({avgEvaluatorTime / Iterations * 1000:F3} μs/次)");
            Console.WriteLine($"  编译器 (已编译): {avgCompilerTime,8:F2} ms ({avgCompilerTime / Iterations * 1000:F3} μs/次)");
            Console.WriteLine($"  性能提升: {speedup:F2}x");
            Console.WriteLine($"  节省时间: {avgEvaluatorTime - avgCompilerTime:F2} ms ({(avgEvaluatorTime - avgCompilerTime) / avgEvaluatorTime * 100:F1}%)");
        }

        private IEvaluationContext CreateContext(string name, string ext, int index, int totalCount)
        {
            return new EvaluationContext(
                name: name,
                ext: ext,
                fullName: $"{name}.{ext}",
                fullPath: $@"C:\test\{name}.{ext}",
                index: index,
                totalCount: totalCount);
        }
    }
}

