"""ONNX-based language predictor with preprocessing"""

import json
from pathlib import Path
from operator import itemgetter
from typing import List, Tuple, Optional

import numpy as np
import onnxruntime as ort

try:
    import farmhash
except ImportError:
    raise ImportError("farmhash library is required. Install it with: pip install pyfarmhash")


class HyperParameter:
    """Model hyper parameters"""
    NB_TOKENS = 10000
    VOCABULARY_SIZE = 5000
    EMBEDDING_SIZE = max(10, int(VOCABULARY_SIZE**0.5))
    N_GRAM = 2


def preprocess_text(text: str) -> List[int]:
    """
    Preprocess text without TensorFlow dependency.
    
    Steps:
    1. Split text into bytes
    2. Generate n-grams (bigrams)
    3. Pad/truncate to NB_TOKENS
    4. Hash to buckets using FarmHash
    
    :param text: Input source code text
    :return: List of hash bucket indices (length NB_TOKENS)
    """
    # Split into bytes
    bytes_list = [bytes([b]) for b in text.encode('utf-8')]
    
    # Generate n-grams (TensorFlow adds space separator between tokens)
    ngrams_list = []
    if len(bytes_list) >= HyperParameter.N_GRAM:
        ngrams_list = [(b' ').join(bytes_list[i:i+HyperParameter.N_GRAM]) 
                       for i in range(len(bytes_list) - HyperParameter.N_GRAM + 1)]
    
    # Pad/truncate to NB_TOKENS
    padding_needed = HyperParameter.NB_TOKENS - len(ngrams_list)
    if padding_needed > 0:
        ngrams_list.extend([b''] * padding_needed)
    else:
        ngrams_list = ngrams_list[:HyperParameter.NB_TOKENS]
    
    # Hash each n-gram to bucket
    return [farmhash.fingerprint64(ngram) % HyperParameter.VOCABULARY_SIZE 
            for ngram in ngrams_list]


class CoDetectModel:
    """Language detection predictor using ONNX runtime"""

    def __init__(self, onnx_model_path: str = 'model_final.onnx', 
                 languages_file: Optional[str] = None) -> None:
        self._session = ort.InferenceSession(onnx_model_path, providers=['CPUExecutionProvider'])
        self._input_name = self._session.get_inputs()[0].name
        self._output_names = [out.name for out in self._session.get_outputs()]
        
        if languages_file is None:
            # Look for languages.json in current directory
            local_languages = Path(__file__).parent / 'languages.json'
            if local_languages.exists():
                languages_file = str(local_languages)
            else:
                raise FileNotFoundError(
                    f"languages.json not found. Please ensure languages.json exists in {Path(__file__).parent} "
                    f"or specify languages_file parameter."
                )
        self._language_names = list(json.loads(Path(languages_file).read_text(encoding='utf-8')).keys())

    def predict(self, source_code: str) -> List[Tuple[str, float]]:
        """Predict programming language probabilities"""
        # Preprocess and run inference
        hash_indices = preprocess_text(source_code)
        outputs = self._session.run(None, {self._input_name: np.array([hash_indices], dtype=np.int32)})
        
        # Get scores (prefer 'scores', fallback to softmax(logits))
        scores = None
        logits = None
        for i, name in enumerate(self._output_names):
            if name == 'scores':
                scores = outputs[i][0]
            elif name == 'logits':
                logits = outputs[i][0]
        
        if scores is None or np.sum(scores) < 0.5:
            scores = logits if logits is not None else outputs[0][0]
            exp_scores = np.exp(scores - np.max(scores))
            scores = exp_scores / np.sum(exp_scores)
        
        # Return sorted results
        results = list(zip(self._language_names, scores))
        results.sort(key=itemgetter(1), reverse=True)
        return results

    def predict_language(self, source_code: str) -> Optional[str]:
        """Predict the most likely language name"""
        if not source_code.strip():
            return None
        results = self.predict(source_code)
        return results[0][0] if results else None

