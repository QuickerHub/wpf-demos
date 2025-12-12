using System;
using System.Linq;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchRenameTool.Tests
{
    /// <summary>
    /// Tests for template method calls
    /// </summary>
    [TestClass]
    public class TemplateMethodTests
    {
        private TemplateParser _parser = null!;
        private TemplateEvaluator _evaluator = null!;

        [TestInitialize]
        public void Setup()
        {
            _parser = new TemplateParser(Enumerable.Empty<Type>());
            _evaluator = new TemplateEvaluator();
        }

        [TestMethod]
        [DataRow("{name.upper()}", "test", "txt", 0, "TEST")]
        [DataRow("{name.lower()}", "TEST", "txt", 0, "test")]
        [DataRow("{name.trim()}", "  test  ", "txt", 0, "test")]
        public void TestBasicMethods(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name.sub(1,3)}", "test", "txt", 0, "es")]
        [DataRow("{name.padLeft(10)}", "test", "txt", 0, "      test")]
        [DataRow("{name.padRight(10)}", "test", "txt", 0, "test      ")]
        public void TestMethodsWithParameters(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name.replace('e','E')}", "test", "txt", 0, "tEst")]
        [DataRow("{name.padLeft(10,'0')}", "test", "txt", 0, "000000test")]
        [DataRow("{name.padRight(10,'-')}", "test", "txt", 0, "test------")]
        public void TestMethodsWithStringLiterals(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name.replace('e','E').upper()}", "test", "txt", 0, "TEST")]
        [DataRow("{name.sub(1).padLeft(10,'*')}", "test", "txt", 0, "*******est")]
        public void TestChainedMethods(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{ext.upper()}", "test", "txt", 0, "TXT")]
        [DataRow("{fullname.lower()}", "test", "TXT", 0, "test.txt")]
        public void TestVariableMethods(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        private void TestTemplate(string template, string name, string ext, int index, string expected)
        {
            var context = new EvaluationContext(
                name: name,
                ext: ext,
                fullName: $"{name}.{ext}",
                fullPath: $@"C:\test\{name}.{ext}",
                index: index,
                totalCount: 10);

            var templateNode = _parser.Parse(template);
            var result = _evaluator.Evaluate(templateNode, context);

            Assert.AreEqual(expected, result, $"Template: {template}, Index: {index}");
        }
    }
}