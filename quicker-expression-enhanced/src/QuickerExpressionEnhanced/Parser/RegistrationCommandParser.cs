using System;
using System.Collections.Generic;
using Quicker.Public.Interfaces;

namespace QuickerExpressionEnhanced.Parser
{
    /// <summary>
    /// Base class for registration commands
    /// </summary>
    public abstract class RegistrationCommand
    {
        public string OriginalLine { get; set; } = string.Empty;
    }

    /// <summary>
    /// Load assembly command: load {assembly}
    /// </summary>
    public class LoadAssemblyCommand : RegistrationCommand
    {
        public string Assembly { get; set; } = string.Empty;
    }

    /// <summary>
    /// Using namespace command: using {namespace} {assembly}
    /// </summary>
    public class UsingNamespaceCommand : RegistrationCommand
    {
        public string Namespace { get; set; } = string.Empty;
        public string Assembly { get; set; } = string.Empty;
    }

    /// <summary>
    /// Register type command: type {class} {assembly}
    /// </summary>
    public class RegisterTypeCommand : RegistrationCommand
    {
        public string ClassName { get; set; } = string.Empty;
        public string Assembly { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of parsing registration commands from expression
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// List of parsed registration commands
        /// </summary>
        public List<RegistrationCommand> Commands { get; set; } = new List<RegistrationCommand>();

        /// <summary>
        /// Remaining expression code after removing registration command lines
        /// </summary>
        public string RemainingCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parser for registration commands
    /// Supports:
    /// - load {assembly}
    /// - using {namespace} {assembly}
    /// - type {typeName}, {assemblyName}[, {additionalInfo}]
    /// </summary>
    public static class RegistrationCommandParser
    {

        /// <summary>
        /// Parse registration commands from expression
        /// Returns parsed commands and remaining code without registration command lines
        /// Uses variable interpolation: {var} is replaced with actual variable value
        /// Supports complex expressions like load {ass}.{version}
        /// </summary>
        /// <param name="expression">Expression code to parse</param>
        /// <param name="context">Action context for variable substitution</param>
        /// <returns>Parse result containing commands and remaining code</returns>
        public static ParseResult Parse(string expression, IActionContext context)
        {
            var remainingCode = ParseCommands(expression, context, out var commands);
            return new ParseResult
            {
                Commands = commands,
                RemainingCode = remainingCode
            };
        }

        /// <summary>
        /// Parse registration commands from code
        /// Removes parsed command lines and returns the remaining code
        /// Uses variable interpolation: {var} is replaced with actual variable value
        /// Supports complex expressions like load {ass}.{version}
        /// </summary>
        /// <param name="code">Code to parse</param>
        /// <param name="context">Action context for variable substitution</param>
        /// <param name="commands">Output list of parsed commands</param>
        /// <returns>Remaining code after removing registration commands</returns>
        public static string ParseCommands(string code, IActionContext context, out List<RegistrationCommand> commands)
        {
            commands = new List<RegistrationCommand>();
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var remainingLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var originalLine = lines[i];
                
                // Replace variable placeholders {key} with actual values
                var lineWithValues = ReplaceVariables(originalLine, context);
                var trimmedLine = lineWithValues.Trim();
                
                // Trim trailing semicolon if present
                if (trimmedLine.EndsWith(";"))
                {
                    trimmedLine = trimmedLine.Substring(0, trimmedLine.Length - 1).TrimEnd();
                }
                
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    remainingLines.Add(originalLine);
                    continue;
                }

                RegistrationCommand? command = null;

                // Remove // comment prefix if present
                var lineToParse = trimmedLine;
                if (trimmedLine.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                {
                    lineToParse = trimmedLine.Substring(2).TrimStart();
                }

                // Try to parse as LoadAssemblyCommand: load {assembly}
                if (lineToParse.StartsWith("load ", StringComparison.OrdinalIgnoreCase))
                {
                    command = TryParseLoadAssembly(lineToParse);
                }
                // Try to parse as UsingNamespaceCommand: using {namespace} {assembly}
                else if (lineToParse.StartsWith("using ", StringComparison.OrdinalIgnoreCase))
                {
                    command = TryParseUsingNamespace(lineToParse);
                }
                // Try to parse as RegisterTypeCommand: type {typeName}, {assemblyName}[, {additionalInfo}]
                else if (lineToParse.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    command = TryParseRegisterType(lineToParse);
                }

                if (command != null)
                {
                    command.OriginalLine = originalLine;
                    commands.Add(command);
                    // Don't add this line to remaining code
                }
                else
                {
                    remainingLines.Add(originalLine);
                }
            }

            return string.Join(Environment.NewLine, remainingLines);
        }

        /// <summary>
        /// Replace variable placeholders {key} with actual values from context
        /// Supports multiple variables in one string, e.g., {ass}.{version}
        /// </summary>
        /// <param name="text">Text containing variable placeholders</param>
        /// <param name="context">Action context</param>
        /// <returns>Text with variables replaced</returns>
        private static string ReplaceVariables(string text, IActionContext context)
        {
            var result = text;
            foreach (var key in context.GetVariables().Keys)
            {
                var value = context.GetVarValue(key);
                var valueStr = value?.ToString() ?? string.Empty;
                result = result.Replace($"{{{key}}}", valueStr);
            }
            return result;
        }

        /// <summary>
        /// Try to parse a line as LoadAssemblyCommand: load {assembly}
        /// </summary>
        /// <param name="line">Line to parse (with variables already replaced with values, should start with "load ")</param>
        /// <returns>LoadAssemblyCommand or null if not a load command</returns>
        private static LoadAssemblyCommand? TryParseLoadAssembly(string line)
        {
            if (!line.StartsWith("load ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var assembly = line.Substring(5).Trim();
            if (string.IsNullOrWhiteSpace(assembly))
            {
                return null;
            }

            return new LoadAssemblyCommand
            {
                Assembly = assembly
            };
        }

        /// <summary>
        /// Try to parse a line as UsingNamespaceCommand: using {namespace} {assembly}
        /// </summary>
        /// <param name="line">Line to parse (with variables already replaced with values, should start with "using ")</param>
        /// <returns>UsingNamespaceCommand or null if not a using command</returns>
        private static UsingNamespaceCommand? TryParseUsingNamespace(string line)
        {
            if (!line.StartsWith("using ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var rest = line.Substring(6).Trim();
            if (string.IsNullOrWhiteSpace(rest))
            {
                return null;
            }

            // Split by whitespace to get namespace and assembly
            var parts = rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            var namespaceValue = parts[0].Trim();
            var assembly = string.Join(" ", parts, 1, parts.Length - 1).Trim();

            return new UsingNamespaceCommand
            {
                Namespace = namespaceValue,
                Assembly = assembly
            };
        }

        /// <summary>
        /// Try to parse a line as RegisterTypeCommand: type {typeName}, {assemblyName}[, {additionalInfo}]
        /// Supports: type System.Windows.Forms.Clipboard, System.Windows.Forms, Version=4.0.0.0
        /// </summary>
        /// <param name="line">Line to parse (with variables already replaced with values, should start with "type ")</param>
        /// <returns>RegisterTypeCommand or null if not a type command</returns>
        private static RegisterTypeCommand? TryParseRegisterType(string line)
        {
            if (!line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var rest = line.Substring(5).Trim();
            if (string.IsNullOrWhiteSpace(rest))
            {
                return null;
            }

            // Split by comma to get typeName and assembly info
            var commaIndex = rest.IndexOf(',');
            if (commaIndex < 0)
            {
                // Assembly is required
                return null;
            }

            var className = rest.Substring(0, commaIndex).Trim();
            var assembly = rest.Substring(commaIndex + 1).Trim();

            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(assembly))
            {
                return null;
            }

            return new RegisterTypeCommand
            {
                ClassName = className,
                Assembly = assembly
            };
        }
    }
}

