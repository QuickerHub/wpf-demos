using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    /// - type {class} {assembly}
    /// </summary>
    public static class RegistrationCommandParser
    {
        // Regex patterns for matching commands
        // Support both //comment format (for expressions) and direct format (for external calls)
        private static readonly Regex LoadPattern = new Regex(@"^\s*(//)?\s*load\s+(\{[^}]+\}|[^\s]+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex UsingPattern = new Regex(@"^\s*(//)?\s*using\s+(\{[^}]+\}|[^\s]+)\s+(\{[^}]+\}|[^\s]+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex TypePattern = new Regex(@"^\s*(//)?\s*type\s+(\{[^}]+\}|[^\s]+)\s+(\{[^}]+\}|[^\s]+)\s*$", RegexOptions.IgnoreCase);

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
                
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    remainingLines.Add(originalLine);
                    continue;
                }

                RegistrationCommand? command = null;

                // Try to parse as LoadAssemblyCommand: load {assembly}
                var loadCommand = TryParseLoadAssembly(trimmedLine);
                if (loadCommand != null)
                {
                    command = loadCommand;
                }
                else
                {
                    // Try to parse as UsingNamespaceCommand: using {namespace} {assembly}
                    var usingCommand = TryParseUsingNamespace(trimmedLine);
                    if (usingCommand != null)
                    {
                        command = usingCommand;
                    }
                    else
                    {
                        // Try to parse as RegisterTypeCommand: type {class} {assembly}
                        var typeCommand = TryParseRegisterType(trimmedLine);
                        if (typeCommand != null)
                        {
                            command = typeCommand;
                        }
                    }
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
        /// <param name="line">Line to parse (with variables already replaced with values)</param>
        /// <returns>LoadAssemblyCommand or null if not a load command</returns>
        private static LoadAssemblyCommand? TryParseLoadAssembly(string line)
        {
            var match = LoadPattern.Match(line);
            if (match.Success)
            {
                var assembly = match.Groups[2].Value.Trim();
                return new LoadAssemblyCommand
                {
                    Assembly = assembly
                };
            }
            return null;
        }

        /// <summary>
        /// Try to parse a line as UsingNamespaceCommand: using {namespace} {assembly}
        /// </summary>
        /// <param name="line">Line to parse (with variables already replaced with values)</param>
        /// <returns>UsingNamespaceCommand or null if not a using command</returns>
        private static UsingNamespaceCommand? TryParseUsingNamespace(string line)
        {
            var match = UsingPattern.Match(line);
            if (match.Success)
            {
                var namespaceValue = match.Groups[2].Value.Trim();
                var assembly = match.Groups[3].Value.Trim();
                return new UsingNamespaceCommand
                {
                    Namespace = namespaceValue,
                    Assembly = assembly
                };
            }
            return null;
        }

        /// <summary>
        /// Try to parse a line as RegisterTypeCommand: type {class} {assembly}
        /// </summary>
        /// <param name="line">Line to parse (with variables already replaced with values)</param>
        /// <returns>RegisterTypeCommand or null if not a type command</returns>
        private static RegisterTypeCommand? TryParseRegisterType(string line)
        {
            var match = TypePattern.Match(line);
            if (match.Success)
            {
                var className = match.Groups[2].Value.Trim();
                var assembly = match.Groups[3].Value.Trim();
                return new RegisterTypeCommand
                {
                    ClassName = className,
                    Assembly = assembly
                };
            }
            return null;
        }
    }
}

