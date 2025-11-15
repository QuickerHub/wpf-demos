using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Z.Expressions;
using log4net;

namespace QuickerExpressionEnhanced.Parser
{
    /// <summary>
    /// Executor for registration commands
    /// Registers assemblies, namespaces, and types to EvalContext
    /// </summary>
    public static class RegistrationCommandExecutor
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RegistrationCommandExecutor));

        /// <summary>
        /// Register parsed commands to EvalContext
        /// Executes load, using, and type commands to register assemblies, namespaces, and types
        /// </summary>
        /// <param name="eval">Eval context to register to</param>
        /// <param name="commands">List of registration commands to execute</param>
        public static void Register(EvalContext eval, List<RegistrationCommand> commands)
        {
            if (eval == null)
            {
                throw new ArgumentNullException(nameof(eval), "Eval context cannot be null");
            }

            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands), "Commands list cannot be null");
            }

            foreach (var command in commands)
            {
                switch (command)
                {
                    case LoadAssemblyCommand loadCmd:
                        RegisterAssembly(eval, loadCmd.Assembly);
                        break;
                    case UsingNamespaceCommand usingCmd:
                        RegisterNamespace(eval, usingCmd.Namespace, usingCmd.Assembly);
                        break;
                    case RegisterTypeCommand typeCmd:
                        RegisterType(eval, typeCmd.ClassName, typeCmd.Assembly);
                        break;
                }
            }
        }

        /// <summary>
        /// Register assembly to EvalContext
        /// </summary>
        /// <param name="eval">Eval context</param>
        /// <param name="assemblyName">Assembly name or path</param>
        private static void RegisterAssembly(EvalContext eval, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));
            }

            Assembly assembly;

            // Check if it's a file path first
            if (IsFilePath(assemblyName))
            {
                // It's a file path, use LoadFrom
                assembly = Assembly.LoadFrom(assemblyName);
                _log.Debug($"Loaded assembly from file path: {assemblyName}");
            }
            else
            {
                // It's an assembly name, use Assembly.Load
                try
                {
                    assembly = Assembly.Load(assemblyName);
                    _log.Debug($"Loaded assembly by name: {assemblyName}");
                }
                catch (Exception loadEx)
                {
                    throw new InvalidOperationException($"Failed to load assembly '{assemblyName}'. Assembly.Load failed: {loadEx.Message}", loadEx);
                }
            }

            // Try to register assembly, but handle ReflectionTypeLoadException gracefully
            // This can happen when assembly has missing dependencies but we still want to use specific types
            try
            {
                eval.RegisterAssembly(assembly);
                _log.Debug($"Registered assembly: {assemblyName}");
            }
            catch (ReflectionTypeLoadException typeLoadEx)
            {
                // Log warning but continue - we can still use specific types from the assembly via TypeInference
                var loaderExceptions = typeLoadEx.LoaderExceptions ?? Array.Empty<Exception>();
                var exceptionMessages = string.Join("; ", loaderExceptions.Take(5).Select(ex => ex?.Message ?? "Unknown error"));
                _log.Warn($"Failed to register all types from assembly '{assemblyName}' due to missing dependencies: {exceptionMessages}. Assembly is loaded and specific types can still be registered individually.");
                
                // Note: We don't throw here because:
                // 1. The assembly is already loaded in memory
                // 2. TypeInference.GetType can still find types from loaded assemblies
                // 3. RegisterType commands will work via TypeInference even if RegisterAssembly failed
            }
            catch (Exception ex)
            {
                // For other exceptions, log and continue as well
                // The assembly is loaded, so type registration may still work
                _log.Warn($"Failed to register assembly '{assemblyName}' to EvalContext: {ex.Message}. Assembly is loaded and specific types can still be registered individually.");
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
            if (path.Contains(Path.DirectorySeparatorChar.ToString()) || path.Contains(Path.AltDirectorySeparatorChar.ToString()))
            {
                return true;
            }

            // Check for file extension (common assembly extensions)
            var extension = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(extension))
            {
                var ext = extension.ToLowerInvariant();
                if (ext == ".dll" || ext == ".exe")
                {
                    return true;
                }
            }

            // Check if it's an absolute path (starts with drive letter on Windows or / on Unix)
            if (Path.IsPathRooted(path))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Register namespace to EvalContext
        /// </summary>
        /// <param name="eval">Eval context</param>
        /// <param name="namespaceName">Namespace name</param>
        /// <param name="assemblyName">Assembly name (optional, used to load assembly if needed)</param>
        private static void RegisterNamespace(EvalContext eval, string namespaceName, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));
            }

            // Load assembly if provided
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException($"Assembly name is required to register namespace '{namespaceName}'", nameof(assemblyName));
            }

            // Load assembly (but don't register it - that should be done via load command)
            var assembly = LoadAssembly(assemblyName);

            // Register namespace - requires assembly
            eval.RegisterNamespace(assembly, namespaceName);
            _log.Debug($"Registered namespace: {namespaceName} from assembly: {assemblyName}");
        }

        /// <summary>
        /// Register type to EvalContext
        /// </summary>
        /// <param name="eval">Eval context</param>
        /// <param name="className">Class name (namespace.class format)</param>
        /// <param name="assemblyName">Assembly name (may include version info, e.g., "System.Windows.Forms, Version=4.0.0.0")</param>
        private static void RegisterType(EvalContext eval, string className, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException("Class name cannot be null or empty", nameof(className));
            }

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Assembly name is required to register type", nameof(assemblyName));
            }

            // Use TypeInference to get the type (will try multiple methods if Type.GetType fails)
            var type = TypeInference.GetType(className, assemblyName) 
                ?? throw new InvalidOperationException($"Failed to find type '{className}' in assembly '{assemblyName}'. Make sure the type name and assembly name are correct.");
            
            eval.RegisterType(type);
            _log.Debug($"Registered type: {className} from assembly: {assemblyName}");
        }

        /// <summary>
        /// Load assembly by name or path
        /// </summary>
        /// <param name="assemblyName">Assembly name or path</param>
        /// <returns>Loaded assembly</returns>
        /// <exception cref="InvalidOperationException">Thrown when assembly cannot be loaded</exception>
        private static Assembly LoadAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));
            }

            // Check if it's a file path first
            if (IsFilePath(assemblyName))
            {
                // It's a file path, use LoadFrom
                return Assembly.LoadFrom(assemblyName);
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
                        throw new InvalidOperationException($"Failed to load assembly '{assemblyName}'. Assembly.Load failed: {loadEx.Message}, and Type.GetType returned null.", loadEx);
                    }
                }
            }
        }
    }
}

