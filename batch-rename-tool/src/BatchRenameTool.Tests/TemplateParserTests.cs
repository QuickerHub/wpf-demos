using System;
using System.Linq;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchRenameTool.Tests
{
    /// <summary>
    /// Tests for template parser and evaluator
    /// </summary>
    [TestClass]
    public class TemplateParserTests
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
        [DataRow("{name}", "test", "txt", 0, "test")]
        [DataRow("{ext}", "test", "txt", 0, "txt")]
        [DataRow("{fullname}", "test", "txt", 0, "test.txt")]
        public void TestBasicVariables(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{i}", "test", "txt", 0, "0")]
        [DataRow("{i}", "test", "txt", 1, "1")]
        [DataRow("{i}", "test", "txt", 5, "5")]
        public void TestIndexVariable(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{i:00}", "test", "txt", 0, "00")]
        [DataRow("{i:00}", "test", "txt", 1, "01")]
        [DataRow("{i:00}", "test", "txt", 10, "10")]
        [DataRow("{i:000}", "test", "txt", 5, "005")]
        [DataRow("{i:01}", "test", "txt", 0, "01")]
        [DataRow("{i:1}", "test", "txt", 0, "1")]
        public void TestFormat(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{i2+1}", "test", "txt", 0, "1")]      // 0*2+1 = 1
        [DataRow("{i2+1}", "test", "txt", 1, "3")]      // 1*2+1 = 3
        [DataRow("{2i+1}", "test", "txt", 0, "1")]     // 2*0+1 = 1
        [DataRow("{2i+1}", "test", "txt", 1, "3")]     // 2*1+1 = 3
        [DataRow("{i*3-2}", "test", "txt", 0, "-2")]   // 0*3-2 = -2
        [DataRow("{i*3-2}", "test", "txt", 2, "4")]    // 2*3-2 = 4
        public void TestExpressionWithoutFormat(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{2i+1:00}", "test", "txt", 0, "01")] // 2*0+1 = 1, format as 01
        [DataRow("{2i+1:00}", "test", "txt", 1, "03")] // 2*1+1 = 3, format as 03
        [DataRow("{2i+1:000}", "test", "txt", 5, "011")] // 2*5+1 = 11, format as 011
        [DataRow("{i*3-2:00}", "test", "txt", 2, "04")] // 2*3-2 = 4, format as 04
        public void TestExpressionWithFormat(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{2*i+1}", "test", "txt", 0, "1")]
        [DataRow("{2*i+1}", "test", "txt", 1, "3")]
        [DataRow("{i*2+1}", "test", "txt", 0, "1")]
        [DataRow("{i*2+1}", "test", "txt", 1, "3")]
        public void TestComplexExpression(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name}_{i:00}", "test", "txt", 0, "test_00")]
        [DataRow("{name}_{2i+1:00}", "test", "txt", 0, "test_01")]
        [DataRow("{name}_{2i+1:00}.{ext}", "test", "txt", 1, "test_03.txt")]
        public void TestMixedTemplates(string template, string name, string ext, int index, string expected)
        {
            TestTemplate(template, name, ext, index, expected);
        }

        private void TestTemplate(string template, string name, string ext, int index, string expected)
        {
            var context = new EvaluationContext
            {
                Name = name,
                Ext = ext,
                FullName = $"{name}.{ext}",
                Index = index
            };

            var templateNode = _parser.Parse(template);
            var result = _evaluator.Evaluate(templateNode, context);

            Assert.AreEqual(expected, result, $"Template: {template}, Index: {index}");
        }
    }
}