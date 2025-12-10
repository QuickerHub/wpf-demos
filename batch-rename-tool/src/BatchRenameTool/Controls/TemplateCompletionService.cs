using System;
using System.Collections.Generic;
using System.Linq;

namespace BatchRenameTool.Controls;

/// <summary>
/// Completion service for template variables and methods
/// </summary>
public class TemplateCompletionService : ICompletionService
{
    private readonly Dictionary<string, string> _variableDescriptions;
    private readonly List<MethodInfo> _stringMethods;

    public TemplateCompletionService()
    {
        _variableDescriptions = new Dictionary<string, string>
        {
            { "name", "原文件名（不含扩展名）" },
            { "ext", "文件扩展名（不含点号）" },
            { "fullname", "完整文件名（包含扩展名）" },
            { "i", "序号变量，从0开始。可使用格式：{i:00}, {i:01}, {i:1}, {i:零}, {i:一}, {i:壹}" }
        };

        _stringMethods = new List<MethodInfo>
        {
            new MethodInfo
            {
                Name = "replace",
                Aliases = new[] { "替换" },
                Description = "替换字符串。用法：{name.replace(old,new)}",
                HasParameters = true
            },
            new MethodInfo
            {
                Name = "upper",
                Aliases = new[] { "大写" },
                Description = "转换为大写。用法：{name.upper} 或 {name.upper()}",
                HasParameters = false
            },
            new MethodInfo
            {
                Name = "lower",
                Aliases = new[] { "小写" },
                Description = "转换为小写。用法：{name.lower} 或 {name.lower()}",
                HasParameters = false
            },
            new MethodInfo
            {
                Name = "trim",
                Aliases = new[] { "去空格" },
                Description = "去除首尾空格。用法：{name.trim} 或 {name.trim()}",
                HasParameters = false
            },
            new MethodInfo
            {
                Name = "sub",
                Aliases = new[] { "截取", "切片" },
                Description = "截取字符串。用法：{name.sub(start)} 或 {name.sub(start,end)} 或 {name[start:end]}",
                HasParameters = true
            },
            new MethodInfo
            {
                Name = "padLeft",
                Aliases = new[] { "左填充" },
                Description = "左侧填充。用法：{name.padLeft(10)} 或 {name.padLeft(10,0)}",
                HasParameters = true
            },
            new MethodInfo
            {
                Name = "padRight",
                Aliases = new[] { "右填充" },
                Description = "右侧填充。用法：{name.padRight(10)} 或 {name.padRight(10,-)}",
                HasParameters = true
            }
        };
    }

    public CompletionContext? GetCompletionContext(string documentText, int caretOffset, char triggerChar)
    {
        if (triggerChar == '{')
        {
            return GetVariableCompletionContext(documentText, caretOffset);
        }
        else if (triggerChar == '.')
        {
            return GetMethodCompletionContext(documentText, caretOffset);
        }
        else if (triggerChar == ':')
        {
            return GetFormatCompletionContext(documentText, caretOffset);
        }
        else
        {
            // Check if cursor is inside braces - if so, trigger variable completion
            var bracePosition = IsInsideBraces(documentText, caretOffset);
            if (bracePosition >= 0)
            {
                // Cursor is inside braces, trigger variable completion
                return GetVariableCompletionContext(documentText, caretOffset);
            }
        }

        return null;
    }

    public CompletionContext? UpdateCompletionContext(CompletionContext context, string documentText, int caretOffset)
    {
        // Check if cursor is still inside braces
        var bracePosition = IsInsideBraces(documentText, caretOffset);
        if (bracePosition < 0)
        {
            return null; // Cursor moved outside braces, close completion
        }

        // Check if context is still valid
        if (caretOffset < context.ReplaceStartOffset || caretOffset > documentText.Length)
        {
            return null; // Context invalid, close completion
        }

        // Check if closing brace was typed between ReplaceStartOffset and cursor
        for (int i = context.ReplaceStartOffset + 1; i < caretOffset && i < documentText.Length; i++)
        {
            if (documentText[i] == '}')
            {
                return null; // Closing brace found, close completion
            }
        }

        // Get filter text (text between FilterStartOffset and cursor)
        string filterText = string.Empty;
        if (caretOffset > context.FilterStartOffset && context.FilterStartOffset < documentText.Length)
        {
            var filterLength = Math.Min(caretOffset - context.FilterStartOffset, documentText.Length - context.FilterStartOffset);
            if (filterLength > 0)
            {
                filterText = documentText.Substring(context.FilterStartOffset, filterLength);
            }
        }

        // Filter items based on current text using original items (not filtered items)
        var originalItems = context.OriginalItems.Count > 0 ? context.OriginalItems : context.Items;
        var filteredItems = FilterCompletionItems(originalItems, filterText);

        return new CompletionContext
        {
            Type = context.Type, // Preserve completion type
            Items = filteredItems,
            OriginalItems = originalItems, // Preserve original items
            ReplaceStartOffset = context.ReplaceStartOffset,
            ReplaceEndOffset = caretOffset,
            FilterStartOffset = context.FilterStartOffset
        };
    }

