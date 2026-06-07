using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace QuickerExpressionEnhanced.Parser;

/// <summary>
/// Process-wide assembly load cache keyed by normalized path or assembly name.
/// </summary>
internal static class AssemblyLoadCache
{
    private static readonly ConcurrentDictionary<string, Assembly> Loaded =
        new(StringComparer.OrdinalIgnoreCase);

    public static Assembly GetOrLoad(string assemblyNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyNameOrPath))
        {
            throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyNameOrPath));
        }

        var key = NormalizeKey(assemblyNameOrPath);
        if (Loaded.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var assembly = LoadCore(assemblyNameOrPath);
        Loaded[key] = assembly;
        return assembly;
    }

    private static string NormalizeKey(string assemblyNameOrPath)
    {
        if (IsFilePath(assemblyNameOrPath))
        {
            return Path.GetFullPath(assemblyNameOrPath);
        }

        return assemblyNameOrPath.Trim();
    }

    private static Assembly LoadCore(string assemblyNameOrPath)
    {
        if (IsFilePath(assemblyNameOrPath))
        {
            return Assembly.LoadFrom(assemblyNameOrPath);
        }

        try
        {
            return Assembly.Load(assemblyNameOrPath);
        }
        catch (Exception loadEx)
        {
            var type = Type.GetType(assemblyNameOrPath);
            if (type is not null)
            {
                return type.Assembly;
            }

            throw;
        }
    }

    private static bool IsFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.Contains(Path.DirectorySeparatorChar.ToString())
            || path.Contains(Path.AltDirectorySeparatorChar.ToString()))
        {
            return true;
        }

        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension))
        {
            var ext = extension.ToLowerInvariant();
            if (ext is ".dll" or ".exe")
            {
                return true;
            }
        }

        return Path.IsPathRooted(path);
    }
}
