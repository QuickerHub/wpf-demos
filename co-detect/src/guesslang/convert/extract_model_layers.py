"""Extract model weights and create ONNX-compatible model structure"""

import os
from pathlib import Path

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

import tensorflow as tf
import numpy as np

# Support both module import and direct execution
try:
    from .preprocessing import HyperParameter
except ImportError:
    # Direct execution - add parent directory to path
    import sys
    from pathlib import Path
    parent_dir = Path(__file__).parent.parent
    sys.path.insert(0, str(parent_dir))
    from convert.preprocessing import HyperParameter


def extract_model_weights(saved_model_dir: str):
    """Extract weights from the saved model"""
    print(f"Loading model from: {saved_model_dir}")
    model = tf.saved_model.load(saved_model_dir)
    
    # Get all variables from the model
    print("\nModel structure:")
    print(f"  Variables: {list(model.variables)}")
    print(f"  Functions: {list(model.signatures.keys())}")
    
    # Try to access the graph
    serving_fn = model.signatures['serving_default']
    
    # Get the concrete function
    print("\nConcrete function:")
    print(f"  Inputs: {serving_fn.inputs}")
    print(f"  Outputs: {serving_fn.outputs}")
    
    # The model uses feature columns which internally handle:
    # 1. Hash bucket lookup -> embedding
    # 2. DNN layers
    # 3. Linear layer
    # 4. Final logits -> probabilities
    
    # Extract variables if possible
    variables_dict = {}
    for var in model.variables:
        variables_dict[var.name] = var.numpy()
        print(f"  {var.name}: shape={var.shape}, dtype={var.dtype}")
    
    return variables_dict, model