    public int IsInsideBraces(string documentText, int caretOffset)
    {
        if (caretOffset < 1 || caretOffset > documentText.Length)
        {
            return -1;
        }

        // Search backwards from cursor to find the nearest opening brace
        int bracePosition = -1;
        for (int i = caretOffset - 1; i >= 0; i--)
        {
            if (documentText[i] == '{')
            {
                bracePosition = i;
                break;
            }
            if (documentText[i] == '}')
            {
                // Found closing brace before opening brace, not inside braces
                return -1;
            }
        }

        if (bracePosition < 0)
        {
            return -1; // No opening brace found
        }

        // Check if there's a closing brace after the opening brace and before cursor
        for (int i = bracePosition + 1; i < caretOffset && i < documentText.Length; i++)
        {
            if (documentText[i] == '}')
            {
                // Found closing brace before cursor, not inside braces
                return -1;
            }
        }

        return bracePosition; // Cursor is inside braces
    }

    private CompletionContext? GetVariableCompletionContext(string documentText, int caretOffset)
    {
        // Check if cursor is inside braces
        var bracePosition = IsInsideBraces(documentText, caretOffset);
        if (bracePosition < 0)
        {
            return null; // Not inside braces
        }

        // FilterStartOffset should be right after '{' for filtering
        var filterStartOffset = bracePosition + 1;
        
        // Get filter text (text between '{' and cursor)
        string filterText = string.Empty;
        if (caretOffset > filterStartOffset && filterStartOffset < documentText.Length)
        {
            var filterLength = Math.Min(caretOffset - filterStartOffset, documentText.Length - filterStartOffset);
            if (filterLength > 0)
            {
                filterText = documentText.Substring(filterStartOffset, filterLength);
            }
        }

        // Create all items first
        var allItems = _variableDescriptions.Keys.Select(key => new CompletionItem
        {
            Text = key,
            DisplayText = $"{{{key}}}",
            Description = _variableDescriptions[key],
            ReplacementText = $"{{{key}}}",
            CursorOffset = -1 // Position cursor before closing '}' to allow typing {name|}
        }).ToList();

        // Filter items based on current input
        var filteredItems = FilterCompletionItems(allItems, filterText);

        return new CompletionContext
        {
            Type = CompletionType.Variable,
            Items = filteredItems,
            OriginalItems = allItems, // Store original items for re-filtering
            ReplaceStartOffset = bracePosition, // Replace from '{'
            ReplaceEndOffset = caretOffset, // Replace to current cursor
            FilterStartOffset = filterStartOffset // Filter starts after '{'
        };
    }

