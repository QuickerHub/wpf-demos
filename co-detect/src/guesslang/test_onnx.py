"""Test script for ONNX model inference"""

from model_onnx import LanguagePredictorONNXFinal


def test_prediction(predictor: LanguagePredictorONNXFinal, code: str, description: str):
    """Test prediction and display results"""
    print(f"\n{description}:")
    print("-" * 60)
    print(f"Code snippet:\n{code[:100]}{'...' if len(code) > 100 else ''}")
    
    results = predictor.predict(code)
    
    if results:
        top_language, top_prob = results[0]
        print(f"\nDetected language: {top_language} ({top_prob:.2%})")
        print("\nTop 5 predictions:")
        for i, (lang, prob) in enumerate(results[:5], 1):
            print(f"  {i}. {lang}: {prob:.2%}")
    else:
        print("No prediction available")


def main():
    """Run tests with various code samples"""
    print("=" * 60)
    print("Testing ONNX Model for Language Detection")
    print("=" * 60)
    
    try:
        predictor = LanguagePredictorONNXFinal()
        print("ONNX model loaded successfully")
        
        # Test 1: Python code
        python_code = """
def hello_world():
    print("Hello, World!")
    return True

class MyClass:
    def __init__(self):
        self.value = 42
"""
        test_prediction(predictor, python_code, "Test 1 - Python code")
        
        # Test 2: JavaScript code
        js_code = """
function greet(name) {
    console.log("Hello, " + name);
    return true;
}

const obj = {
    key: "value",
    number: 42
};
"""
        test_prediction(predictor, js_code, "Test 2 - JavaScript code")
        
        # Test 3: C++ code
        cpp_code = """
#include <iostream>
using namespace std;

int main() {
    cout << "Hello, World!" << endl;
    return 0;
}
"""
        test_prediction(predictor, cpp_code, "Test 3 - C++ code")
        
        # Test 4: Java code
        java_code = """
public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello, World!");
    }
}
"""
        test_prediction(predictor, java_code, "Test 4 - Java code")
        
        # Test 5: Short code
        short_code = "def hello(): pass"
        test_prediction(predictor, short_code, "Test 5 - Short Python code")
        
        # Test 6: Empty string
        print("\n" + "=" * 60)
        print("Test 6 - Empty string:")
        print("-" * 60)
        result = predictor.predict_language("")
        print(f"Result: {result}")
        
        print("\n" + "=" * 60)
        print("All tests completed!")
        print("=" * 60)
        
    except FileNotFoundError as e:
        print(f"Error: {e}")
        print("\nPlease ensure:")
        print("  1. Run: python build_onnx_model.py")
        print("  2. Run: python convert_onnx_compatible_to_onnx.py")
        print("  3. Then run this test script")
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()


if __name__ == '__main__':
    main()

