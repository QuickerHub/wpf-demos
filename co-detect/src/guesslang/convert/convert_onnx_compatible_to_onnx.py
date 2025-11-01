"""Convert the ONNX-compatible model to ONNX format"""

import subprocess
import sys
from pathlib import Path


def convert_to_onnx():
    """Convert the saved model to ONNX"""
    from pathlib import Path
    root_dir = Path(__file__).parent.parent
    model_dir = str(root_dir / 'onnx_compatible_model')
    output_path = str(root_dir / 'model_final.onnx')
    
    if not Path(model_dir).exists():
        print(f"Model directory not found: {model_dir}")
        print("Please run build_onnx_model.py first")
        return False
    
    print(f"Converting {model_dir} to ONNX...")
    
    cmd = [
        sys.executable,
        '-m', 'tf2onnx.convert',
        '--saved-model', model_dir,
        '--output', output_path,
        '--opset', '15'  # Use opset 15 for better operator support
    ]
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    if result.returncode == 0:
        print(f"Success! ONNX model saved to: {output_path}")
        size_mb = Path(output_path).stat().st_size / 1024 / 1024
        print(f"Model size: {size_mb:.2f} MB")
        return True
    else:
        print(f"Conversion failed:")
        print(f"STDOUT: {result.stdout}")
        print(f"STDERR: {result.stderr}")
        return False


if __name__ == '__main__':
    convert_to_onnx()

