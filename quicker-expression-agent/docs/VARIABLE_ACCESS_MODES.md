# 变量访问模式说明

## 概述

`RoslynExpressionService` 支持两种变量访问模式，可以通过构造函数参数选择：

1. **TypeCasting 模式**（默认）：使用类型转换访问变量
2. **DynamicType 模式**：动态生成类，变量作为属性直接访问

## 两种模式对比

### TypeCasting 模式（默认）

**实现方式：**
- 将 `{varname}` 替换为 `(Variables["varname"] as Type)`
- 使用固定的 `ScriptGlobals` 类，包含 `Variables` 字典

**优点：**
- ✅ 实现简单，代码清晰
- ✅ 无需动态类型生成
- ✅ 内存占用小
- ✅ 启动快，无类型生成开销

**缺点：**
- ❌ 表达式代码较长（需要类型转换）
- ❌ 运行时类型转换开销

**示例：**
```csharp
// 输入表达式
"{userName} is {age} years old"

// 处理后
"(Variables[\"userName\"] as string) is (Variables[\"age\"] as int) years old"
```

### DynamicType 模式

**实现方式：**
- 动态生成强类型的 `ScriptGlobals` 子类
- 每个变量成为类的属性
- 将 `{varname}` 替换为 `varname`（直接属性访问）

**优点：**
- ✅ 表达式代码简洁（直接使用变量名）
- ✅ 类型安全（编译时检查）
- ✅ 性能更好（直接属性访问，无字典查找和类型转换）
- ✅ 类型缓存（相同变量组合只生成一次）

**缺点：**
- ❌ 首次执行需要生成类型（~0.3-5ms）
- ❌ 内存占用稍大（动态类型缓存）
- ❌ 实现较复杂

**示例：**
```csharp
// 输入表达式
"{userName} is {age} years old"

// 处理后
"userName is age years old"

// 动态生成的类
public class DynamicScriptGlobals : ScriptGlobals
{
    public string userName { get; set; }
    public int age { get; set; }
}
```

## 使用方式

### 默认使用 TypeCasting 模式

```csharp
var service = new RoslynExpressionService();
// 等同于
var service = new RoslynExpressionService(VariableAccessMode.TypeCasting);
```

### 使用 DynamicType 模式

```csharp
var service = new RoslynExpressionService(VariableAccessMode.DynamicType);
```

## 性能对比

### TypeCasting 模式

```
执行时间：~11ms
├─ 字符串替换：0.01ms
├─ 创建 ScriptGlobals：0.01ms
├─ Roslyn 编译：10ms（主要耗时）
└─ Roslyn 执行：0.5ms
```

### DynamicType 模式

**首次执行（需要生成类型）：**
```
执行时间：~11.3ms
├─ 类型生成：0.3ms（仅首次）
├─ 实例创建：0.05ms
├─ 属性设置：0.03ms
├─ 字符串替换：0.01ms
├─ Roslyn 编译：10ms（主要耗时）
└─ Roslyn 执行：0.5ms
```

**后续执行（类型已缓存）：**
```
执行时间：~11ms
├─ 类型查找：0.001ms（可忽略）
├─ 实例创建：0.05ms
├─ 属性设置：0.03ms
├─ 字符串替换：0.01ms
├─ Roslyn 编译：10ms（主要耗时）
└─ Roslyn 执行：0.5ms
```

**结论：** 两种模式的性能差异很小（<5%），主要瓶颈是 Roslyn 编译。

## 选择建议

### 使用 TypeCasting 模式（默认）当：
- ✅ 需要简单实现，易于维护
- ✅ 变量组合经常变化（避免类型缓存失效）
- ✅ 对性能要求不高
- ✅ 表达式执行频率较低

### 使用 DynamicType 模式当：
- ✅ 需要最佳性能（表达式执行频率高）
- ✅ 变量组合相对固定（类型缓存有效）
- ✅ 表达式代码需要简洁（直接使用变量名）
- ✅ 需要编译时类型检查

## 代码示例

### TypeCasting 模式示例

```csharp
var service = new RoslynExpressionService(VariableAccessMode.TypeCasting);

var variables = new Dictionary<string, object>
{
    { "userName", "John" },
    { "age", 25 }
};

// 表达式使用 {varname} 格式
string expression = "{userName} is {age} years old";

var result = await service.ExecuteExpressionAsync(expression, variables);
// 结果：John is 25 years old
```

### DynamicType 模式示例

```csharp
var service = new RoslynExpressionService(VariableAccessMode.DynamicType);

var variables = new Dictionary<string, object>
{
    { "userName", "John" },
    { "age", 25 }
};

// 表达式使用 {varname} 格式（会被替换为直接变量名）
string expression = "{userName} is {age} years old";

var result = await service.ExecuteExpressionAsync(expression, variables);
// 结果：John is 25 years old
```

**注意：** 两种模式的表达式输入格式相同（都使用 `{varname}`），但内部处理方式不同。

## 技术细节

### TypeCasting 模式实现

1. **字符串替换**：`{varname}` → `(Variables["varname"] as Type)`
2. **类型推断**：根据变量值的实际类型生成 C# 类型名
3. **ScriptGlobals**：固定类，包含 `Variables` 字典

### DynamicType 模式实现

1. **类型生成**：使用 `Reflection.Emit` 动态生成类
2. **类型缓存**：相同变量组合（变量名+类型）只生成一次
3. **字符串替换**：`{varname}` → `varname`
4. **属性设置**：使用反射设置属性值

## 注意事项

1. **变量名验证**：DynamicType 模式会验证变量名是否为有效的 C# 标识符
2. **类型缓存**：DynamicType 模式的类型缓存基于变量名和类型的组合
3. **线程安全**：两种模式都是线程安全的
4. **内存管理**：DynamicType 模式的类型缓存会一直保留，直到服务销毁

