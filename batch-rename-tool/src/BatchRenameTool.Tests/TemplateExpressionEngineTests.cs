using System;
using System.Linq;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.ExpressionEngine;
using BatchRenameTool.Template.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchRenameTool.Tests
{
    /// <summary>
    /// Tests for the new expression engine architecture
    /// </summary>
    [TestClass]
    public class TemplateExpressionEngineTests
    {
        private TemplateParser _parser = null!;
        private TemplateExpressionExecutor _executor = null!;
        private TemplateNodeExecutor _templateExecutor = null!;

        [TestInitialize]
        public void Setup()
        {
            _parser = new TemplateParser(Enumerable.Empty<Type>());
            _executor = new TemplateExpressionExecutor();
            _templateExecutor = new TemplateNodeExecutor(_executor);
        }

        #region Basic Value Type Tests

        [TestMethod]
        public void TestStringValue()
        {
            var str = new StringValue("hello");
            Assert.AreEqual("hello", str.ToString());
            Assert.IsTrue(str.HasMethod("upper"));
            Assert.IsFalse(str.HasMethod("unknown"));

            var upper = str.InvokeMethod("upper", Array.Empty<ITemplateValue>());
            Assert.IsInstanceOfType(upper, typeof(StringValue));
            Assert.AreEqual("HELLO", upper.ToString());
        }

        [TestMethod]
        public void TestStringValueMethods()
        {
            var str = new StringValue("  test  ");

            // Test trim
            var trimmed = str.InvokeMethod("trim", Array.Empty<ITemplateValue>());
            Assert.AreEqual("test", trimmed.ToString());

            // Test upper
            var upper = str.InvokeMethod("upper", Array.Empty<ITemplateValue>());
            Assert.AreEqual("  TEST  ", upper.ToString());

            // Test lower
            var lower = new StringValue("TEST").InvokeMethod("lower", Array.Empty<ITemplateValue>());
            Assert.AreEqual("test", lower.ToString());
        }

        [TestMethod]
        public void TestStringValueReplace()
        {
            var str = new StringValue("test");
            var oldValue = new StringValue("e");
            var newValue = new StringValue("E");

            var result = str.InvokeMethod("replace", new[] { oldValue, newValue });
            Assert.AreEqual("tEst", result.ToString());
        }

        [TestMethod]
        public void TestStringValueSub()
        {
            var str = new StringValue("test");
            var start = new NumberValue(1);
            var end = new NumberValue(3);

            var result = str.InvokeMethod("sub", new[] { start, end });
            Assert.AreEqual("es", result.ToString());
        }

        [TestMethod]
        public void TestStringValueSlice()
        {
            var str = new StringValue("test");
            
            // Test slice with start and end
            var result1 = str.InvokeMethod("slice", new[] { new NumberValue(1), new NumberValue(3) });
            Assert.AreEqual("es", result1.ToString());

            // Test slice with start only
            var result2 = str.InvokeMethod("slice", new[] { new NumberValue(1) });
            Assert.AreEqual("est", result2.ToString());

            // Test slice with no arguments
            var result3 = str.InvokeMethod("slice", Array.Empty<ITemplateValue>());
            Assert.AreEqual("test", result3.ToString());
        }

        [TestMethod]
        public void TestNumberValue()
        {
            var num = new NumberValue(42);
            Assert.AreEqual(42, num.GetValue());
            Assert.AreEqual("42", num.ToString());
            Assert.AreEqual("042", num.ToString("000"));
        }

        [TestMethod]
        public void TestIndexValue()
        {
            var index = new IndexValue(0, 10);
            Assert.AreEqual(0, index.GetValue());
            Assert.AreEqual("0", index.ToString());
            Assert.AreEqual("00", index.ToString("00"));
            Assert.AreEqual("001", index.ToString("001"));
        }

        [TestMethod]
        public void TestIndexValueExpression()
        {
            var index = new IndexValue(0, 10);

            // Test expression: 2i+1
            var result1 = index.EvaluateExpression("2i+1", null);
            Assert.AreEqual("1", result1.ToString());

            // Test expression with format: 2i+1:00
            var result2 = index.EvaluateExpression("2i+1", "00");
            Assert.AreEqual("01", result2.ToString());

            // Test with index = 1
            var index1 = new IndexValue(1, 10);
            var result3 = index1.EvaluateExpression("2i+1", "00");
            Assert.AreEqual("03", result3.ToString());
        }

        [TestMethod]
        public void TestDateValue()
        {
            var date = DateTime.Today;
            var dateValue = new DateValue(date);

            Assert.AreEqual(date, dateValue.GetValue());
            Assert.IsTrue(dateValue.ToString().Contains(date.Year.ToString(), StringComparison.Ordinal));

            // Test format
            var formatted = dateValue.ToString("yyyyMMdd");
            Assert.AreEqual(8, formatted.Length);
        }

        #endregion

        #region Expression Execution Tests

        [TestMethod]
        [DataRow("{name}", "test", "txt", 0, "test")]
        [DataRow("{ext}", "test", "txt", 0, "txt")]
        [DataRow("{fullname}", "test", "txt", 0, "test.txt")]
        [DataRow("{i}", "test", "txt", 0, "0")]
        [DataRow("{i}", "test", "txt", 5, "5")]
        public void TestBasicVariables(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{i:00}", "test", "txt", 0, "00")]
        [DataRow("{i:00}", "test", "txt", 1, "01")]
        [DataRow("{i:000}", "test", "txt", 5, "005")]
        [DataRow("{i:001}", "test", "txt", 0, "001")]
        public void TestFormat(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name.upper()}", "test", "txt", 0, "TEST")]
        [DataRow("{name.lower()}", "TEST", "txt", 0, "test")]
        [DataRow("{name.trim()}", "  test  ", "txt", 0, "test")]
        public void TestBasicMethods(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name.replace('e','E')}", "test", "txt", 0, "tEst")]
        [DataRow("{name.sub(1,3)}", "test", "txt", 0, "es")]
        [DataRow("{name.padLeft(10)}", "test", "txt", 0, "      test")]
        [DataRow("{name.padRight(10)}", "test", "txt", 0, "test      ")]
        public void TestMethodsWithParameters(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name[1:3]}", "test", "txt", 0, "es")]
        [DataRow("{name[:3]}", "test", "txt", 0, "tes")]
        [DataRow("{name[1:]}", "test", "txt", 0, "est")]
        public void TestSlice(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{name}_{i:00}", "test", "txt", 0, "test_00")]
        [DataRow("prefix_{name}.{ext}", "test", "txt", 0, "prefix_test.txt")]
        public void TestComplexTemplates(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        [TestMethod]
        [DataRow("{2i+1:00}", "test", "txt", 0, "01")]
        [DataRow("{2i+1:00}", "test", "txt", 1, "03")]
        [DataRow("{i*3-2:00}", "test", "txt", 2, "04")]
        public void TestExpressionWithFormat(string template, string name, string ext, int index, string expected)
        {
            TestTemplateExecution(template, name, ext, index, expected);
        }

        #endregion

        #region Method Chaining Tests

        [TestMethod]
        public void TestMethodChaining()
        {
            // Test: {name.upper().replace('E','e')}
            var context = CreateContext("test", "txt", 0);
            var templateNode = _parser.Parse("{name.upper()}");
            var result = _templateExecutor.Execute(templateNode, context);
            Assert.AreEqual("TEST", result);

            // Note: Method chaining like {name.upper().replace('E','e')} 
            // requires parser support for nested method calls
            // This will be tested when parser is enhanced
        }

        #endregion

        #region Comparison with Old Evaluator

        [TestMethod]
        [DataRow("{name}", "test", "txt", 0)]
        [DataRow("{ext}", "test", "txt", 0)]
        [DataRow("{i:00}", "test", "txt", 0)]
        [DataRow("{i:00}", "test", "txt", 1)]
        [DataRow("{name.upper()}", "test", "txt", 0)]
        [DataRow("{name.replace('e','E')}", "test", "txt", 0)]
        [DataRow("{name[1:3]}", "test", "txt", 0)]
        [DataRow("{2i+1:00}", "test", "txt", 0)]
        public void TestComparisonWithOldEvaluator(string template, string name, string ext, int index)
        {
            var context = CreateContext(name, ext, index);

            // Old evaluator
            var oldEvaluator = new TemplateEvaluator();
            var templateNode = _parser.Parse(template);
            var oldResult = oldEvaluator.Evaluate(templateNode, context);

            // New executor
            var newResult = _templateExecutor.Execute(templateNode, context);

            // Results should match
            Assert.AreEqual(oldResult, newResult, 
                $"Template: {template}, Name: {name}, Ext: {ext}, Index: {index}");
        }

        #endregion

        #region Value Type Conversion Tests

        [TestMethod]
        public void TestValueTypeConversions()
        {
            // Test StringValue
            var str = new StringValue("123");
            Assert.AreEqual("123", str.ToString());

            // Test NumberValue
            var num = new NumberValue(123);
            Assert.AreEqual(123, num.GetValue());
            Assert.AreEqual("123", num.ToString());

            // Test conversion from string to number in arguments
            var strValue = new StringValue("test");
            var numArg = new NumberValue(1);
            var result = strValue.InvokeMethod("sub", new[] { numArg });
            Assert.AreEqual("est", result.ToString());
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void TestInvalidMethod()
        {
            var str = new StringValue("test");
            try
            {
                str.InvokeMethod("invalidMethod", Array.Empty<ITemplateValue>());
                Assert.Fail("Expected NotSupportedException was not thrown");
            }
            catch (NotSupportedException)
            {
                // Expected exception
            }
        }

        [TestMethod]
        public void TestInvalidMethodWithHasMethod()
        {
            var str = new StringValue("test");
            Assert.IsFalse(str.HasMethod("invalidMethod"));
        }

        #endregion

        #region Helper Methods

        private void TestTemplateExecution(string template, string name, string ext, int index, string expected)
        {
            var context = CreateContext(name, ext, index);

            var templateNode = _parser.Parse(template);
            var result = _templateExecutor.Execute(templateNode, context);

            Assert.AreEqual(expected, result, 
                $"Template: {template}, Name: {name}, Ext: {ext}, Index: {index}");
        }

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

