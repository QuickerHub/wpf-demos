# Guesslang - 语言检测模型

原始项目用于检测源代码的编程语言。

## 简化版本

本项目提供了两个版本的预测器：

### 1. TensorFlow 版本（推荐用于验证）

```python
from predictor import LanguagePredictor

predictor = LanguagePredictor()
result = predictor.predict("def hello(): pass")
print(result)  # [('Python', 0.0441), ...]
```

### 2. ONNX 版本（单文件模型）

详细使用方法请参考 [README_ONNX_SOLUTION.md](README_ONNX_SOLUTION.md)

## 项目结构

```
├── predictor.py                    # TensorFlow 预测器（简化版）
├── preprocessing.py                # 手动预处理实现
├── extract_model_layers.py         # 模型权重提取
├── build_onnx_model.py             # 构建 ONNX 兼容模型
├── convert_onnx_compatible_to_onnx.py  # ONNX 转换
├── model_onnx.py                   # ONNX 推理器
├── test_onnx_final.py              # 测试脚本
├── example_usage.py                # 使用示例
└── guesslang/                      # 原始 guesslang 代码
```

## 快速开始

### TensorFlow 版本
```bash
uv run python example_usage.py
```

### ONNX 版本
```bash
# 1. 构建模型
uv run python build_onnx_model.py

# 2. 转换为 ONNX
uv run python convert_onnx_compatible_to_onnx.py

# 3. 测试
uv run python test_onnx_final.py
```

## 原始项目

- GitHub: https://github.com/yoeo/guesslang
- 文档: https://guesslang.readthedocs.io/
