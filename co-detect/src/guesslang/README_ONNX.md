# ONNX 模型转换和使用

本目录包含将 guesslang TensorFlow 模型转换为 ONNX 格式的代码。

## 文件说明

### 核心文件

### 转换相关文件（位于 `convert/` 文件夹）

1. **`convert/preprocessing.py`** - 文本预处理（手动实现）
   - 将源代码转换为 hash indices
   - 使用 TensorFlow 的字符串操作确保哈希一致性

2. **`convert/extract_model_layers.py`** - 提取模型权重
   - 从原始 TensorFlow SavedModel 中提取权重
   - 返回权重字典供后续使用

3. **`convert/build_onnx_model.py`** - 构建 ONNX 兼容模型
   - 使用提取的权重构建新的 TensorFlow 模型
   - 该模型接受预处理后的 hash indices 作为输入
   - 排除 padding hash (2263) 以避免 logits 爆炸
   - 保存为 SavedModel 格式到 `onnx_compatible_model/`（临时文件夹）

4. **`convert/convert_onnx_compatible_to_onnx.py`** - 转换为 ONNX
   - 使用 `tf2onnx` 将 SavedModel 转换为 ONNX 格式
   - 输入：`onnx_compatible_model/`（临时文件夹）
   - 输出：`model_final.onnx`（最终模型）

### 推理相关文件（位于根目录）

5. **`model_onnx.py`** - ONNX 推理代码
   - 使用 ONNX Runtime 进行推理
   - 自动处理预处理和后处理

6. **`test_onnx.py`** - ONNX 模型测试脚本
   - 测试 ONNX 模型在各种代码样本上的表现
   - 包含多种编程语言的示例代码

7. **`test_tf.py`** - TensorFlow 模型测试脚本
   - 测试原始 TensorFlow 模型的表现
   - 使用与 ONNX 测试相同的测试用例

8. **`test_compare.py`** - 模型对比测试脚本
   - 同时测试 TensorFlow 和 ONNX 模型
   - 对比两个模型的预测结果
   - 显示概率差异和匹配情况

### 使用流程

```bash
# 1. 构建 ONNX 兼容模型（两种方式都可以）
python convert/build_onnx_model.py
# 或者
python -m convert.build_onnx_model

# 2. 转换为 ONNX 格式
python convert/convert_onnx_compatible_to_onnx.py

# 3. 使用 ONNX 模型进行推理
python -c "from model_onnx import LanguagePredictorONNXFinal; p = LanguagePredictorONNXFinal(); print(p.predict_language('def hello(): pass'))"

# 或者运行测试脚本
python test_onnx.py          # 测试 ONNX 模型
python test_tf.py            # 测试 TensorFlow 模型
python test_compare.py       # 对比两个模型
```

## 技术要点

### 问题解决

1. **TensorFlow 字符串操作不支持 ONNX**
   - 解决：手动实现预处理，只转换数值计算部分

2. **Padding hash 导致 logits 爆炸**
   - 解决：在聚合时排除 padding hash (2263)

3. **TensorScatterAdd 不支持**
   - 解决：使用 `tf.one_hot` + `reduce_sum` 替代

### 模型结构

- **输入**: Hash indices `[batch_size, 10000]`
- **DNN 路径**: Embedding → DNN [512, 32] → Logits
- **Linear 路径**: Sparse features (5000维) → Linear → Logits
- **输出**: Combined logits → Softmax → Scores `[batch_size, 54]`

## 依赖

- `tensorflow==2.5.0`
- `tf2onnx`
- `onnxruntime`

## 测试结果

运行 `test_compare.py` 可以验证 ONNX 模型与原始 TensorFlow 模型的一致性：

- ✅ 所有测试用例的 Top 1 预测完全匹配
- ✅ 所有测试用例的 Top 5 预测完全匹配
- ✅ 所有概率值完全相同（差异为 0）

这证明了 ONNX 转换是完全成功的，模型行为完全一致。

## 注意事项

- `onnx_compatible_model/` 是转换过程中的临时文件夹，包含中间 SavedModel，可以删除
- 如果需要重新转换 ONNX 模型，只需重新运行步骤 1-2，会重新生成临时文件夹

