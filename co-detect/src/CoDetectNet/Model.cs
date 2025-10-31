using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;

namespace CoDetectNet
{
    /// <summary>
    /// Model hyper parameters
    /// </summary>
    internal static class HyperParameter
    {
        public const int NB_TOKENS = 10000;
        public const int VOCABULARY_SIZE = 5000;
        public static int EMBEDDING_SIZE => Math.Max(10, (int)Math.Sqrt(VOCABULARY_SIZE));
        public const int N_GRAM = 2;
    }

    /// <summary>
    /// Language detection predictor using ONNX runtime
    /// </summary>
    public class CoDetectModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string[] _outputNames;
        private readonly string[] _languageNames;
        private readonly FarmHash _farmHash;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="onnxModelPath">Path to ONNX model file (default: 'codetect.onnx' in current directory)</param>
        /// <param name="languagesFile">Path to languages.json file (default: 'languages.json' in current directory)</param>
        public CoDetectModel(string onnxModelPath = "codetect.onnx", string? languagesFile = null)
        {
            // Initialize FarmHash instance
            _farmHash = new FarmHash();
            
            // Load ONNX model
            var sessionOptions = new SessionOptions();
            _session = new InferenceSession(onnxModelPath, sessionOptions);
            
            // Get input/output names
            _inputName = _session.InputMetadata.Keys.First();
            _outputNames = _session.OutputMetadata.Keys.ToArray();

            // Load language names
            if (languagesFile == null)
            {
                var localLanguages = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "languages.json");
                if (File.Exists(localLanguages))
                {
                    languagesFile = localLanguages;
                }
                else
                {
                    throw new FileNotFoundException(
                        $"languages.json not found. Please ensure languages.json exists in {AppDomain.CurrentDomain.BaseDirectory} " +
                        "or specify languagesFile parameter."
                    );
                }
            }

            var jsonContent = File.ReadAllText(languagesFile);
            var languagesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            if (languagesDict == null)
            {
                throw new InvalidOperationException("Failed to parse languages.json");
            }
            _languageNames = languagesDict.Keys.ToArray();
        }

        /// <summary>
        /// Preprocess text for model input
        /// </summary>
        private int[] PreprocessText(string text)
        {
            // Split into bytes
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var bytesList = bytes.Select(b => new byte[] { b }).ToList();

            // Generate n-grams (bigrams)
            // In Python: (b' ').join(bytes_list[i:i+N_GRAM])
            // This creates n-grams with space separators between bytes
            var ngramsList = new List<byte[]>();
            if (bytesList.Count >= HyperParameter.N_GRAM)
            {
                for (int i = 0; i <= bytesList.Count - HyperParameter.N_GRAM; i++)
                {
                    var ngramBytes = bytesList.Skip(i).Take(HyperParameter.N_GRAM).ToList();
                    // Join with space separator (like Python's b' '.join())
                    var ngram = new List<byte>();
                    for (int j = 0; j < ngramBytes.Count; j++)
                    {
                        if (j > 0)
                        {
                            ngram.Add(32); // space separator
                        }
                        ngram.AddRange(ngramBytes[j]);
                    }
                    ngramsList.Add(ngram.ToArray());
                }
            }

            // Pad/truncate to NB_TOKENS
            while (ngramsList.Count < HyperParameter.NB_TOKENS)
            {
                ngramsList.Add(Array.Empty<byte>());
            }
            if (ngramsList.Count > HyperParameter.NB_TOKENS)
            {
                ngramsList = ngramsList.Take(HyperParameter.NB_TOKENS).ToList();
            }

            // Hash each n-gram to bucket
            var hashIndices = new int[HyperParameter.NB_TOKENS];
            for (int i = 0; i < ngramsList.Count; i++)
            {
                var hash = _farmHash.Fingerprint64(ngramsList[i]);
                hashIndices[i] = (int)(hash % (ulong)HyperParameter.VOCABULARY_SIZE);
            }

            return hashIndices;
        }

        /// <summary>
        /// Predict programming language probabilities
        /// </summary>
        public List<(string Language, float Probability)> Predict(string sourceCode)
        {
            // Preprocess
            var hashIndices = PreprocessText(sourceCode);

            // Create input tensor
            var inputTensor = new DenseTensor<int>(hashIndices, new[] { 1, hashIndices.Length });
            var inputContainer = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
            };

            // Run inference
            using var results = _session.Run(inputContainer);
            
            // Get scores (prefer 'scores', fallback to softmax(logits))
            float[]? scores = null;
            float[]? logits = null;

            foreach (var result in results)
            {
                if (result.Name == "scores")
                {
                    var tensor = result.AsTensor<float>();
                    scores = tensor.ToArray();
                }
                else if (result.Name == "logits")
                {
                    var tensor = result.AsTensor<float>();
                    logits = tensor.ToArray();
                }
            }

            // Apply softmax if needed
            // If we have scores, check if they're already normalized (sum close to 1.0)
            // If not, or if we only have logits, apply softmax
            if (scores == null || Math.Abs(scores.Sum() - 1.0f) > 0.1f)
            {
                var source = logits ?? results.First().AsTensor<float>().ToArray();
                if (source.Length > 0)
                {
                    var maxScore = source.Max();
                    var expScores = source.Select(s => (float)Math.Exp(s - maxScore)).ToArray();
                    var sumExp = expScores.Sum();
                    scores = expScores.Select(s => s / sumExp).ToArray();
                }
            }

            // Return sorted results
            var resultsList = _languageNames.Zip(scores, (lang, prob) => (lang, prob))
                .OrderByDescending(x => x.prob)
                .ToList();

            return resultsList;
        }

        /// <summary>
        /// Predict the most likely language name
        /// </summary>
        public string? PredictLanguage(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return null;
            }

            var results = Predict(sourceCode);
            return results.FirstOrDefault().Language;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}

