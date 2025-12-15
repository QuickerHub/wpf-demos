using System;
using System.Linq;
using BatchRenameTool.Template.Compiler;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchRenameTool.Tests
{
    /// <summary>
    /// Tests for template compiler - verify compiled functions produce same results as evaluator
    /// </summary>
    [TestClass]
    public class TemplateCompilerTests
    {
        private TemplateParser _parser = null!;
        private TemplateEvaluator _evaluator = null!;
        private TemplateCompiler _compiler = null!;

        [TestInitialize]
        public void Setup()
        {
            _parser = new TemplateParser(Enumerable.Empty<Type>());
            _evaluator = new TemplateEvaluator();
            _compiler = new TemplateCompiler();
        }

        #region Basic Variable Tests

        [TestMethod]
        [DataRow("{name}", "test", "txt", 0)]
        [DataRow("{ext}", "test", "txt", 0)]
        [DataRow("{fullname}", "test", "txt", 0)]
        [DataRow("{i}", "test", "txt", 0)]
        [DataRow("{i}", "test", "txt", 5)]
        [DataRow("{iv}", "test", "txt", 0, 10)]
        [DataRow("{iv}", "test", "txt", 5, 10)]
        public void TestBasicVariables(string template, string name, string ext, int index, int totalCount = 10)
        {
            TestCompiledVsEvaluator(template, name, ext, index, totalCount);
        }

        #endregion

        #region Format Tests

        [TestMethod]
        [DataRow("{i:00}", "test", "txt", 0)]
        [DataRow("{i:00}", "test", "txt", 1)]
        [DataRow("{i:00}", "test", "txt", 10)]
        [DataRow("{i:000}", "test", "txt", 5)]
        [DataRow("{i:01}", "test", "txt", 0)]
        [DataRow("{i:1}", "test", "txt", 0)]
        [DataRow("{i:001}", "test", "txt", 0)]
        public void TestFormat(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        #endregion

        #region Expression Tests

        [TestMethod]
        [DataRow("{i2+1}", "test", "txt", 0)]
        [DataRow("{i2+1}", "test", "txt", 1)]
        [DataRow("{2i+1}", "test", "txt", 0)]
        [DataRow("{2i+1}", "test", "txt", 1)]
        [DataRow("{i*3-2}", "test", "txt", 0)]
        [DataRow("{i*3-2}", "test", "txt", 2)]
        [DataRow("{2*i+1}", "test", "txt", 0)]
        [DataRow("{i*2+1}", "test", "txt", 1)]
        public void TestExpressionWithoutFormat(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        [TestMethod]
        [DataRow("{2i+1:00}", "test", "txt", 0)]
        [DataRow("{2i+1:00}", "test", "txt", 1)]
        [DataRow("{2i+1:000}", "test", "txt", 5)]
        [DataRow("{i*3-2:00}", "test", "txt", 2)]
        public void TestExpressionWithFormat(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        #endregion

        #region Method Tests

        [TestMethod]
        [DataRow("{name.upper()}", "test", "txt", 0)]
        [DataRow("{name.lower()}", "TEST", "txt", 0)]
        [DataRow("{name.trim()}", "  test  ", "txt", 0)]
        public void TestBasicMethods(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        [TestMethod]
        [DataRow("{name.replace('e','E')}", "test", "txt", 0)]
        [DataRow("{name.sub(1,3)}", "test", "txt", 0)]
        [DataRow("{name.padLeft(10)}", "test", "txt", 0)]
        [DataRow("{name.padRight(10)}", "test", "txt", 0)]
        [DataRow("{name.padLeft(10,'0')}", "test", "txt", 0)]
        [DataRow("{name.padRight(10,'-')}", "test", "txt", 0)]
        public void TestMethodsWithParameters(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        [TestMethod]
        [DataRow("{name.replace('e','E').upper()}", "test", "txt", 0)]
        [DataRow("{name.sub(1).padLeft(10,'*')}", "test", "txt", 0)]
        public void TestChainedMethods(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        #endregion

        #region Slice Tests

        [TestMethod]
        [DataRow("{name[1:3]}", "test", "txt", 0)]
        [DataRow("{name[:3]}", "test", "txt", 0)]
        [DataRow("{name[1:]}", "test", "txt", 0)]
        [DataRow("{name[:]}", "test", "txt", 0)]
        public void TestSlice(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        #endregion

        #region Complex Template Tests

        [TestMethod]
        [DataRow("{name}_{i:00}", "test", "txt", 0)]
        [DataRow("{name}_{2i+1:00}", "test", "txt", 0)]
        [DataRow("{name}_{2i+1:00}.{ext}", "test", "txt", 1)]
        [DataRow("prefix_{name}.{ext}", "test", "txt", 0)]
        [DataRow("{name[:5].upper}_{i:00}.{ext}", "testfile", "txt", 0)]
        [DataRow("{name[4:].upper.replace('_','-')}.{ext}", "IMG_photo", "jpg", 0)]
        public void TestComplexTemplates(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        #endregion

        #region Date/Time Tests

        [TestMethod]
        [DataRow("{today:yyyyMMdd}", "test", "txt", 0)]
        [DataRow("{today:yyyy-MM-dd}", "test", "txt", 0)]
        [DataRow("{now:yyyyMMddHHmmss}", "test", "txt", 0)]
        public void TestDateTime(string template, string name, string ext, int index)
        {
            TestCompiledVsEvaluator(template, name, ext, index);
        }

        #endregion

        #region Multiple Compilation Tests

        [TestMethod]
        public void TestMultipleCompilations()
        {
            // Test that compiling the same template multiple times produces consistent results
            var template = "{name}_{i:00}.{ext}";
            var context = CreateContext("test", "txt", 0);

            var templateNode = _parser.Parse(template);
            var compiled1 = _compiler.Compile(templateNode);
            var compiled2 = _compiler.Compile(templateNode);

            var result1 = compiled1(context);
            var result2 = compiled2(context);

            Assert.AreEqual(result1, result2, "Multiple compilations should produce same results");
        }

        [TestMethod]
        public void TestCompilationCache()
        {
            // Test that the same compiled function can be reused
            var template = "{name}_{i:00}.{ext}";
            var context1 = CreateContext("test1", "txt", 0);
            var context2 = CreateContext("test2", "txt", 1);

            var templateNode = _parser.Parse(template);
            var compiled = _compiler.Compile(templateNode);

            var result1 = compiled(context1);
            var result2 = compiled(context2);

            Assert.AreEqual("test1_00.txt", result1);
            Assert.AreEqual("test2_01.txt", result2);
        }

        #endregion

        #region Performance Comparison Tests

        [TestMethod]
        public void TestPerformanceComparison()
        {
            // This test verifies that compiled functions work correctly
            // Actual performance testing should be done separately
            var template = "{name[:5].upper.replace('_','-')}_{i:000}.{ext}";
            var context = CreateContext("test_file_name", "txt", 5);

            var templateNode = _parser.Parse(template);
            
            // Evaluate using evaluator
            var evaluatorResult = _evaluator.Evaluate(templateNode, context);
            
            // Compile and execute
            var compiled = _compiler.Compile(templateNode);
            var compiledResult = compiled(context);

            Assert.AreEqual(evaluatorResult, compiledResult, "Compiled function should produce same result as evaluator");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Test that compiled function produces same result as evaluator
        /// </summary>
        private void TestCompiledVsEvaluator(string template, string name, string ext, int index, int totalCount = 10)
        {
            var context = CreateContext(name, ext, index, totalCount);

            // Parse template
            var templateNode = _parser.Parse(template);

            // Evaluate using evaluator (original method)
            var evaluatorResult = _evaluator.Evaluate(templateNode, context);

            // Compile and execute (new method)
            var compiled = _compiler.Compile(templateNode);
            var compiledResult = compiled(context);

            // Results should match
            Assert.AreEqual(
                evaluatorResult, 
                compiledResult, 
                $"Template: {template}, Name: {name}, Ext: {ext}, Index: {index}, TotalCount: {totalCount}");
        }

        /// <summary>
        /// Create evaluation context for testing
        /// </summary>
        private IEvaluationContext CreateContext(string name, string ext, int index, int totalCount = 10)
        {
            return new EvaluationContext(
                name: name,
                ext: ext,
                fullName: $"{name}.{ext}",
                fullPath: $@"C:\test\{name}.{ext}",
                index: index,
                totalCount: totalCount);
        }

        #endregion
    }
}

