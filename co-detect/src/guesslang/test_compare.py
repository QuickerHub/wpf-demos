"""Compare ONNX and TensorFlow model predictions"""

import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

import json
from pathlib import Path
from operator import itemgetter
from typing import List, Tuple

import tensorflow as tf
import numpy as np
from guesslang.model import load, predict
from model_onnx import LanguagePredictorONNXFinal


def format_results(results: List[Tuple[str, float]], max_items: int = 5) -> str:
    """Format prediction results as string"""
    if not results:
        return "No predictions"
    lines = []
    for i, (lang, prob) in enumerate(results[:max_items], 1):
        lines.append(f"  {i}. {lang}: {prob:.2%}")
    return "\n".join(lines)


def test_comparison(tf_model, tf_mapping: dict, onnx_predictor, code: str, description: str):
    """Compare predictions from both models"""
    print(f"\n{description}:")
    print("-" * 60)
    print(f"Code snippet:\n{code[:100]}{'...' if len(code) > 100 else ''}")
    
    # TensorFlow prediction
    tf_results = predict(tf_model, tf_mapping, code)
    
    # ONNX prediction
    onnx_results = onnx_predictor.predict(code)
    
    # Compare
    print("\n" + "=" * 60)
    print("TensorFlow Model:")
    print("-" * 60)
    if tf_results:
        top_lang, top_prob = tf_results[0]
        print(f"Top prediction: {top_lang} ({top_prob:.2%})")
        print("\nTop 5:")
        print(format_results(tf_results))
    
    print("\n" + "=" * 60)
    print("ONNX Model:")
    print("-" * 60)
    if onnx_results:
        top_lang, top_prob = onnx_results[0]
        print(f"Top prediction: {top_lang} ({top_prob:.2%})")
        print("\nTop 5:")
        print(format_results(onnx_results))
    
    # Check if top predictions match
    if tf_results and onnx_results:
        tf_top = tf_results[0][0]
        onnx_top = onnx_results[0][0]
        match = "[MATCH]" if tf_top == onnx_top else "[DIFFER]"
        print(f"\nTop prediction: {match}")
        
        # Check top 5 overlap
        tf_top5 = [lang for lang, _ in tf_results[:5]]
        onnx_top5 = [lang for lang, _ in onnx_results[:5]]
        overlap = len(set(tf_top5) & set(onnx_top5))
        print(f"Top 5 overlap: {overlap}/5 languages")
        
        # Calculate probability differences
        tf_dict = dict(tf_results)
        onnx_dict = dict(onnx_results)
        all_langs = set(tf_dict.keys()) | set(onnx_dict.keys())
        diffs = []
        for lang in all_langs:
            tf_prob = tf_dict.get(lang, 0.0)
            onnx_prob = onnx_dict.get(lang, 0.0)
            diff = abs(tf_prob - onnx_prob)
            diffs.append(diff)
        max_diff = max(diffs) if diffs else 0.0
        mean_diff = np.mean(diffs) if diffs else 0.0
        print(f"Max probability difference: {max_diff:.6f}")
        print(f"Mean probability difference: {mean_diff:.6f}")


def main():
    """Run comparison tests"""
    print("=" * 60)
    print("Comparing TensorFlow vs ONNX Model Predictions")
    print("=" * 60)
    
    try:
        # Load TensorFlow model
        data_dir = Path(__file__).absolute().parent.joinpath('guesslang', 'data')
        model_dir = str(data_dir.joinpath('model'))
        languages_file = str(data_dir.joinpath('languages.json'))
        
        print(f"\nLoading TensorFlow model from: {model_dir}")
        tf_model = load(model_dir)
        print("TensorFlow model loaded")
        
        # Load language mapping
        language_map = json.loads(Path(languages_file).read_text(encoding='utf-8'))
        tf_mapping = {ext: name for name, ext in language_map.items()}
        
        # Load ONNX model
        print("\nLoading ONNX model...")
        onnx_predictor = LanguagePredictorONNXFinal()
        print("ONNX model loaded")
        
        print("\n" + "=" * 60)
        print("Starting comparison tests...")
        print("=" * 60)
        
        # Test cases (same as individual test scripts)
        python_code = """
def hello_world():
    print("Hello, World!")
    return True

class MyClass:
    def __init__(self):
        self.value = 42
"""
        test_comparison(tf_model, tf_mapping, onnx_predictor, python_code, "Test 1 - Python code")
        
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
        test_comparison(tf_model, tf_mapping, onnx_predictor, js_code, "Test 2 - JavaScript code")
        
        cpp_code = """
#include <iostream>
using namespace std;

int main() {
    cout << "Hello, World!" << endl;
    return 0;
}
"""
        test_comparison(tf_model, tf_mapping, onnx_predictor, cpp_code, "Test 3 - C++ code")
        
        java_code = """
public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello, World!");
    }
}
"""
        test_comparison(tf_model, tf_mapping, onnx_predictor, java_code, "Test 4 - Java code")
        
        short_code = "def hello(): pass"
        test_comparison(tf_model, tf_mapping, onnx_predictor, short_code, "Test 5 - Short Python code")
        
        print("\n" + "=" * 60)
        print("Comparison completed!")
        print("=" * 60)
        
    except FileNotFoundError as e:
        print(f"Error: {e}")
        print("\nPlease ensure:")
        print("  1. TensorFlow model exists: guesslang/data/model/")
        print("  2. ONNX model exists: model_final.onnx")
        print("  3. Languages file exists: guesslang/data/languages.json")
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()


if __name__ == '__main__':
    main()

