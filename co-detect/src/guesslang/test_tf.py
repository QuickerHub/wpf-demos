"""Test script for TensorFlow model inference"""

import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

import json
from pathlib import Path
from operator import itemgetter
from typing import List, Tuple

import tensorflow as tf
from guesslang.model import load, predict


def test_prediction(model, mapping: dict, code: str, description: str):
    """Test prediction and display results"""
    print(f"\n{description}:")
    print("-" * 60)
    print(f"Code snippet:\n{code[:100]}{'...' if len(code) > 100 else ''}")
    
    results = predict(model, mapping, code)
    
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
    print("Testing TensorFlow Model for Language Detection")
    print("=" * 60)
    
    try:
        # Load model
        data_dir = Path(__file__).absolute().parent.joinpath('guesslang', 'data')
        model_dir = str(data_dir.joinpath('model'))
        languages_file = str(data_dir.joinpath('languages.json'))
        
        print(f"Loading TensorFlow model from: {model_dir}")
        model = load(model_dir)
        print("TensorFlow model loaded successfully")
        
        # Load language mapping
        language_map = json.loads(Path(languages_file).read_text(encoding='utf-8'))
        # Create extension to language name mapping (reverse of language_map)
        mapping = {ext: name for name, ext in language_map.items()}
        
        # Test 1: Python code
        python_code = """
def hello_world():
    print("Hello, World!")
    return True

class MyClass:
    def __init__(self):
        self.value = 42
"""
        test_prediction(model, mapping, python_code, "Test 1 - Python code")
        
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
        test_prediction(model, mapping, js_code, "Test 2 - JavaScript code")
        
        # Test 3: C++ code
        cpp_code = """
#include <iostream>
using namespace std;

int main() {
    cout << "Hello, World!" << endl;
    return 0;
}
"""
        test_prediction(model, mapping, cpp_code, "Test 3 - C++ code")
        
        # Test 4: Java code
        java_code = """
public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello, World!");
    }
}
"""
        test_prediction(model, mapping, java_code, "Test 4 - Java code")
        
        # Test 5: Short code
        short_code = "def hello(): pass"
        test_prediction(model, mapping, short_code, "Test 5 - Short Python code")
        
        # Test 6: Empty string
        print("\n" + "=" * 60)
        print("Test 6 - Empty string:")
        print("-" * 60)
        if short_code.strip():
            # Empty string handling
            content_tensor = tf.constant([""])
            predicted = model.signatures['serving_default'](content_tensor)
            scores = predicted['scores'][0].numpy()
            top_idx = scores.argmax()
            top_lang = list(language_map.keys())[top_idx]
            print(f"Result: {top_lang} ({scores[top_idx]:.2%})")
        else:
            print("Result: None")
        
        print("\n" + "=" * 60)
        print("All tests completed!")
        print("=" * 60)
        
    except FileNotFoundError as e:
        print(f"Error: {e}")
        print("\nPlease ensure the model directory and languages.json exist:")
        print("  guesslang/data/model/")
        print("  guesslang/data/languages.json")
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()


if __name__ == '__main__':
    main()

