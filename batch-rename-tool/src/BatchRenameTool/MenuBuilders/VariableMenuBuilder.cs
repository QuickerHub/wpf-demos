using System.Collections.Generic;
using System.Windows.Controls;
using BatchRenameTool.Template;

namespace BatchRenameTool.MenuBuilders;

/// <summary>
/// Builder for creating variable menu items dynamically
/// </summary>
public static class VariableMenuBuilder
{
    /// <summary>
    /// Build menu items for all variables with their format options
    /// </summary>
    /// <param name="onVariableSelected">Callback when a variable is selected (variable name, format option text or null)</param>
    /// <returns>List of menu items</returns>
    public static List<MenuItem> BuildVariableMenuItems(System.Action<string, string?> onVariableSelected)
    {
        var menuItems = new List<MenuItem>();
        var allVariables = VariableInfo.GetAllVariables();

        foreach (var variable in allVariables)
        {
            // Extract short title from description (before first period or comma, or first 10 characters)
            var shortTitle = GetShortTitle(variable.Description);
            
            // Format: "短标题(varname)"
            var menuItem = new MenuItem
            {
                Header = $"{shortTitle}({variable.VariableName})",
                ToolTip = variable.Description
            };

            // If variable has format options, create submenu
            if (variable.FormatOptions != null && variable.FormatOptions.Count > 0)
            {
                foreach (var formatOption in variable.FormatOptions)
                {
                    var formatMenuItem = new MenuItem
                    {
                        Header = formatOption.Text,
                        ToolTip = formatOption.Description
                    };

                    // Create the pattern text with format
                    var patternText = $"{{{variable.VariableName}:{formatOption.Text}}}";
                    formatMenuItem.Click += (s, e) =>
                    {
                        onVariableSelected(variable.VariableName, formatOption.Text);
                    };

                    menuItem.Items.Add(formatMenuItem);
                }
            }
            else
            {
                // No format options, just insert variable name
                var patternText = $"{{{variable.VariableName}}}";
                menuItem.Click += (s, e) =>
                {
                    onVariableSelected(variable.VariableName, null);
                };
            }

            menuItems.Add(menuItem);
        }

        return menuItems;
    }

    /// <summary>
    /// Extract short title from description
    /// </summary>
    private static string GetShortTitle(string description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        // Try to find the first sentence (before period, comma, or colon)
        var periodIndex = description.IndexOf('。');
        var commaIndex = description.IndexOf('，');
        var colonIndex = description.IndexOf('：');
        var dotIndex = description.IndexOf('.');

        int endIndex = -1;
        if (periodIndex > 0) endIndex = periodIndex;
        if (commaIndex > 0 && (endIndex == -1 || commaIndex < endIndex)) endIndex = commaIndex;
        if (colonIndex > 0 && (endIndex == -1 || colonIndex < endIndex)) endIndex = colonIndex;
        if (dotIndex > 0 && (endIndex == -1 || dotIndex < endIndex)) endIndex = dotIndex;

        if (endIndex > 0)
        {
            return description.Substring(0, endIndex).Trim();
        }

        // If no punctuation found, take first 10 characters
        return description.Length > 10 ? description.Substring(0, 10) + "..." : description;
    }
}
