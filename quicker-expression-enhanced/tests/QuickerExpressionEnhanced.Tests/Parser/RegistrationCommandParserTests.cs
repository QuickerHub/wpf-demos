using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickerExpressionEnhanced.Parser;
using Quicker.Public.Interfaces;
using Moq;

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
            var code = "type {class} {assembly}\nvar x = 1;";

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
            var code = "load {assembly}\nusing {namespace} {assembly}\ntype {class} {assembly}\nvar x = 1;";

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
            var code = "//type {class} {assembly}\nvar x = 1;";

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
    }
}

