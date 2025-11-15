using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickerExpressionEnhanced.Parser;
using Quicker.Public.Interfaces;
using Moq;
using Z.Expressions;

namespace QuickerExpressionEnhanced.Tests.Parser
{
    /// <summary>
    /// Tests for RegistrationCommandParser
    /// </summary>
    [TestClass]
    public class RegistrationCommandParserTests
    {
        private IActionContext CreateMockContext(Dictionary<string, object> variables)
        {
            var mock = new Mock<IActionContext>();
            var vars = new Dictionary<string, object>();
            foreach (var kvp in variables)
            {
                vars[kvp.Key] = kvp.Value;
            }
            mock.Setup(x => x.GetVariables()).Returns(vars);
            mock.Setup(x => x.GetVarValue(It.IsAny<string>())).Returns<string>(key => 
                variables.TryGetValue(key, out var value) ? value : null);
            return mock.Object;
        }

        [TestMethod]
        public void ParseCommands_LoadAssembly_WithVariable()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly", "System.Core" }
            });
            var code = "load {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("System.Core", loadCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_LoadAssembly_WithCommentPrefix()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly", "System.Core" }
            });
            var code = "//load {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("System.Core", loadCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_LoadAssembly_WithComplexVariable()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "ass", "System.Core" },
                { "version", "1.0.0" }
            });
            var code = "load {ass}.{version}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("System.Core.1.0.0", loadCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_LoadAssembly_WithLiteral()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "load System.Core\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("System.Core", loadCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_LoadAssembly_WithFilePathAndVariables()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "packagePath", "C:\\Packages" },
                { "version", "1.0.0" }
            });
            var code = "load {packagePath}/IntelliTools.Quicker.{version}.dll\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("C:\\Packages/IntelliTools.Quicker.1.0.0.dll", loadCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_LoadAssembly_WithFilePathAndVariables_ForwardSlash()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "packagePath", "C:/Packages" },
                { "version", "1.0.0" }
            });
            var code = "load {packagePath}/IntelliTools.Quicker.{version}.dll\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("C:/Packages/IntelliTools.Quicker.1.0.0.dll", loadCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_UsingNamespace_WithVariables()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "namespace", "System" },
                { "assembly", "System.Core" }
            });
            var code = "using {namespace} {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(UsingNamespaceCommand));
            var usingCmd = (UsingNamespaceCommand)commands[0];
            Assert.AreEqual("System", usingCmd.Namespace);
            Assert.AreEqual("System.Core", usingCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_RegisterType_WithVariables()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "class", "MyClass" },
                { "assembly", "MyAssembly" }
            });
            var code = "type {class}, {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
            var typeCmd = (RegisterTypeCommand)commands[0];
            Assert.AreEqual("MyClass", typeCmd.ClassName);
            Assert.AreEqual("MyAssembly", typeCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_MultipleCommands()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly", "System.Core" },
                { "namespace", "System" },
                { "class", "MyClass" }
            });
            var code = "load {assembly}\nusing {namespace} {assembly}\ntype {class}, {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(3, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            Assert.IsInstanceOfType(commands[1], typeof(UsingNamespaceCommand));
            Assert.IsInstanceOfType(commands[2], typeof(RegisterTypeCommand));
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_NoCommands()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "var x = 1;\nvar y = 2;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.IsEmpty(commands);
            // Normalize line endings for comparison
            var normalizedCode = code.Replace("\r\n", "\n").Replace("\r", "\n");
            var normalizedRemaining = remainingCode.Replace("\r\n", "\n").Replace("\r", "\n");
            Assert.AreEqual(normalizedCode, normalizedRemaining);
        }

        [TestMethod]
        public void ParseCommands_EmptyLines()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly", "System.Core" }
            });
            var code = "load {assembly}\n\nvar x = 1;\n";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            StringAssert.Contains(remainingCode, "var x = 1;");
        }

        [TestMethod]
        public void ParseCommands_CaseInsensitive()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly", "System.Core" }
            });
            var code = "LOAD {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
        }

        [TestMethod]
        public void ParseCommands_WhitespaceHandling()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly", "System.Core" }
            });
            var code = "  load   {assembly}  \nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            var loadCmd = (LoadAssemblyCommand)commands[0];
            Assert.AreEqual("System.Core", loadCmd.Assembly);
        }

        [TestMethod]
        public void ParseCommands_UsingNamespace_WithCommentPrefix()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "namespace", "System" },
                { "assembly", "System.Core" }
            });
            var code = "//using {namespace} {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(UsingNamespaceCommand));
            var usingCmd = (UsingNamespaceCommand)commands[0];
            Assert.AreEqual("System", usingCmd.Namespace);
            Assert.AreEqual("System.Core", usingCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_RegisterType_WithCommentPrefix()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "class", "MyClass" },
                { "assembly", "MyAssembly" }
            });
            var code = "//type {class}, {assembly}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
            var typeCmd = (RegisterTypeCommand)commands[0];
            Assert.AreEqual("MyClass", typeCmd.ClassName);
            Assert.AreEqual("MyAssembly", typeCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_MixedCommentAndDirect()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "assembly1", "System.Core" },
                { "assembly2", "System.Data" }
            });
            var code = "//load {assembly1}\nload {assembly2}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(2, commands);
            Assert.IsInstanceOfType(commands[0], typeof(LoadAssemblyCommand));
            Assert.IsInstanceOfType(commands[1], typeof(LoadAssemblyCommand));
            Assert.AreEqual("System.Core", ((LoadAssemblyCommand)commands[0]).Assembly);
            Assert.AreEqual("System.Data", ((LoadAssemblyCommand)commands[1]).Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_UsingNamespace_WithCommentPrefixAndSemicolon()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "//using System.Windows PresentationFramework;\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(UsingNamespaceCommand));
            var usingCmd = (UsingNamespaceCommand)commands[0];
            Assert.AreEqual("System.Windows", usingCmd.Namespace);
            Assert.AreEqual("PresentationFramework", usingCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_RegisterType_WithVersionInfo()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "type System.Windows.Forms.Clipboard, System.Windows.Forms, Version=4.0.0.0\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
            var typeCmd = (RegisterTypeCommand)commands[0];
            Assert.AreEqual("System.Windows.Forms.Clipboard", typeCmd.ClassName);
            Assert.AreEqual("System.Windows.Forms, Version=4.0.0.0", typeCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_RegisterType_WithCommentPrefixAndSemicolon()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "//type System.Windows.Forms.Clipboard, System.Windows.Forms, Version=4.0.0.0;\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
            var typeCmd = (RegisterTypeCommand)commands[0];
            Assert.AreEqual("System.Windows.Forms.Clipboard", typeCmd.ClassName);
            Assert.AreEqual("System.Windows.Forms, Version=4.0.0.0", typeCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_RegisterType_WithFilePath()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "type System.String, C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\mscorlib.dll\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
            var typeCmd = (RegisterTypeCommand)commands[0];
            Assert.AreEqual("System.String", typeCmd.ClassName);
            Assert.AreEqual("C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\mscorlib.dll", typeCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        [TestMethod]
        public void ParseCommands_RegisterType_WithFilePathAndVariables()
        {
            // Arrange
            var context = CreateMockContext(new Dictionary<string, object>
            {
                { "className", "System.String" },
                { "dllpath", "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\mscorlib.dll" }
            });
            var code = "type {className}, {dllpath}\nvar x = 1;";

            // Act
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);

            // Assert
            Assert.HasCount(1, commands);
            Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
            var typeCmd = (RegisterTypeCommand)commands[0];
            Assert.AreEqual("System.String", typeCmd.ClassName);
            Assert.AreEqual("C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\mscorlib.dll", typeCmd.Assembly);
            Assert.AreEqual("var x = 1;", remainingCode.Trim());
        }

        /// <summary>
        /// Generic performance test method that measures parse and execute times
        /// Runs multiple iterations, removes min/max, and calculates average
        /// </summary>
        /// <param name="code">Code to test</param>
        /// <param name="context">Action context</param>
        /// <param name="iterations">Number of iterations (default: 10)</param>
        /// <param name="testName">Test name for output</param>
        /// <param name="assertions">Optional action to perform assertions on each iteration</param>
        /// <returns>Performance test results</returns>
        private (double AvgParseTime, double AvgExecuteTime, double AvgTotalTime) RunPerformanceTest(
            string code,
            IActionContext context,
            int iterations = 10,
            string testName = "Performance Test",
            Action<List<RegistrationCommand>>? assertions = null)
        {
            // Warm up
            var evalWarmup = new EvalContext();
            RegistrationCommandParser.ParseCommands(code, context, out var warmupCommands);
            if (warmupCommands.Count > 0)
            {
                try
                {
                    RegistrationCommandExecutor.Register(evalWarmup, warmupCommands);
                }
                catch
                {
                    // Ignore warmup errors
                }
            }

            // Run tests
            var parseTimes = new List<long>();
            var executeTimes = new List<long>();
            var totalTimes = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var eval = new EvalContext();

                // Measure parsing time
                var parseStopwatch = Stopwatch.StartNew();
                var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);
                parseStopwatch.Stop();

                // Measure execution time
                var executeStopwatch = Stopwatch.StartNew();
                try
                {
                    RegistrationCommandExecutor.Register(eval, commands);
                }
                catch (Exception ex)
                {
                    executeStopwatch.Stop();
                    throw new InvalidOperationException($"Execution failed on iteration {i + 1}: {ex.Message}", ex);
                }
                executeStopwatch.Stop();

                // Run custom assertions if provided
                assertions?.Invoke(commands);

                // Record times
                var parseMs = parseStopwatch.ElapsedMilliseconds;
                var executeMs = executeStopwatch.ElapsedMilliseconds;
                var totalMs = parseMs + executeMs;

                parseTimes.Add(parseMs);
                executeTimes.Add(executeMs);
                totalTimes.Add(totalMs);
            }

            // Remove min and max values
            parseTimes.Sort();
            executeTimes.Sort();
            totalTimes.Sort();

            if (parseTimes.Count >= 2)
            {
                parseTimes.RemoveAt(0); // Remove min
                parseTimes.RemoveAt(parseTimes.Count - 1); // Remove max
            }
            if (executeTimes.Count >= 2)
            {
                executeTimes.RemoveAt(0); // Remove min
                executeTimes.RemoveAt(executeTimes.Count - 1); // Remove max
            }
            if (totalTimes.Count >= 2)
            {
                totalTimes.RemoveAt(0); // Remove min
                totalTimes.RemoveAt(totalTimes.Count - 1); // Remove max
            }

            // Calculate averages
            var avgParseTime = parseTimes.Count > 0 ? parseTimes.Average() : 0;
            var avgExecuteTime = executeTimes.Count > 0 ? executeTimes.Average() : 0;
            var avgTotalTime = totalTimes.Count > 0 ? totalTimes.Average() : 0;

            // Output performance results
            Console.WriteLine($"{testName} Results ({iterations} iterations, min/max removed):");
            Console.WriteLine($"Average Parse time: {avgParseTime:F2} ms");
            Console.WriteLine($"Average Execute time: {avgExecuteTime:F2} ms");
            Console.WriteLine($"Average Total time: {avgTotalTime:F2} ms");
            Console.WriteLine($"Parse times: [{string.Join(", ", parseTimes)}] ms");
            Console.WriteLine($"Execute times: [{string.Join(", ", executeTimes)}] ms");

            return (avgParseTime, avgExecuteTime, avgTotalTime);
        }

        [TestMethod]
        public void PerformanceTest_RegisterType_SystemWindowsClipboard()
        {
            // Arrange - Using System.String which is always available in mscorlib
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "type System.String, mscorlib, Version=4.0.0.0;\nvar x = 1;";

            // Run performance test
            var (avgParseTime, avgExecuteTime, avgTotalTime) = RunPerformanceTest(
                code,
                context,
                iterations: 10,
                testName: "RegisterType System.String Performance Test",
                assertions: commands =>
                {
                    Assert.HasCount(1, commands);
                    Assert.IsInstanceOfType(commands[0], typeof(RegisterTypeCommand));
                    var typeCmd = (RegisterTypeCommand)commands[0];
                    Assert.AreEqual("System.String", typeCmd.ClassName);
                    Assert.AreEqual("mscorlib, Version=4.0.0.0", typeCmd.Assembly);
                });

            // Performance assertions (adjust thresholds as needed)
            Assert.IsTrue(avgParseTime < 100, $"Average parse time should be less than 100ms, but was {avgParseTime:F2}ms");
            Assert.IsTrue(avgExecuteTime < 1000, $"Average execute time should be less than 1000ms, but was {avgExecuteTime:F2}ms");
        }

        [TestMethod]
        public void PerformanceTest_RegisterType_SystemWindowsClipboard_WithoutAssembly()
        {
            // Arrange - Test without assembly name (should use type inference)
            var context = CreateMockContext(new Dictionary<string, object>());
            var code = "type System.Windows.Clipboard;\nvar x = 1;";
            var eval = new EvalContext();

            // Note: This will fail because assembly is required, but we can test the parsing part
            var parseStopwatch = Stopwatch.StartNew();
            var remainingCode = RegistrationCommandParser.ParseCommands(code, context, out var commands);
            parseStopwatch.Stop();

            // Assert - parsing should work, but execution will fail without assembly
            Console.WriteLine($"Parse time (without assembly): {parseStopwatch.ElapsedMilliseconds} ms ({parseStopwatch.ElapsedTicks} ticks)");
            
            // This should parse but not execute (assembly required)
            if (commands.Count > 0)
            {
                var typeCmd = (RegisterTypeCommand)commands[0];
                Assert.AreEqual("System.Windows.Clipboard", typeCmd.ClassName);
            }
        }
    }
}

