"""Preprocessing for ONNX model conversion - No TensorFlow dependency"""

from typing import List

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

