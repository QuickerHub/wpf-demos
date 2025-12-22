using System;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;

namespace QuickerExpressionEnhanced.Parser
{
    /// <summary>
    /// Infers and resolves types when Type.GetType fails
    /// </summary>
    public static class TypeInference
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TypeInference));

        /// <summary>
        /// Format exception details focusing on LoaderException information
        /// </summary>
        private static string FormatExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Type: {ex.GetType().Name}");
            
            // Add FileName for FileNotFoundException
            if (ex is System.IO.FileNotFoundException fnf)
            {
                if (!string.IsNullOrEmpty(fnf.FileName))
                {
                    sb.AppendLine($"FileName: {fnf.FileName}");
                }
                if (!string.IsNullOrEmpty(fnf.FusionLog))
                {
                    sb.AppendLine($"FusionLog: {fnf.FusionLog}");
                }
            }
            
            // Add FileName for FileLoadException (often contains strong name errors)
            if (ex is System.IO.FileLoadException fle)
            {
                if (!string.IsNullOrEmpty(fle.FileName))
                {
                    sb.AppendLine($"FileName: {fle.FileName}");
                }
                if (!string.IsNullOrEmpty(fle.FusionLog))
                {
                    sb.AppendLine($"FusionLog: {fle.FusionLog}");
                }
            }
            
            // Add FileName for BadImageFormatException
            if (ex is BadImageFormatException bif)
            {
                if (!string.IsNullOrEmpty(bif.FileName))
                {
                    sb.AppendLine($"FileName: {bif.FileName}");
                }
            }
            
            // Check LoaderException details for ReflectionTypeLoadException
            if (ex is ReflectionTypeLoadException rtle)
            {
                if (rtle.LoaderExceptions != null && rtle.LoaderExceptions.Length > 0)
                {
                    sb.AppendLine("LoaderExceptions:");
                    for (int i = 0; i < rtle.LoaderExceptions.Length; i++)
                    {
                        var loaderEx = rtle.LoaderExceptions[i];
                        if (loaderEx != null)
                        {
                            sb.AppendLine($"  [{i}] {loaderEx.GetType().Name}: {loaderEx.Message}");
                            
                            // Add FileName for FileNotFoundException
                            if (loaderEx is System.IO.FileNotFoundException loaderFnf && !string.IsNullOrEmpty(loaderFnf.FileName))
                            {
                                sb.AppendLine($"      文件名: {loaderFnf.FileName}");
                                if (!string.IsNullOrEmpty(loaderFnf.FusionLog))
                                {
                                    sb.AppendLine($"      FusionLog: {loaderFnf.FusionLog}");
                                }
                            }
                            
                            // Add FileName for FileLoadException (strong name errors)
                            if (loaderEx is System.IO.FileLoadException loaderFle && !string.IsNullOrEmpty(loaderFle.FileName))
                            {
                                sb.AppendLine($"      文件名: {loaderFle.FileName}");
                                if (!string.IsNullOrEmpty(loaderFle.FusionLog))
                                {
                                    sb.AppendLine($"      FusionLog: {loaderFle.FusionLog}");
                                }
                            }
                            
                            // Add FileName for BadImageFormatException
                            if (loaderEx is BadImageFormatException loaderBif && !string.IsNullOrEmpty(loaderBif.FileName))
                            {
                                sb.AppendLine($"      文件名: {loaderBif.FileName}");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"  [{i}] (null)");
                        }
                    }
                }
            }
            
            // Add inner exception details
            if (ex.InnerException != null)
            {
                sb.AppendLine($"InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                if (ex.InnerException is System.IO.FileNotFoundException innerFnf && !string.IsNullOrEmpty(innerFnf.FileName))
                {
                    sb.AppendLine($"InnerException FileName: {innerFnf.FileName}");
                }
                if (ex.InnerException is System.IO.FileLoadException innerFle && !string.IsNullOrEmpty(innerFle.FileName))
                {
                    sb.AppendLine($"InnerException FileName: {innerFle.FileName}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Get type by name and assembly, with fallback inference if Type.GetType fails
        /// Efficiently searches all loaded assemblies first, then loads the specified assembly if needed
        /// </summary>
        /// <param name="typeName">Full type name (e.g., "System.Windows.Forms.Clipboard")</param>
        /// <param name="assemblyName">Assembly name (e.g., "System.Windows.Forms" or "System.Windows.Forms, Version=4.0.0.0")</param>
        /// <returns>Resolved type or null if not found</returns>
        public static Type? GetType(string typeName, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
            }

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));
            }

            // Extract base assembly name (before comma) for loading
            var baseAssemblyName = assemblyName.Split(',')[0].Trim();

            // Check if assemblyName is a file path - if so, skip Type.GetType (it doesn't support file paths)
            var isFilePath = IsFilePath(baseAssemblyName);

            Type? type = null;

            // If a file path is specified, prioritize loading from that specific file
            // This ensures we use the exact version the user specified, not an already loaded version
            if (isFilePath)
            {
                try
                {
                    var assembly = LoadAssembly(baseAssemblyName);
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        _log.Debug($"Resolved type from specified assembly file: {typeName} in {baseAssemblyName}");
                        return type;
                    }
                }
                catch (Exception ex)
                {
                    var details = FormatExceptionDetails(ex);
                    _log.Warn($"Failed to load assembly from file path '{baseAssemblyName}' for type inference:\n{details}");
                }
            }
            else
            {
                // First, try Type.GetType with full assembly name (only if not a file path)
                var fullyQualifiedTypeName = $"{typeName}, {assemblyName}";
                type = Type.GetType(fullyQualifiedTypeName);
                if (type != null)
                {
                    _log.Debug($"Resolved type using Type.GetType: {fullyQualifiedTypeName}");
                    return type;
                }

                // Search in all currently loaded assemblies, sorted by segment match score
                type = SearchInLoadedAssemblies(typeName);
                if (type != null)
                {
                    _log.Debug($"Resolved type from loaded assemblies: {typeName} in {type.Assembly.GetName().Name}");
                    return type;
                }

                // If not found in loaded assemblies, try to load the specified assembly
                try
                {
                    var assembly = LoadAssembly(baseAssemblyName);
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        _log.Debug($"Resolved type from loaded assembly: {typeName} in {baseAssemblyName}");
                        return type;
                    }
                }
                catch (Exception ex)
                {
                    var details = FormatExceptionDetails(ex);
                    _log.Warn($"Failed to load assembly '{baseAssemblyName}' for type inference:\n{details}");
                }
            }

            // Last resort: try Type.GetType with just the type name (may work if assembly is already loaded)
            type = Type.GetType(typeName);
            if (type != null)
            {
                _log.Debug($"Resolved type using Type.GetType without assembly: {typeName}");
                return type;
            }

            _log.Warn($"Failed to resolve type: {typeName} in assembly: {assemblyName}");
            return null;
        }

        /// <summary>
        /// Search for type in all currently loaded assemblies
        /// Prioritizes assemblies with higher name match score (prefix overlap with type name)
        /// </summary>
        /// <param name="typeName">Full type name to search for</param>
        /// <returns>Found type or null</returns>
        private static Type? SearchInLoadedAssemblies(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Calculate match score for each assembly and sort by score (descending)
            var assembliesWithScore = assemblies
                .Select(asm => new
                {
                    Assembly = asm,
                    Score = CalculateMatchScore(typeName, asm.GetName().Name ?? string.Empty)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            // Search in order of match score (highest first)
            foreach (var item in assembliesWithScore)
            {
                try
                {
                    var type = item.Assembly.GetType(typeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Ignore exceptions from individual assemblies and continue searching
                }
            }
            return null;
        }

        /// <summary>
        /// Calculate match score between type name and assembly name
        /// Returns the number of matching segments (parts separated by dots) from the start
        /// </summary>
        /// <param name="typeName">Type name (e.g., "System.Windows.Forms.Clipboard")</param>
        /// <param name="assemblyName">Assembly name (e.g., "System.Windows.Forms")</param>
        /// <returns>Match score (number of matching segments from the start)</returns>
        private static int CalculateMatchScore(string typeName, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(assemblyName))
            {
                return 0;
            }

            // Split by dots to get segments
            var typeSegments = typeName.Split('.');
            var assemblySegments = assemblyName.Split('.');

            // Count matching segments from the start
            int matchCount = 0;
            int minLength = Math.Min(typeSegments.Length, assemblySegments.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (string.Equals(typeSegments[i], assemblySegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    matchCount++;
                }
                else
                {
                    break;
                }
            }

            return matchCount;
        }

        /// <summary>
        /// Load assembly by name or path
        /// </summary>
        /// <param name="assemblyName">Assembly name or path</param>
        /// <returns>Loaded assembly</returns>
        private static Assembly LoadAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));
            }

            // Check if it's a file path first
            if (IsFilePath(assemblyName))
            {
                try
                {
                    return Assembly.LoadFrom(assemblyName);
                }
                catch (Exception loadEx)
                {
                    var details = FormatExceptionDetails(loadEx);
                    _log.Error($"Failed to load assembly from file path '{assemblyName}':\n{details}", loadEx);
                    throw new InvalidOperationException($"Failed to load assembly from file path '{assemblyName}'.\n\n{details}", loadEx);
                }
            }
            else
            {
                // It's an assembly name, try Load first
                try
                {
                    return Assembly.Load(assemblyName);
                }
                catch (Exception loadEx)
                {
                    // If Load fails, try GetType as fallback
                    var type = Type.GetType(assemblyName);
                    if (type != null)
                    {
                        return type.Assembly;
                    }
                    else
                    {
                        var details = FormatExceptionDetails(loadEx);
                        _log.Error($"Failed to load assembly '{assemblyName}':\n{details}", loadEx);
                        throw new InvalidOperationException($"Failed to load assembly '{assemblyName}'. Assembly.Load failed, and Type.GetType returned null.\n\n{details}", loadEx);
                    }
                }
            }
        }

        /// <summary>
        /// Check if the string is a file path
        /// </summary>
        /// <param name="path">String to check</param>
        /// <returns>True if it appears to be a file path</returns>
        private static bool IsFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // Check for path separators
            if (path.Contains(System.IO.Path.DirectorySeparatorChar.ToString()) || path.Contains(System.IO.Path.AltDirectorySeparatorChar.ToString()))
            {
                return true;
            }

            // Check for file extension (common assembly extensions)
            var extension = System.IO.Path.GetExtension(path);
            if (!string.IsNullOrEmpty(extension))
            {
                var ext = extension.ToLowerInvariant();
                if (ext == ".dll" || ext == ".exe")
                {
                    return true;
                }
            }

            // Check if it's an absolute path (starts with drive letter on Windows or / on Unix)
            if (System.IO.Path.IsPathRooted(path))
            {
                return true;
            }

            return false;
        }
    }
}

