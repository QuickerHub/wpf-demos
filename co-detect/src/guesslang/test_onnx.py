"""Test script for ONNX model inference - using JSON test cases"""

import json
from pathlib import Path
from model_onnx import LanguagePredictorONNXFinal


def main():
    """Run tests with examples from JSON file"""
    # Load ONNX predictor
    predictor = LanguagePredictorONNXFinal()
    
    # Load test cases from JSON
    test_cases = json.loads(Path('test_cases.json').read_text(encoding='utf-8'))
    
    # Run tests
    for case in test_cases:
        results = predictor.predict(case['code'])
        top_lang, top_prob = results[0] if results else (None, 0)
        print(f"{case['language']:12} -> {top_lang:12} ({top_prob:.2%})")


if __name__ == '__main__':
    main()
