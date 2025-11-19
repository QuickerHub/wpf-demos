using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Services.VariableTypeFormatters;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Variable access mode for Roslyn expression execution
/// </summary>
public enum VariableAccessMode
{
    /// <summary>
    /// Use type casting: {varname} → (Variables["varname"] as Type)
    /// Simpler implementation, no dynamic type generation
    /// </summary>
    TypeCasting,
    
    /// <summary>
    /// Use dynamic type generation: variables become properties of ScriptGlobals
    /// More performant for repeated executions, allows direct variable access
    /// </summary>
    DynamicType
}

/// <summary>
/// Service for executing C# expressions using Roslyn scripting
/// Variables are provided via dictionary and are directly accessible in scripts by name
/// </summary>
public class RoslynExpressionService : IRoslynExpressionService
{
    private readonly ScriptOptions _scriptOptions;
    private readonly VariableTypeFormatterFactory _formatterFactory;
    private readonly VariableAccessMode _accessMode;
    
    // Cache for dynamically generated types (only used in DynamicType mode)
    private readonly Dictionary<string, Type> _globalsTypeCache = new();
    private readonly object _cacheLock = new object();

    public RoslynExpressionService(VariableAccessMode accessMode = VariableAccessMode.TypeCasting)
    {
        // Initialize script options with necessary references
        // Add all necessary assemblies for common .NET types
        _scriptOptions = ScriptOptions.Default
            .WithImports("System", "System.Linq", "System.Collections.Generic")
            .WithReferences(
                typeof(object).Assembly,                                    // System.Runtime
                typeof(System.Linq.Enumerable).Assembly,                    // System.Linq
                typeof(List<>).Assembly,         // System.Collections
                typeof(System.Random).Assembly,                            // System (for Random, etc.)
                typeof(System.Console).Assembly,                           // System.Console
                typeof(System.Math).Assembly,
                typeof(Dictionary<,>).Assembly);                             // System.Math

        
        _formatterFactory = new VariableTypeFormatterFactory();
        _accessMode = accessMode;
    }
    
    /// <summary>
    /// Execute a C# expression using Roslyn scripting with dictionary-based variables
    /// Variables are accessible via type casting: {varname} is replaced with (varname as Type)
    /// </summary>
    public async Task<ExpressionResult> ExecuteExpressionAsync(
        string code,
        Dictionary<string, object>? variables = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ExpressionResult
                {
                    Success = false,
                    Error = "Expression code cannot be empty."
                };
            }

            // Debug: Log the input
            Console.WriteLine($"[Roslyn Debug] Input code: {code}");
            Console.WriteLine($"[Roslyn Debug] Variable count: {variables?.Count ?? 0}");
            if (variables != null && variables.Count > 0)
            {
                Console.WriteLine("[Roslyn Debug] Variables:");
                foreach (var kvp in variables)
                {
                    Console.WriteLine($"  - {kvp.Key}: {kvp.Value} (Type: {kvp.Value?.GetType().Name ?? "null"})");
                }
            }
            Console.WriteLine($"[Roslyn Debug] =====================");
            
            object globals;
            string processedCode;
            Type globalsType;
            List<string> usedVariableNames;
            
            // Choose implementation based on access mode
            if (_accessMode == VariableAccessMode.DynamicType)
            {
                // Method 1: Dynamic type generation - variables become properties
                globals = CreateDynamicGlobals(variables);
                globalsType = globals.GetType();
                
                // Replace {varname} with direct variable name (no casting needed)
                var (processed, used) = ProcessVariableReferencesForDirectAccess(code, variables);
                processedCode = processed;
                usedVariableNames = used;
                Console.WriteLine($"[Roslyn Debug] Using DynamicType mode - variables are direct properties");
            }
            else
            {
                // Method 2: Type casting - use Variables dictionary with type casting
                globals = new ScriptGlobals();
                if (variables != null && variables.Count > 0)
                {
                    foreach (var kvp in variables)
                    {
                        ((ScriptGlobals)globals).Variables[kvp.Key] = kvp.Value;
                    }
                }
                globalsType = typeof(ScriptGlobals);
                
                // Replace {varname} with (Variables["varname"] as Type)
                var (processed, used) = ProcessVariableReferences(code, variables);
                processedCode = processed;
                usedVariableNames = used;
                Console.WriteLine($"[Roslyn Debug] Using TypeCasting mode - variables accessed via dictionary");
            }
            
