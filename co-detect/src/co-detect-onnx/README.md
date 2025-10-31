# ONNX Language Detection

独立的 ONNX 语言检测项目，不依赖 TensorFlow 运行时。

## 快速开始

```bash
# 安装依赖
uv sync

# 运行演示
python demo.py
```

## 使用示例

```python
from model_onnx import LanguagePredictorONNXFinal

# 初始化
predictor = LanguagePredictorONNXFinal()

# 预测语言
result = predictor.predict_language('def hello(): pass')
print(result)  # 输出: Python

# 获取所有预测概率
results = predictor.predict('def hello(): pass')
for lang, prob in results[:5]:
    print(f"{lang}: {prob:.2%}")
```
