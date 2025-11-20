using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Program manager for version control and exiting other versions
/// </summary>
public static class ProgramManager
{
    public static string ProjectName { get; } = "QuickerExpressionAgent.Quicker";

    public static Version CurrentVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    /// <summary>
    /// Get running assemblies with similar names
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Assembly> GetRunningAssembly()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.GetName().Name?.StartsWith(ProjectName) == true);
    }

    /// <summary>
    /// Exit other version programs
    /// </summary>
    public static void ExitOtherVersionProgram()
    {
        foreach (var assembly in GetRunningAssembly())
        {
            try
            {
                // Skip self
                if (Assembly.GetExecutingAssembly().FullName == assembly.FullName)
                {
                    continue;
                }

                // Try to find Exit method in Launcher class
                var launcherType = assembly.GetType($"{ProjectName}.Launcher");
                var exitMethod = launcherType?.GetMethod("Exit", BindingFlags.Public | BindingFlags.Static);

                if (exitMethod != null)
                {
                    exitMethod.Invoke(null, null);
                }
            }
            catch
            {
                // Ignore errors when trying to exit other versions
            }
        }
    }
}

