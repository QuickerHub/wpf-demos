namespace BatchRenameTool.Services;

/// <summary>
/// Manages template pattern generation (prefix and suffix)
/// </summary>
public static class TemplatePatternManager
{
    /// <summary>
    /// Generate pattern with prefix: variable + {name}.{ext}
    /// </summary>
    /// <param name="prefix">Prefix variable</param>
    /// <param name="currentPattern">Current pattern (optional)</param>
    /// <returns>Generated pattern</returns>
    public static string GeneratePrefixPattern(string prefix, string? currentPattern = null)
    {
        // If current pattern is empty or null, use default pattern
        if (string.IsNullOrWhiteSpace(currentPattern))
        {
            return $"{prefix}{{name}}.{{ext}}";
        }

        // If current pattern already contains {name} and {ext}, insert prefix before {name}
        if (currentPattern.Contains("{name}") && currentPattern.Contains("{ext}"))
        {
            return currentPattern.Replace("{name}", $"{prefix}{{name}}");
        }

        // Otherwise, prepend prefix to the pattern
        return $"{prefix}{currentPattern}";
    }

    /// <summary>
    /// Generate pattern with suffix: {name} + variable + .{ext}
    /// </summary>
    /// <param name="suffix">Suffix variable</param>
    /// <param name="currentPattern">Current pattern (optional)</param>
    /// <returns>Generated pattern</returns>
    public static string GenerateSuffixPattern(string suffix, string? currentPattern = null)
    {
        // If current pattern is empty or null, use default pattern
        if (string.IsNullOrWhiteSpace(currentPattern))
        {
            return $"{{name}}{suffix}.{{ext}}";
        }

        // If current pattern contains {name} and {ext}, insert suffix between them
        if (currentPattern.Contains("{name}") && currentPattern.Contains("{ext}"))
        {
            return currentPattern.Replace("{name}", $"{{name}}{suffix}");
        }

        // If pattern contains {name} but not {ext}, append suffix after {name}
        if (currentPattern.Contains("{name}"))
        {
            return currentPattern.Replace("{name}", $"{{name}}{suffix}");
        }

        // Otherwise, append suffix to the pattern
        return $"{currentPattern}{suffix}";
    }
}
