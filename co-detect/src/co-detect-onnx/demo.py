"""Demo script for ONNX language detection"""

from pathlib import Path
from model_onnx import LanguagePredictorONNXFinal


def demo():
    """Demonstrate language detection capabilities"""
    
    print("=" * 70)
    print("ONNX Language Detection Demo")
    print("=" * 70)
    
    try:
        predictor = LanguagePredictorONNXFinal()
        print("Model loaded successfully!\n")
    except Exception as e:
        print(f"Error loading model: {e}")
        print("\nPlease ensure:")
        print("  1. model_final.onnx exists in this directory")
        print("  2. languages.json exists in this directory")
        return
    
    # Demo examples
    examples = [
        {
            "name": "Python Code",
            "code": """def fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n-1) + fibonacci(n-2)

class Calculator:
    def add(self, a, b):
        return a + b"""
        },
        {
            "name": "JavaScript Code",
            "code": """function factorial(n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}

const obj = {
    name: "Hello",
    value: 42
};"""
        },
        {
            "name": "C++ Code",
            "code": """#include <iostream>
#include <vector>

int main() {
    std::vector<int> vec = {1, 2, 3, 4, 5};
    for (auto& x : vec) {
        std::cout << x << std::endl;
    }
    return 0;
}"""
        },
        {
            "name": "Java Code",
            "code": """public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello, World!");
        int sum = 0;
        for (int i = 0; i < 10; i++) {
            sum += i;
        }
    }
}"""
        },
        {
            "name": "HTML Code",
            "code": """<!DOCTYPE html>
<html>
<head>
    <title>Demo</title>
</head>
<body>
    <h1>Hello World</h1>
    <p>This is a demo page.</p>
</body>
</html>"""
        },
        {
            "name": "SQL Query",
            "code": """SELECT users.name, orders.total
FROM users
INNER JOIN orders ON users.id = orders.user_id
WHERE orders.date > '2024-01-01'
ORDER BY orders.total DESC;"""
        },
        {
            "name": "Short Python",
            "code": "print('Hello, World!')"
        }
    ]
    
    # Run predictions
    for i, example in enumerate(examples, 1):
        print("-" * 70)
        print(f"Example {i}: {example['name']}")
        print("-" * 70)
        print(f"Code:\n{example['code'][:150]}{'...' if len(example['code']) > 150 else ''}")
        print()
        
        results = predictor.predict(example['code'])
        
        if results:
            top_language, top_prob = results[0]
            print(f"Detected Language: {top_language} ({top_prob:.2%} confidence)")
            print(f"\nTop 5 Predictions:")
            for j, (lang, prob) in enumerate(results[:5], 1):
                bar = "=" * int(prob * 50)
                print(f"  {j}. {lang:20s} {prob:6.2%} {bar}")
        else:
            print("No prediction available")
        
        print()
    
    print("=" * 70)
    print("Demo completed!")
    print("=" * 70)


if __name__ == '__main__':
    demo()

