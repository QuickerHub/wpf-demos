# Semantic Kernel 复杂对象函数调用调研

## 概述

Semantic Kernel 支持在 KernelFunction 中使用复杂对象作为参数和返回值。本文档总结了处理复杂对象的最佳实践。

## 1. 基本支持

### 1.1 复杂对象作为返回值

Semantic Kernel **支持**复杂对象作为返回值：

```csharp
[KernelFunction]
[Description("Get a specific variable's information")]
public VariableClass? GetVariable(string variableName)
{
    return _toolHandler.GetVariable(variableName);
}
```

**工作原理：**
- Semantic Kernel 会自动将复杂对象序列化为 JSON
- AI 模型会收到 JSON 格式的返回值
- 序列化使用 `System.Text.Json.JsonSerializer`

### 1.2 复杂对象作为参数

**重要发现：** Semantic Kernel **不直接支持**复杂对象作为参数类型。

#### 问题示例

```csharp
// ❌ 不推荐：直接使用复杂对象作为参数
[KernelFunction]
public string CreateVariables(List<VariableClass> variables)
{
    // 这种方式可能无法正常工作
}
```

**原因：**
- AI 模型在调用函数时，参数会被序列化为 JSON 字符串
- Semantic Kernel 需要能够将 JSON 字符串反序列化为参数类型
- 对于复杂类型（如 `List<VariableClass>`），反序列化可能失败或不稳定

## 2. 推荐方案

### 2.1 方案一：使用 JSON 字符串参数（当前实现）

**优点：**
- ✅ 完全可控的序列化/反序列化过程
- ✅ 明确的错误处理
- ✅ AI 模型更容易理解 JSON 格式
- ✅ 可以自定义 JSON 格式和验证

**实现：**

```csharp
[KernelFunction]
[Description("Test an expression with optional variable values")]
public async Task<string> TestExpression(
    [Description("Expression to test")] string expression,
    [Description("Optional JSON array: [{\"VarName\":\"name\",\"VarType\":\"String\",\"DefaultValue\":\"value\"}]")] 
    string? variables = null)
{
    List<VariableClass>? variablesToUse = null;
    
    if (!string.IsNullOrWhiteSpace(variables))
    {
        try
        {
            variablesToUse = JsonSerializer.Deserialize<List<VariableClass>>(variables);
        }
        catch (JsonException ex)
        {
            return $"Error: Invalid JSON format. {ex.Message}";
        }
    }
    
    // 使用解析后的变量...
}
```

**辅助方法：**

```csharp
private (bool Success, List<VariableClass>? Variables, string ErrorMessage) ParseVariablesJson(string json)
{
    if (string.IsNullOrWhiteSpace(json))
    {
        return (false, null, "Error: JSON string cannot be empty.");
    }

    try
    {
        var variables = JsonSerializer.Deserialize<List<VariableClass>>(json);
        if (variables == null)
        {
            return (false, null, "Error: Failed to deserialize variables.");
        }
        return (true, variables, string.Empty);
    }
    catch (JsonException ex)
    {
        return (false, null, $"Error: Invalid JSON format. {ex.Message}");
    }
}
```

### 2.2 方案二：使用简单类型参数（如果可能）

如果复杂对象可以拆分为简单类型，优先使用简单类型：

```csharp
// ✅ 推荐：使用简单类型
[KernelFunction]
public string CreateVariable(
    string name,
    VariableType varType,
    object? defaultValue = null)
{
    // 实现...
}
```

### 2.3 方案三：多个简单参数

如果对象属性不多，可以拆分为多个参数：

```csharp
[KernelFunction]
public string UpdateVariable(
    string name,
    string? newName = null,
    VariableType? newType = null,
    object? newDefaultValue = null)
{
    // 实现...
}
```

## 3. 当前项目中的实践

### 3.1 已实现的模式

1. **返回值使用复杂对象：**
   - `GetVariable()` 返回 `VariableClass?` ✅
   - 工作正常，因为 Semantic Kernel 会自动序列化

2. **参数使用 JSON 字符串：**
   - `TestExpression(expression, string? variables = null)` ✅
   - `SetExpression(expression, string? variables = null)` ✅（已移除）
   - 使用 `ParseVariablesJson` 辅助方法解析

3. **参数使用简单类型：**
   - `CreateVariable(name, varType, defaultValue)` ✅
   - `SetVarDefaultValue(name, defaultValue)` ✅

### 3.2 最佳实践总结

| 场景 | 推荐方案 | 示例 |
|------|---------|------|
| 单个复杂对象参数 | JSON 字符串 | `TestExpression(expression, string? variables)` |
| 多个简单属性 | 多个简单参数 | `CreateVariable(name, varType, defaultValue)` |
| 复杂对象返回值 | 直接返回对象 | `GetVariable()` 返回 `VariableClass?` |
| 列表/集合参数 | JSON 字符串 | `variables: "[{...}, {...}]"` |

## 4. 技术细节

### 4.1 序列化配置

Semantic Kernel 使用 `System.Text.Json` 进行序列化：

```csharp
// 自动序列化（返回值）
var result = JsonSerializer.Serialize(complexObject);

// 手动反序列化（参数）
var obj = JsonSerializer.Deserialize<ComplexType>(jsonString);
```

### 4.2 错误处理

对于 JSON 参数，必须处理以下错误：
- JSON 格式错误（`JsonException`）
- 反序列化失败（返回 null）
- 类型不匹配

### 4.3 类型支持

Semantic Kernel 原生支持的类型：
- ✅ 基本类型：`string`, `int`, `double`, `bool`, `DateTime`
- ✅ 枚举类型：`VariableType`（会被序列化为字符串）
- ✅ 可空类型：`string?`, `int?`, `object?`
- ⚠️ 复杂对象：需要手动 JSON 序列化/反序列化
- ⚠️ 集合类型：`List<T>` 需要 JSON 字符串

## 5. 参考资源

- [Semantic Kernel 插件文档](https://learn.microsoft.com/zh-cn/semantic-kernel/concepts/plugins/)
- [System.Text.Json 文档](https://learn.microsoft.com/zh-cn/dotnet/api/system.text.json)

## 6. 结论

**推荐做法：**
1. **返回值**：可以直接使用复杂对象，Semantic Kernel 会自动序列化
2. **参数**：复杂对象应使用 JSON 字符串，并在方法内部手动反序列化
3. **简单对象**：如果属性较少，优先拆分为多个简单类型参数

**当前项目已遵循最佳实践：**
- ✅ 使用 JSON 字符串处理复杂参数（`TestExpression`）
- ✅ 使用简单类型参数（`CreateVariable`, `SetVarDefaultValue`）
- ✅ 返回值直接使用复杂对象（`GetVariable`）
- ✅ 提供辅助方法统一处理 JSON 解析（`ParseVariablesJson`）

