"""Analyze why Python code is misidentified as INI"""

import json
from pathlib import Path

# Check if model_onnx exists
try:
    from model_onnx import LanguagePredictorONNXFinal
    predictor = LanguagePredictorONNXFinal()
    
    # Test the problematic case
    problematic_code = "x = [1, 2, 3]\nfor item in x:\n    print(item)"
    
    print("=" * 60)
    print("Analyzing problematic Python code:")
    print("=" * 60)
    print(f"Code:\n{problematic_code}\n")
    
    results = predictor.predict(problematic_code)
    
    print("Top 10 predictions:")
    for i, (lang, prob) in enumerate(results[:10], 1):
        marker = " <-- WRONG!" if lang == "INI" else ""
        print(f"  {i:2}. {lang:12} {prob:6.2%}{marker}")
        
except Exception as e:
    print(f"Error: {e}")
    print("\nUsing test cases analysis instead...")
    
    # Load test cases and explain
    test_cases = json.loads(Path('test_cases.json').read_text(encoding='utf-8'))
    
    print("\n" + "=" * 60)
    print("Why Python code may be misidentified as INI:")
    print("=" * 60)
    print("""
原因分析：

1. **代码太短，特征不明显**
   - 短代码片段缺乏足够的语言特征
   - 模型基于 n-gram (bigrams) 特征，需要足够的上下文

2. **语法相似性**
   - `x = [1, 2, 3]` 看起来像 INI 的 `key = value` 格式
   - INI 文件格式：`section = value` 或 `key = value`
   - Python 赋值语句：`variable = value`

3. **缺少 Python 特有的关键字**
   - 没有 `def`, `class`, `import` 等明显的 Python 关键字
   - `for item in x:` 虽然独特，但在短代码中权重可能不够

4. **字符特征重叠**
   - 都使用 `=` 赋值
   - 都可能有数字和列表结构
   - INI 文件中也可能有类似的格式

建议：使用包含更多 Python 特征的代码，如：
- 使用 `def` 定义函数
- 使用 `if __name__ == '__main__':`
- 使用 Python 特有的语法如 `import`, `lambda`, `with` 等
""")
