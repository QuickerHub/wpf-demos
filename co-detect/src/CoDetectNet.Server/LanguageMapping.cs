using System;
using System.Collections.Generic;

namespace CoDetectNet.Server
{
    /// <summary>
    /// Maps CoDetectNet language names to Monaco Editor language IDs
    /// </summary>
    public static class LanguageMapping
    {
        /// <summary>
        /// Common languages list (ordered by commonness)
        /// </summary>
        public static readonly HashSet<string> CommonLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "JavaScript",
            "TypeScript",
            "Python",
            "Java",
            "C#",
            "C++",
            "C",
            "HTML",
            "CSS",
            "JSON",
            "XML",
            "SQL",
            "PHP",
            "Ruby",
            "Go",
            "Rust",
            "Swift",
            "Kotlin",
            "Dart",
            "Markdown",
            "YAML",
            "Shell",
            "PowerShell",
            "Lua",
            "R"
        };
        /// <summary>
        /// Dictionary mapping CoDetectNet language names to Monaco Editor language IDs
        /// </summary>
        public static readonly Dictionary<string, string> CoDetectToMonaco = new Dictionary<string, string>
        {
            // Direct mappings
            { "Python", "python" },
            { "C#", "csharp" },
            { "JSON", "json" },
            { "JavaScript", "javascript" },
            { "TypeScript", "typescript" },
            { "Java", "java" },
            { "HTML", "html" },
            { "CSS", "css" },
            { "XML", "xml" },
            { "SQL", "sql" },
            { "Markdown", "markdown" },
            { "Go", "go" },
            { "Rust", "rust" },
            { "PHP", "php" },
            { "Ruby", "ruby" },
            { "Kotlin", "kotlin" },
            { "Swift", "swift" },
            { "Objective-C", "objective-c" },
            { "Scala", "scala" },
            { "PowerShell", "powershell" },
            { "Lua", "lua" },
            { "R", "r" },
            { "Haskell", "haskell" },
            { "Perl", "perl" },
            { "Elixir", "elixir" },
            { "Clojure", "clojure" },
            { "Dart", "dart" },
            { "C", "c" },
            { "C++", "cpp" },
            { "Fortran", "fortran" },
            { "YAML", "yaml" },
            { "TOML", "toml" },
            { "Dockerfile", "dockerfile" },
            { "INI", "ini" },
            { "Verilog", "verilog" },
            { "CoffeeScript", "coffeescript" },
            
            // Special mappings (languages with similar syntax)
            { "Groovy", "csharp" }, // Groovy syntax is similar to C#/Java, map to csharp
            
            // Language family mappings
            { "Scheme", "scheme" }, // If detected, map to scheme
            { "Lisp", "scheme" }, // Lisp can use Scheme highlighting
            
            // LaTeX/TeX mapping
            { "TeX", "latex" },
            
            // Languages not in Monaco list - map to closest alternatives
            { "F#", "fsharp" },
            { "Assembly", "plaintext" }, // No assembly syntax in Monaco
            { "Batchfile", "shell" }, // Batch files can use shell highlighting
            { "CMake", "plaintext" }, // CMake not in Monaco list
            { "COBOL", "plaintext" }, // COBOL not in Monaco list
            { "CSV", "plaintext" }, // CSV is plain data
            { "DM", "plaintext" }, // DM (DreamMaker) not in Monaco list
            { "Erlang", "elixir" }, // Erlang similar to Elixir
            { "Julia", "python" }, // Julia syntax similar to Python
            { "Matlab", "python" }, // MATLAB syntax similar to Python
            { "OCaml", "fsharp" }, // OCaml similar to F#
            { "Pascal", "plaintext" }, // Pascal not in Monaco list
            { "Prolog", "plaintext" }, // Prolog not in Monaco list
            { "Visual Basic", "plaintext" }, // VB not in Monaco list
        };

        /// <summary>
        /// Get Monaco Editor language ID from CoDetectNet language name
        /// </summary>
        /// <param name="coDetectLanguage">Language name from CoDetectNet</param>
        /// <returns>Monaco Editor language ID, or "plaintext" if not found</returns>
        public static string GetMonacoLanguageId(string coDetectLanguage)
        {
            if (string.IsNullOrWhiteSpace(coDetectLanguage))
            {
                return "plaintext";
            }

            return CoDetectToMonaco.TryGetValue(coDetectLanguage, out var monacoId) 
                ? monacoId 
                : "plaintext";
        }
    }
}

