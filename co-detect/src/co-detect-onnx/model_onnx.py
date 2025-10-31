"""ONNX-based language predictor"""

import json
from pathlib import Path
from operator import itemgetter
from typing import List, Tuple, Optional

import numpy as np
import onnxruntime as ort
from preprocessing import preprocess_text


class LanguagePredictorONNXFinal:
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