            Console.WriteLine($"[Roslyn Debug] Processed code: {processedCode}");
            
            // Create script with appropriate globals type
            var script = CSharpScript.Create(processedCode.Trim(), _scriptOptions, globalsType);
            
            // Compile and check for errors
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                var errors = string.Join("\n", diagnostics.Select(d => d.GetMessage()));
                Console.WriteLine($"[Roslyn Debug] Compilation error: {errors}");
                return new ExpressionResult
                {
                    Success = false,
                    Error = $"Compilation errors:\n{errors}"
                };
            }
            
            // Run the script with globals
            var result = await script.RunAsync(globals);
            var returnValue = result.ReturnValue;
            
            // Build list of used variables
            var usedVariables = new List<VariableClass>();
            if (variables != null && usedVariableNames.Count > 0)
            {
                foreach (var varName in usedVariableNames)
                {
                    if (variables.TryGetValue(varName, out var varValue))
                    {
                        var varType = varValue?.GetType() ?? typeof(object);
                        usedVariables.Add(new VariableClass
                        {
                            VarName = varName,
                            VarType = varType.ConvertToVariableType(),
                            DefaultValue = varValue
                        });
                    }
                }
            }
            
            return new ExpressionResult
            {
                Success = true,
                Value = returnValue,
                UsedVariables = usedVariables
            };
        }
        catch (CompilationErrorException ex)
        {
            var errors = string.Join("\n", ex.Diagnostics.Select(d => d.GetMessage()));
            return new ExpressionResult
            {
                Success = false,
                Error = $"Compilation error: {errors}",
            };
        }
        catch (Exception ex)
        {
            var errorDetails = $"Execution error: {ex.GetType().Name}\n";
            errorDetails += $"Message: {ex.Message}\n";
            if (ex.InnerException != null)
            {
                errorDetails += $"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n";
            }
            errorDetails += $"Stack Trace:\n{ex.StackTrace}";
            
            return new ExpressionResult
            {
                Success = false,
                Error = errorDetails
            };
        }
    }
    
    /// <summary>
    /// Process {varname} format and replace with type casting: (varname as Type)
    /// Returns the processed code and a list of variable names that were found in the expression
    /// </summary>
    private (string processedCode, List<string> usedVariableNames) ProcessVariableReferences(string code, Dictionary<string, object>? variables)
    {
        if (variables == null || variables.Count == 0)
        {
            return (code, new List<string>());
        }
        
        var processedCode = code;
        var usedVariableNames = new List<string>();
        
        // Replace {varname} with (Variables["varname"] as Type)
        foreach (var kvp in variables)
        {
            var varName = kvp.Key;
            var varValue = kvp.Value;
            var varType = varValue?.GetType() ?? typeof(object);
            
            // Check if this variable is used in the expression
            if (code.Contains($"{{{varName}}}"))
            {
                usedVariableNames.Add(varName);
            }
            
            // Get C# type name for casting
            var typeName = GetCSharpTypeName(varType);
            
            // Replace {varname} with (Variables["varname"] as Type)
            var replacement = $"(Variables[\"{varName}\"] as {typeName})";
            processedCode = processedCode.Replace($"{{{varName}}}", replacement);
        }
        
        return (processedCode, usedVariableNames);
    }
    
    /// <summary>
    /// Get C# type name for type casting
    /// </summary>
    private string GetCSharpTypeName(Type type)
    {
        if (type == null)
            return "object";
        
        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return GetCSharpTypeName(underlyingType) + "?";
        }
        
        // Handle generic types
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();
            
            if (genericDef == typeof(List<>))
            {
                var argType = GetCSharpTypeName(genericArgs[0]);
                return $"List<{argType}>";
            }
            
            if (genericDef == typeof(Dictionary<,>))
            {
                var keyType = GetCSharpTypeName(genericArgs[0]);
                var valueType = GetCSharpTypeName(genericArgs[1]);
                return $"Dictionary<{keyType}, {valueType}>";
            }
            
            // Other generic types
            var typeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var args = string.Join(", ", genericArgs.Select(GetCSharpTypeName));
            return $"{typeName}<{args}>";
        }
        
        // Handle simple types
        return type.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Int64" => "long",
            "Double" => "double",
            "Single" => "float",
            "Boolean" => "bool",
            "DateTime" => "DateTime",
            "Object" => "object",
            _ => type.Name
        };
    }
    
    /// <summary>
    /// Process {varname} format for direct access (DynamicType mode)
    /// Replaces {varname} with just varname (variables are properties of ScriptGlobals)
    /// Returns the processed code and a list of variable names that were found in the expression
    /// </summary>
    private (string processedCode, List<string> usedVariableNames) ProcessVariableReferencesForDirectAccess(string code, Dictionary<string, object>? variables)
    {
        if (variables == null || variables.Count == 0)
        {
            return (code, new List<string>());
        }
        
        var processedCode = code;
        var usedVariableNames = new List<string>();
        
        // Replace {varname} with just varname (direct property access)
        foreach (var kvp in variables)
        {
            var varName = kvp.Key;
            
            // Check if this variable is used in the expression
            if (code.Contains($"{{{varName}}}"))
            {
                usedVariableNames.Add(varName);
            }
            
            // Simple replacement: {varname} → varname
            processedCode = processedCode.Replace($"{{{varName}}}", varName);
        }
        
        return (processedCode, usedVariableNames);
    }
    
    /// <summary>
    /// Create a dynamic ScriptGlobals object with variables as properties (DynamicType mode)
    /// This allows scripts to directly access variables by name
    /// Uses dynamic code generation to create a strongly-typed class
    /// </summary>
    private object CreateDynamicGlobals(Dictionary<string, object>? variables)
    {
        if (variables == null || variables.Count == 0)
        {
            return new ScriptGlobals();
        }
        
        // Filter and validate variable names
        var validVariables = new Dictionary<string, object>();
        foreach (var kvp in variables)
        {
            if (IsValidCSharpIdentifier(kvp.Key))
            {
                validVariables[kvp.Key] = kvp.Value;
            }
        }
        
        if (validVariables.Count == 0)
        {
            return new ScriptGlobals();
        }
        
        // Generate a cache key based on variable names and types
        var cacheKey = GenerateCacheKey(validVariables);
        
        // Get or create the dynamic type
        Type globalsType;
        lock (_cacheLock)
        {
            if (!_globalsTypeCache.TryGetValue(cacheKey, out globalsType))
            {
                globalsType = CreateDynamicGlobalsType(validVariables);
                _globalsTypeCache[cacheKey] = globalsType;
            }
        }
        
        // Create an instance of the dynamic type and set properties
        var instance = Activator.CreateInstance(globalsType);
        if (instance == null)
        {
            return new ScriptGlobals();
        }
        
        foreach (var kvp in validVariables)
        {
            var property = globalsType.GetProperty(kvp.Key);
            if (property != null)
            {
                property.SetValue(instance, kvp.Value);
            }
        }
        
        return instance;
    }
    
    /// <summary>
    /// Generate a cache key for the variables dictionary
    /// </summary>
    private string GenerateCacheKey(Dictionary<string, object> variables)
    {
        var sb = new StringBuilder();
        foreach (var kvp in variables.OrderBy(x => x.Key))
        {
            sb.Append($"{kvp.Key}:{kvp.Value?.GetType().Name ?? "null"},");
        }
        return sb.ToString();
    }
    
    /// <summary>
    /// Dynamically create a ScriptGlobals type with properties for each variable
    /// </summary>
    private Type CreateDynamicGlobalsType(Dictionary<string, object> variables)
    {
        // Create a dynamic assembly and module
        var assemblyName = new AssemblyName($"DynamicScriptGlobals_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        
        // Create the type
        var typeBuilder = moduleBuilder.DefineType(
            "DynamicScriptGlobals",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(ScriptGlobals));
        
        // Add properties for each variable
        foreach (var kvp in variables)
        {
            var propertyName = kvp.Key;
            var propertyType = kvp.Value?.GetType() ?? typeof(object);
            
            // Define the field
            var fieldBuilder = typeBuilder.DefineField(
                $"_{propertyName.ToLowerInvariant()}",
                propertyType,
                FieldAttributes.Private);
            
            // Define the property
            var propertyBuilder = typeBuilder.DefineProperty(
                propertyName,
                PropertyAttributes.HasDefault,
                propertyType,
                null);
            
            // Define the getter
            var getMethodBuilder = typeBuilder.DefineMethod(
                $"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes);
            
            var getIl = getMethodBuilder.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);
            
            propertyBuilder.SetGetMethod(getMethodBuilder);
            
            // Define the setter
            var setMethodBuilder = typeBuilder.DefineMethod(
                $"set_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new[] { propertyType });
            
            var setIl = setMethodBuilder.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);
            
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }
        
        // Create the type
        return typeBuilder.CreateType() ?? typeof(ScriptGlobals);
    }
    
    /// <summary>
    /// Check if a string is a valid C# identifier
    /// </summary>
    private bool IsValidCSharpIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        // First character must be letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;
        
        // Remaining characters must be letter, digit, or underscore
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;
        }
        
        // Check for C# keywords
        var keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };
        
        return !keywords.Contains(name);
    }
    
    /// <summary>
    /// Execute a C# expression using Roslyn scripting (legacy method for backward compatibility)
    /// Converts VariableClass list to dictionary and calls the new method
    /// Returns the result with UsedVariables populated from the provided variableList
    /// </summary>
    /// <param name="code">Expression code that may contain {varname} format for variable references</param>
    /// <param name="variableList">List of predefined variables with default values</param>
    public async Task<ExpressionResult> ExecuteExpressionAsync(
        string code,
        List<VariableClass> variableList)
    {
        // Convert VariableClass list to dictionary
        var variablesDict = new Dictionary<string, object>();
        var variableMap = new Dictionary<string, VariableClass>();
        foreach (var variable in variableList)
        {
            variablesDict[variable.VarName] = variable.DefaultValue;
            variableMap[variable.VarName] = variable;
        }
        
        // Call the dictionary-based method (it will handle {varname} replacement based on access mode)
        var result = await ExecuteExpressionAsync(code, variablesDict);
        
        // Update UsedVariables to use the original VariableClass objects from variableList
        if (result.UsedVariables.Count > 0)
        {
            var usedVariables = new List<VariableClass>();
            foreach (var usedVar in result.UsedVariables)
            {
                if (variableMap.TryGetValue(usedVar.VarName, out var originalVar))
                {
                    usedVariables.Add(originalVar);
                }
                else
                {
                    usedVariables.Add(usedVar);
                }
            }
            result.UsedVariables = usedVariables;
        }
        
        return result;
    }
}

/// <summary>
/// Globals class for Roslyn scripting
/// Variables are stored in a dictionary and accessed via type casting
/// </summary>
public class ScriptGlobals
{
    public Dictionary<string, object> Variables { get; } = [];
}

