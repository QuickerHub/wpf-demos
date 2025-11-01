"""Build ONNX-compatible model from extracted weights"""

import os
from pathlib import Path

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

import tensorflow as tf
import numpy as np

# Support both module import and direct execution
try:
    from .preprocessing import HyperParameter
    from .extract_model_layers import extract_model_weights
except ImportError:
    # Direct execution - add parent directory to path
    import sys
    from pathlib import Path
    parent_dir = Path(__file__).parent.parent
    sys.path.insert(0, str(parent_dir))
    from convert.preprocessing import HyperParameter
    from convert.extract_model_layers import extract_model_weights


class InferenceModel(tf.Module):
    """ONNX-compatible model that accepts preprocessed hash indices"""
    
    def __init__(self, weights_dict: dict):
        super().__init__()
        
        # Extract embedding weights
        embedding_key = 'dnn/input_from_feature_columns/input_layer/content_embedding/embedding_weights:0'
        self.embedding = tf.Variable(weights_dict[embedding_key], trainable=False, name='embedding')
        
        # DNN layers
        self.dnn_kernel_0 = tf.Variable(weights_dict['dnn/hiddenlayer_0/kernel:0'], trainable=False)
        self.dnn_bias_0 = tf.Variable(weights_dict['dnn/hiddenlayer_0/bias:0'], trainable=False)
        self.dnn_kernel_1 = tf.Variable(weights_dict['dnn/hiddenlayer_1/kernel:0'], trainable=False)
        self.dnn_bias_1 = tf.Variable(weights_dict['dnn/hiddenlayer_1/bias:0'], trainable=False)
        
        # DNN logits
        self.dnn_logits_kernel = tf.Variable(weights_dict['dnn/logits/kernel:0'], trainable=False)
        self.dnn_logits_bias = tf.Variable(weights_dict['dnn/logits/bias:0'], trainable=False)
        
        # Linear layer
        self.linear_weights = tf.Variable(weights_dict['linear/linear_model/content/weights:0'], trainable=False)
        self.linear_bias = tf.Variable(weights_dict['linear/linear_model/bias_weights:0'], trainable=False)
    
    @tf.function(input_signature=[
        tf.TensorSpec(shape=[None, HyperParameter.NB_TOKENS], dtype=tf.int32)
    ])
    def __call__(self, hash_indices):
        """
        Forward pass using vectorized operations:
        1. Aggregate hash indices: count occurrences per bucket -> sparse representation
        2. Embedding lookup and aggregation (weighted average)
        3. DNN layers
        4. Linear layer (sparse one-hot)
        5. Combine and softmax
        """
        batch_size = tf.shape(hash_indices)[0]
        
        # Vectorized aggregation using tf.map_fn
        def process_sample(sample_hashes):
            # Aggregate: count occurrences of each hash bucket
            unique_hashes, idx, counts = tf.unique_with_counts(sample_hashes)
            
            # Clip indices to valid range [0, VOCABULARY_SIZE-1]
            unique_hashes = tf.clip_by_value(
                unique_hashes, 0, HyperParameter.VOCABULARY_SIZE - 1
            )
            
            # Filter out padding (empty string hash = 2263)
            # Padding appears ~9984 times and causes logits explosion
            empty_string_hash = 2263
            mask = tf.not_equal(unique_hashes, empty_string_hash)
            unique_hashes_filtered = tf.boolean_mask(unique_hashes, mask)
            counts_filtered = tf.boolean_mask(counts, mask)
            
            # For DNN path: weighted average by counts (excluding padding)
            embeddings = tf.nn.embedding_lookup(self.embedding, unique_hashes_filtered)
            counts_float = tf.cast(counts_filtered, tf.float32)[:, tf.newaxis]
            weighted_embeddings = embeddings * counts_float
            aggregated_embedding = tf.reduce_sum(weighted_embeddings, axis=0)
            total_count = tf.reduce_sum(counts_float)
            aggregated_embedding = aggregated_embedding / (total_count + 1e-8)
            
            # For Linear path: create sparse representation with counts (excluding padding)
            vocab_size = HyperParameter.VOCABULARY_SIZE
            one_hot = tf.one_hot(unique_hashes_filtered, vocab_size, dtype=tf.float32)  # [num_unique, vocab_size]
            counts_float_expanded = tf.cast(counts_filtered, tf.float32)[:, tf.newaxis]  # [num_unique, 1]
            # Sum counts for each hash bucket
            linear_features = tf.reduce_sum(one_hot * counts_float_expanded, axis=0)  # [vocab_size]
            
            return aggregated_embedding, linear_features
        
        # Process all samples in batch
        results = tf.map_fn(
            process_sample,
            hash_indices,
            fn_output_signature=(
                tf.TensorSpec(shape=[HyperParameter.EMBEDDING_SIZE], dtype=tf.float32),
                tf.TensorSpec(shape=[HyperParameter.VOCABULARY_SIZE], dtype=tf.float32)
            )
        )
        
        dnn_input = results[0]  # [batch_size, EMBEDDING_SIZE]
        linear_input = results[1]  # [batch_size, VOCABULARY_SIZE]
        
        # DNN path
        dnn_hidden_0 = tf.nn.relu(
            tf.matmul(dnn_input, self.dnn_kernel_0) + self.dnn_bias_0
        )  # [batch_size, 512]
        
        dnn_hidden_1 = tf.nn.relu(
            tf.matmul(dnn_hidden_0, self.dnn_kernel_1) + self.dnn_bias_1
        )  # [batch_size, 32]
        
        dnn_logits = tf.matmul(dnn_hidden_1, self.dnn_logits_kernel) + self.dnn_logits_bias  # [batch_size, 54]
        
        # Linear path
        linear_logits = tf.matmul(linear_input, self.linear_weights) + self.linear_bias  # [batch_size, 54]
        
        # Combine DNN and linear logits
        combined_logits = dnn_logits + linear_logits  # [batch_size, 54]
        
        # Softmax
        scores = tf.nn.softmax(combined_logits, axis=-1)  # [batch_size, 54]
        
        return {
            'scores': scores,
            'logits': combined_logits
        }


def build_model():
    """Build ONNX-compatible model from original TensorFlow model"""
    root_dir = Path(__file__).parent.parent
    data_dir = root_dir.joinpath('guesslang', 'data')
    model_dir = str(data_dir.joinpath('model'))
    
    print("Extracting weights from original model...")
    weights_dict, _ = extract_model_weights(model_dir)
    
    print("\nBuilding ONNX-compatible inference model...")
    model = InferenceModel(weights_dict)
    
    # Save model
    print("\nSaving model...")
    saved_model_dir = str(root_dir / 'onnx_compatible_model')
    tf.saved_model.save(model, saved_model_dir)
    print(f"Model saved to: {saved_model_dir}")
    
    return model, saved_model_dir


if __name__ == '__main__':
    build_model()

