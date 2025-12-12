using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace BatchRenameTool.Utils;

/// <summary>
/// Utility class for loading embedded resources
/// </summary>
public static class ResourceLoader
{
    /// <summary>
    /// Creates pack URI for resource
    /// </summary>
    private static Uri CreatePackUri(string path, Assembly assembly)
    {
        string assemblyName = assembly.GetName().Name!;
        return new Uri($"pack://application:,,,/{assemblyName};component/{path}");
    }

    /// <summary>
    /// Reads text content from embedded resource
    /// </summary>
    /// <param name="path">Resource path (e.g., "Help.md")</param>
    /// <param name="assembly">Assembly containing the resource (optional)</param>
    /// <returns>Text content of the resource</returns>
    public static string ReadText(string path, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        try
        {
            var resourceInfo = Application.GetResourceStream(CreatePackUri(path, assembly));
            if (resourceInfo?.Stream == null)
                throw new FileNotFoundException($"Resource not found: {path}");

            using var stream = resourceInfo.Stream;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Failed to load resource: {path}", ex);
        }
    }
}
