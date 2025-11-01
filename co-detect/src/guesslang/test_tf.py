"""Test script for TensorFlow model inference - using JSON test cases"""

import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

import json
from pathlib import Path
from typing import List, Tuple

import tensorflow as tf
from guesslang.model import load, predict


def main():
    """Run tests with examples from JSON file"""
    # Load model and mapping
    data_dir = Path(__file__).parent / 'guesslang' / 'data'
    model = load(str(data_dir / 'model'))
    language_map = json.loads((data_dir / 'languages.json').read_text(encoding='utf-8'))
    mapping = {ext: name for name, ext in language_map.items()}
    
    # Load test cases from JSON
    test_cases = json.loads(Path('test_cases.json').read_text(encoding='utf-8'))
    
    # Run tests
    for case in test_cases:
        results = predict(model, mapping, case['code'])
        top_lang, top_prob = results[0] if results else (None, 0)
        print(f"{case['language']:12} -> {top_lang:12} ({top_prob:.2%})")


if __name__ == '__main__':
    main()
