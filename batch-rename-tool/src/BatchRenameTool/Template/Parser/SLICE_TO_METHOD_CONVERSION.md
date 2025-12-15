# 切片语法转换为方法调用

## 概述

切片语法 `[start:end]` 现在在解析阶段被转换为方法调用 `.slice(start, end)`，使 AST 更加统一。

## 转换规则

### 语法转换

| 原始语法 | 转换后 | 说明 |
|---------|--------|------|
| `name[:3]` | `name.slice(0, 3)` | 从开始到索引3 |
| `name[3:]` | `name.slice(3)` | 从索引3到结尾 |
| `name[1:3]` | `name.slice(1, 3)` | 从索引1到索引3 |
| `name[:]` | `name.slice()` | 整个字符串（无参数） |

### 实现细节

在 `TemplateParser.ParseSlice()` 方法中：

1. **`[:3]`** - start 为 null，end 为 3
   - 转换为 `.slice(0, 3)` - 添加 start=0

2. **`[3:]`** - start 为 3，end 为 null
   - 转换为 `.slice(3)` - 只有 start 参数

3. **`[1:3]`** - start 为 1，end 为 3
   - 转换为 `.slice(1, 3)` - 两个参数

4. **`[:]`** - start 为 null，end 为 null
   - 转换为 `.slice()` - 无参数，返回完整字符串

## 优势

1. **统一的 AST**：所有操作都通过方法调用实现，不再需要单独的 `SliceNode`
2. **方法链支持**：切片可以与其他方法链式调用
3. **更易扩展**：添加新的切片变体只需修改方法实现
4. **向后兼容**：旧的 `SliceNode` 仍然可以在执行器中处理（向后兼容）

## 示例

### 基本切片

```
模板: {name[:3]}
解析: VariableNode("name") -> MethodNode("slice", [LiteralNode(0), LiteralNode(3)])
执行: StringValue("test").InvokeMethod("slice", [NumberValue(0), NumberValue(3)])
结果: "tes"
```

### 方法链

```
模板: {name[:3].upper()}
解析: VariableNode("name") 
      -> MethodNode("slice", [LiteralNode(0), LiteralNode(3)])
      -> MethodNode("upper", [])
执行: StringValue("test")
      -> StringValue("tes")
      -> StringValue("TES")
结果: "TES"
```

## 代码位置

- **解析器**：`TemplateParser.ParseSlice()` - 将切片语法转换为 `MethodNode`
- **评估器**：`TemplateEvaluator.ExecuteSlice()` - 处理 `slice` 方法调用
- **编译器**：`TemplateCompiler.CompileSliceMethod()` - 编译 `slice` 方法调用
- **表达式执行器**：`StringValue.ExecuteSlice()` - 处理 `slice` 方法调用

## 测试

所有切片相关的测试都已通过：
- ✅ `{name[1:3]}` - 基本切片
- ✅ `{name[:3]}` - 从开始切片（转换为 `.slice(0, 3)`）
- ✅ `{name[1:]}` - 到结尾切片（转换为 `.slice(1)`）
- ✅ `{name[:]}` - 完整字符串（转换为 `.slice()`）
- ✅ 所有 140 个测试通过

## 实现细节

### 解析器转换逻辑

```csharp
// [:3] -> start=null, end=3 -> .slice(0, 3)
if (start == null && end != null)
{
    arguments.Add(new LiteralNode(0));
    arguments.Add(end);
}
// [3:] -> start=3, end=null -> .slice(3)
else if (start != null && end == null)
{
    arguments.Add(start);
}
// [1:3] -> start=1, end=3 -> .slice(1, 3)
else if (start != null && end != null)
{
    arguments.Add(start);
    arguments.Add(end);
}
// [:] -> start=null, end=null -> .slice()
```

### 支持的执行器

1. **TemplateEvaluator** - 旧评估器，已添加 `ExecuteSlice` 方法
2. **TemplateCompiler** - 编译器，已添加 `CompileSliceMethod` 方法
3. **TemplateExpressionExecutor** - 新表达式执行器，使用 `StringValue.ExecuteSlice` 方法

