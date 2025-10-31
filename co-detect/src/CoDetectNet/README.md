# CoDetect ONNX .NET Framework 4.7.2

C# implementation of the language detection model using ONNX Runtime for .NET Framework 4.7.2.

## Requirements

- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later (or .NET SDK)

## Dependencies

- Microsoft.ML.OnnxRuntime (1.20.0)
- Newtonsoft.Json (13.0.3)

## Quick Start

1. Copy required resource files to `resources` directory:
   - `resources/codetect.onnx`
   - `resources/languages.json`

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Build the project (resource files will be automatically copied to output):
```bash
dotnet build
```

4. Run the demo:
```bash
dotnet run
```

Or use the run script:
```bash
.\run.ps1
```

## Usage Example

```csharp
using CoDetectNet;

using var model = new CoDetectModel();

// Predict language
var language = model.PredictLanguage("def hello(): pass");
Console.WriteLine(language); // Output: Python

// Get all predictions
var results = model.Predict("def hello(): pass");
foreach (var (lang, prob) in results.Take(5))
{
    Console.WriteLine($"{lang}: {prob:P2}");
}
```

## Notes

- The FarmHash implementation is a simplified version. For production use, consider using the Google.FarmHash NuGet package if available.
- Resource files (`codetect.onnx` and `languages.json`) should be placed in the `resources` directory. They will be automatically copied to the output directory during build via the .csproj Content items.

