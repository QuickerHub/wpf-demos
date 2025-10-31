using System;
using System.Collections.Generic;
using System.Linq;

namespace CoDetectNet
{
    /// <summary>
    /// Demo class for language detection
    /// </summary>
    public static class Demo
    {
        /// <summary>
        /// Run the language detection demo
        /// </summary>
        public static void Run()
        {
            Console.WriteLine(new string('=', 70));
            Console.WriteLine("ONNX Language Detection Demo (.NET Framework 4.7.2)");
            Console.WriteLine(new string('=', 70));

            CoDetectModel model;
            try
            {
                model = new CoDetectModel();
                Console.WriteLine("Model loaded successfully!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading model: {ex.Message}");
                Console.WriteLine("\nPlease ensure:");
                Console.WriteLine("  1. codetect.onnx exists in this directory");
                Console.WriteLine("  2. languages.json exists in this directory");
                return;
            }

            // Demo examples
            var examples = new[]
            {
                new { Name = "Python Code", Code = @"def fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n-1) + fibonacci(n-2)

class Calculator:
    def add(self, a, b):
        return a + b" },
                new { Name = "JavaScript Code", Code = @"function factorial(n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}

const obj = {
    name: ""Hello"",
    value: 42
};" },
                new { Name = "C++ Code", Code = @"#include <iostream>
#include <vector>

int main() {
    std::vector<int> vec = {1, 2, 3, 4, 5};
    for (auto& x : vec) {
        std::cout << x << std::endl;
    }
    return 0;
}" },
                new { Name = "Java Code", Code = @"public class HelloWorld {
    public static void main(String[] args) {
        System.out.println(""Hello, World!"");
        int sum = 0;
        for (int i = 0; i < 10; i++) {
            sum += i;
        }
    }
}" },
                new { Name = "HTML Code", Code = @"<!DOCTYPE html>
<html>
<head>
    <title>Demo</title>
</head>
<body>
    <h1>Hello World</h1>
    <p>This is a demo page.</p>
</body>
</html>" }
            };

            using (model)
            {
                // Run predictions
                for (int i = 0; i < examples.Length; i++)
                {
                    var example = examples[i];
                    Console.WriteLine(new string('-', 70));
                    Console.WriteLine($"Example {i + 1}: {example.Name}");
                    Console.WriteLine(new string('-', 70));
                    var preview = example.Code.Length > 150 ? example.Code.Substring(0, 150) + "..." : example.Code;
                    Console.WriteLine($"Code:\n{preview}");
                    Console.WriteLine();

                    var results = model.Predict(example.Code);

                    if (results.Count > 0)
                    {
                        var (topLanguage, topProb) = results[0];
                        Console.WriteLine($"Detected Language: {topLanguage} ({topProb:P2} confidence)");
                        Console.WriteLine("\nTop 5 Predictions:");
                        foreach (var (lang, prob) in results.Take(5))
                        {
                            var bar = new string('=', (int)(prob * 50));
                            Console.WriteLine($"  {lang,-20} {prob,6:P2} {bar}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No prediction available");
                    }

                    Console.WriteLine();
                }

                Console.WriteLine(new string('=', 70));
                Console.WriteLine("Demo completed!");
                Console.WriteLine(new string('=', 70));
            }
        }
    }
}

