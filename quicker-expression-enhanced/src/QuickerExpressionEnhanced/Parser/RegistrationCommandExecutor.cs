using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
                try
                {
                    assembly = Assembly.LoadFrom(assemblyName);
                    _log.Debug($"Loaded assembly from file path: {assemblyName}");
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
                // It's an assembly name, use Assembly.Load
                try
                {
                    assembly = Assembly.Load(assemblyName);
                    _log.Debug($"Loaded assembly by name: {assemblyName}");
                }
                catch (Exception loadEx)
                {
                    var details = FormatExceptionDetails(loadEx);
                    _log.Error($"Failed to load assembly by name '{assemblyName}':\n{details}", loadEx);
                    throw new InvalidOperationException($"Failed to load assembly '{assemblyName}'.\n\n{details}", loadEx);
                }
            }

            // Try to register assembly, throw exception with detailed LoaderException information if it fails
            try
            {
                eval.RegisterAssembly(assembly);
                _log.Debug($"Registered assembly: {assemblyName}");
            }
            catch (ReflectionTypeLoadException typeLoadEx)
            {
                // Format detailed LoaderException information and throw
                var details = FormatExceptionDetails(typeLoadEx);
                _log.Error($"Failed to register all types from assembly '{assemblyName}' due to missing dependencies:\n{details}", typeLoadEx);
                throw new InvalidOperationException($"无法加载程序集 '{assemblyName}' 中的一个或多个请求的类型。\n\n{details}", typeLoadEx);
            }
            catch (Exception ex)
            {
                // Format detailed exception information and throw
                var details = FormatExceptionDetails(ex);
                _log.Error($"Failed to register assembly '{assemblyName}' to EvalContext:\n{details}", ex);
                throw new InvalidOperationException($"无法注册程序集 '{assemblyName}' 到 EvalContext。\n\n{details}", ex);
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
    }
}

