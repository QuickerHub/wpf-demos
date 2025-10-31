using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CoDetectNet;

namespace CoDetectNet.Server
{
    /// <summary>
    /// Static API wrapper for language detection
    /// </summary>
    public static class CoDetect
    {
        private static Lazy<CoDetectModel> _modelLazy = new Lazy<CoDetectModel>(CreateModel);
        private static readonly object _lock = new object();

        /// <summary>
        /// Get the directory where the current assembly (DLL) is located
        /// </summary>
        private static string GetAssemblyDirectory()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation) ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Create the model instance
        /// </summary>
        private static CoDetectModel CreateModel()
        {
            var modelPath = Path.Combine(GetAssemblyDirectory(), "codetect.onnx");
            var langPath = Path.Combine(GetAssemblyDirectory(), "languages.json");
            return new CoDetectModel(modelPath, langPath);
        }

        /// <summary>
        /// Get or create the model instance
        /// </summary>
        private static CoDetectModel GetModel()
        {
            return _modelLazy.Value;
        }

        /// <summary>
        /// Predict programming language from source code
        /// </summary>
        /// <param name="code">Source code to analyze</param>
        /// <param name="topN">Number of top results to return (default: 5)</param>
        /// <returns>List of language predictions with probabilities</returns>
        public static List<(string Language, float Probability)> Predict(string code, int topN = 5)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new List<(string, float)>();
            }

            var model = GetModel();
            var allResults = model.Predict(code);
            
            // Filter out Groovy and renormalize probabilities
            var filteredResults = allResults
                .Where(r => !string.Equals(r.Language, "Groovy", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (filteredResults.Count == 0)
            {
                return new List<(string, float)>();
            }
            
            // Renormalize probabilities so they sum to 1
            var totalProbability = filteredResults.Sum(r => r.Probability);
            if (totalProbability > 0)
            {
                filteredResults = filteredResults
                    .Select(r => (r.Language, r.Probability / totalProbability))
                    .ToList();
            }
            
            var topResults = filteredResults.Take(topN).ToList();
            
            // If top results contain common languages, prioritize the highest scoring common language
            var commonLanguagesInResults = topResults
                .Where(r => LanguageMapping.CommonLanguages.Contains(r.Language))
                .ToList();
            
            if (commonLanguagesInResults.Count > 0)
            {
                // Get the highest scoring common language
                var topCommonLanguage = commonLanguagesInResults
                    .OrderByDescending(r => r.Probability)
                    .First();
                
                // Reorder: put the top common language first, then others
                var reorderedResults = new List<(string Language, float Probability)> { topCommonLanguage };
                reorderedResults.AddRange(topResults.Where(r => r.Language != topCommonLanguage.Language));
                
                return reorderedResults.Take(topN).ToList();
            }
            
            return topResults;
        }

        /// <summary>
        /// Predict the most likely programming language
        /// </summary>
        /// <param name="code">Source code to analyze</param>
        /// <returns>Most likely language name, or null if empty</returns>
        public static string? PredictLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            // Use Predict method which already filters out Groovy
            var results = Predict(code, topN: 1);
            return results.Count > 0 ? results[0].Language : null;
        }

        /// <summary>
        /// Predict programming language with Monaco Editor language ID mapping
        /// </summary>
        /// <param name="code">Source code to analyze</param>
        /// <param name="topN">Number of top results to return (default: 5)</param>
        /// <returns>List of predictions with Monaco Editor language IDs and probabilities</returns>
        public static List<(string MonacoLanguageId, float Probability)> PredictWithMapping(string code, int topN = 5)
        {
            var results = Predict(code, topN);
            return results
                .Select(r => (LanguageMapping.GetMonacoLanguageId(r.Language), r.Probability))
                .ToList();
        }

        /// <summary>
        /// Predict the most likely programming language with Monaco Editor language ID mapping
        /// </summary>
        /// <param name="code">Source code to analyze</param>
        /// <returns>Monaco Editor language ID, or "plaintext" if empty or not found</returns>
        public static string PredictLanguageWithMapping(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "plaintext";
            }

            var language = PredictLanguage(code);
            return language != null ? LanguageMapping.GetMonacoLanguageId(language) : "plaintext";
        }

        /// <summary>
        /// Dispose the model (optional cleanup)
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                if (_modelLazy?.IsValueCreated == true)
                {
                    _modelLazy.Value.Dispose();
                }
                _modelLazy = null;
            }
        }
    }
}