    private CompletionContext? GetMethodCompletionContext(string documentText, int caretOffset)
    {
        // Check if there's a '.' before cursor
        if (caretOffset < 2 || caretOffset > documentText.Length)
        {
            return null;
        }

        if (documentText[caretOffset - 1] != '.')
        {
            return null;
        }

        // Find the opening brace and variable name
        int braceStart = -1;
        string? variableName = null;

        for (int i = caretOffset - 2; i >= 0 && i >= caretOffset - 50; i--)
        {
            if (i < documentText.Length)
            {
                var ch = documentText[i];
                if (ch == '{')
                {
                    braceStart = i;
                    // Extract variable name
                    if (i + 1 < caretOffset - 1)
                    {
                        variableName = documentText.Substring(i + 1, caretOffset - 1 - i - 1);
                    }
                    break;
                }
                if (ch == '}')
                {
                    return null; // Found closing brace, not in completion context
                }
            }
        }

        if (braceStart < 0 || string.IsNullOrEmpty(variableName))
        {
            return null;
        }

        // Only show methods for string variables (not 'i')
        if (variableName.Equals("i", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var dotPosition = caretOffset - 1;
        var items = new List<CompletionItem>();

        foreach (var method in _stringMethods)
        {
            // Determine replacement text and cursor offset based on method parameters
            string replacementText;
            int cursorOffset;
            if (method.HasParameters)
            {
                replacementText = $".{method.Name}()";
                cursorOffset = -1; // Position cursor before ')' to allow typing parameters
            }
            else
            {
                replacementText = $".{method.Name}";
                cursorOffset = 0; // Position cursor after method name
            }

            // Add method name
            items.Add(new CompletionItem
            {
                Text = method.Name,
                DisplayText = $".{method.Name}()",
                Description = method.Description,
                ReplacementText = replacementText,
                CursorOffset = cursorOffset,
                Metadata = method // Store method info for completion
            });

            // Add aliases
            foreach (var alias in method.Aliases)
            {
                items.Add(new CompletionItem
                {
                    Text = alias,
                    DisplayText = $".{alias}()",
                    Description = method.Description,
                    ReplacementText = replacementText,
                    CursorOffset = cursorOffset,
                    Metadata = method // Store method info for completion
                });
            }
        }

        return new CompletionContext
        {
            Type = CompletionType.Method,
            Items = items,
            OriginalItems = items, // Store original items for re-filtering
            ReplaceStartOffset = dotPosition, // Replace from '.'
            ReplaceEndOffset = caretOffset, // Replace to current cursor
            FilterStartOffset = caretOffset // Filter starts after '.'
        };
    }

    private CompletionContext? GetFormatCompletionContext(string documentText, int caretOffset)
    {
        // Check if we have '{i:' pattern
        if (caretOffset < 3 || caretOffset > documentText.Length)
        {
            return null;
        }

        // Find the colon position by searching backwards from cursor
        int colonPosition = -1;
        for (int i = caretOffset - 1; i >= 0 && i >= caretOffset - 10; i--) // Search up to 10 chars back
        {
            if (documentText[i] == ':')
            {
                // Check if character before ':' is 'i'
                if (i >= 1 && (documentText[i - 1] == 'i' || documentText[i - 1] == 'I'))
                {
                    // Check if character before 'i' is '{'
                    if (i >= 2 && documentText[i - 2] == '{')
                    {
                        colonPosition = i;
                        break;
                    }
                }
            }
            // Stop if we hit a '{' or '}' (we're outside the expression)
            if (documentText[i] == '{' || documentText[i] == '}')
            {
                break;
            }
        }

        if (colonPosition < 0)
        {
            return null;
        }

        var bracePosition = colonPosition - 2;
        // ReplaceStartOffset should be right after ':', regardless of what's currently there
        var replaceStartOffset = colonPosition + 1;

        var formatOptions = new[]
        {
            new { Text = "00", Description = "2位数字，从00开始：00, 01, 02..." },
            new { Text = "000", Description = "3位数字，从000开始：000, 001, 002..." },
            new { Text = "01", Description = "2位数字，从01开始：01, 02, 03..." },
            new { Text = "1", Description = "序号，从1开始：1, 2, 3..." },
            new { Text = "零", Description = "中文序号，从零开始：零, 一, 二..." },
            new { Text = "一", Description = "中文序号，从一开始：一, 二, 三..." },
            new { Text = "壹", Description = "中文序号（大写），从壹开始：壹, 贰, 叁..." }
        };

        var items = formatOptions.Select(opt => new CompletionItem
        {
            Text = opt.Text,
            DisplayText = opt.Text,
            Description = opt.Description,
            ReplacementText = opt.Text,
            CursorOffset = 0 // Position cursor after format text
        }).ToList();

        return new CompletionContext
        {
            Type = CompletionType.Format,
            Items = items,
            OriginalItems = items, // Store original items for re-filtering
            ReplaceStartOffset = replaceStartOffset, // Replace from after ':' (only format part)
            ReplaceEndOffset = caretOffset, // Replace to current cursor
            FilterStartOffset = replaceStartOffset // For format completion, FilterStartOffset should be ReplaceStartOffset (after ':')
        };
    }

    private List<CompletionItem> FilterCompletionItems(List<CompletionItem> items, string filterText)
    {
        if (string.IsNullOrEmpty(filterText))
        {
            return items;
        }

        return items.Where(item =>
            item.Text.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) ||
            item.DisplayText.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public class MethodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public string Description { get; set; } = string.Empty;
        public bool HasParameters { get; set; }
    }
}
