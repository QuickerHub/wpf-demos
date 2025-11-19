# .NET 表达式执行器库调研

## 概述

本文档调研了类似 Z.Expression 的 .NET 表达式执行器库，用于在运行时动态解析和执行 C# 表达式。

## 主要库对比

### 1. **Roslyn** (Microsoft.CodeAnalysis.CSharp.Scripting)

**特点：**
- ✅ 微软官方支持，功能最强大
- ✅ 完整的 C# 语法支持
- ✅ 支持脚本执行、代码分析、重构等
- ✅ 支持强类型 ScriptGlobals
- ✅ 活跃维护，文档完善
- ❌ 体积较大，依赖较多
- ❌ 性能相对较慢（需要编译）
- ❌ 学习曲线较陡

**适用场景：**
- 需要完整 C# 语法支持
- 需要代码分析和重构功能
- 对性能要求不高的场景

**NuGet 包：**
```
Microsoft.CodeAnalysis.CSharp.Scripting
```

**示例：**
```csharp
// 静态方式：编译时已知变量
public class ScriptGlobals
{
    public int x { get; set; }
    public int y { get; set; }
}

var script = CSharpScript.Create("x + y", ScriptOptions.Default, typeof(ScriptGlobals));
var globals = new ScriptGlobals { x = 1, y = 2 };
var result = await script.RunAsync(globals);
```

**从 Dictionary<string, object> 实现直接变量访问：**

Roslyn 需要强类型的 ScriptGlobals，但我们可以通过以下方式实现动态变量访问：

### 方案 1：使用 Reflection.Emit 动态生成类（当前实现）

**原理：**
1. 根据 `Dictionary<string, object>` 动态生成一个强类型的 ScriptGlobals 类
2. 为每个变量创建对应的属性
3. 使用类型缓存优化性能

**实现步骤：**
```csharp
// 1. 从 Dictionary 创建动态类型
Dictionary<string, object> variables = new() { { "x", 1 }, { "y", 2 } };

// 2. 动态生成 ScriptGlobals 类（使用 Reflection.Emit）
Type globalsType = CreateDynamicGlobalsType(variables);
// 生成的类类似于：
// public class DynamicScriptGlobals : ScriptGlobals
// {
//     public int x { get; set; }
//     public int y { get; set; }
// }

// 3. 创建实例并设置属性值
object globals = Activator.CreateInstance(globalsType);
globalsType.GetProperty("x").SetValue(globals, 1);
globalsType.GetProperty("y").SetValue(globals, 2);

// 4. 使用动态类型创建脚本
var script = CSharpScript.Create("x + y", ScriptOptions.Default, globalsType);
var result = await script.RunAsync(globals);
```

**优点：**
- ✅ 类型安全，性能好（编译后直接访问属性）
- ✅ 支持完整的 C# 语法
- ✅ 可以缓存类型定义，避免重复生成

**缺点：**
- ❌ 实现较复杂，需要 IL 代码生成
- ❌ 需要处理类型转换和验证

### 方案 2：使用 ExpandoObject（不推荐）

```csharp
// 尝试使用 ExpandoObject
var expando = new ExpandoObject() as IDictionary<string, object>;
expando["x"] = 1;
expando["y"] = 2;

// ❌ 问题：Roslyn 需要强类型，ExpandoObject 是 dynamic，无法直接使用
// var script = CSharpScript.Create("x + y", ScriptOptions.Default, typeof(ExpandoObject));
// 这会导致编译错误，因为 Roslyn 无法识别动态属性
```

### 方案 3：使用匿名类型（不适用）

```csharp
// ❌ 匿名类型需要在编译时知道所有属性，无法动态创建
var globals = new { x = 1, y = 2 };
// 无法从 Dictionary 动态创建匿名类型
```

### 方案 4：使用代码生成（替代方案）

```csharp
// 使用字符串拼接生成 C# 代码，然后编译
string classCode = $@"
public class DynamicScriptGlobals : ScriptGlobals
{{
    public int x {{ get; set; }}
    public int y {{ get; set; }}
}}
";

// 使用 Roslyn 编译代码，然后创建实例
// 这种方式更灵活，但性能较差
```

**当前项目实现：**

项目已实现方案 1（Reflection.Emit），核心代码在 `RoslynExpressionService.CreateDynamicGlobals()` 方法中：

```csharp
// 输入：Dictionary<string, object>
var variables = new Dictionary<string, object> { { "x", 1 }, { "y", 2 } };

// 输出：可以直接在脚本中使用 x + y
var globals = CreateDynamicGlobals(variables);
var script = CSharpScript.Create("x + y", _scriptOptions, globals.GetType());
var result = await script.RunAsync(globals);
```

**关键点：**
1. **类型缓存**：相同变量组合只生成一次类型，提高性能
2. **变量名验证**：确保变量名是有效的 C# 标识符
3. **类型推断**：根据值的实际类型确定属性类型

## 核心需求：表达式直接访问程序变量

### 需求说明

**目标：** 表达式代码能够直接使用变量名，就像访问程序中的局部变量一样。

**示例：**
```csharp
// 输入变量
var variables = new Dictionary<string, object>
{
    { "userName", "John" },
    { "age", 25 },
    { "scores", new List<int> { 90, 85, 95 } }
};

// 表达式代码（直接使用变量名）
string expression = "userName + \" is \" + age + \" years old. Average score: \" + scores.Average()";

// 执行结果：John is 25 years old. Average score: 90
```

### 实现验证

当前实现**完全满足**这个需求：

1. **动态生成 ScriptGlobals 类**：
   ```csharp
   // Dictionary { { "userName", "John" }, { "age", 25 } }
   // ↓ 动态生成
   public class DynamicScriptGlobals : ScriptGlobals
   {
       public string userName { get; set; }
       public int age { get; set; }
   }
   ```

2. **脚本可以直接访问**：
   ```csharp
   // 表达式代码
   "userName + \" is \" + age"
   
   // 编译后等价于
   globals.userName + " is " + globals.age
   
   // 但脚本中直接写变量名即可，无需 globals. 前缀
   ```

3. **完整示例**：
   ```csharp
   // 1. 准备变量
   var variables = new Dictionary<string, object>
   {
       { "x", 10 },
       { "y", 20 },
       { "items", new List<string> { "a", "b", "c" } }
   };
   
   // 2. 执行表达式（直接使用变量名）
   var result = await roslynService.ExecuteExpressionAsync(
       "x + y + items.Count",  // 直接使用 x, y, items
       variables
   );
   
   // 3. 结果：33 (10 + 20 + 3)
   ```

### 与其他方案的对比

| 方案 | 表达式写法 | 是否满足需求 |
|------|-----------|------------|
| **当前实现（Reflection.Emit）** | `x + y` | ✅ 完全满足 |
| **ExpandoObject** | `Variables["x"] + Variables["y"]` | ❌ 不满足 |
| **静态 ScriptGlobals** | `x + y` | ✅ 满足，但不支持动态变量 |
| **字符串替换** | `{x} + {y}` → `x + y` | ⚠️ 部分满足，需要预处理 |

### 实际使用场景

在 Quicker Expression Agent 中：

```csharp
// Agent 生成的表达式
string expression = "\"Hello, \" + userName + \"! You have \" + itemCount + \" items.\"";

// 执行时传入变量
var variables = new Dictionary<string, object>
{
    { "userName", "John" },
    { "itemCount", 5 }
};

// 表达式直接访问变量，无需任何包装
var result = await ExecuteExpressionAsync(expression, variables);
// 结果：Hello, John! You have 5 items.
```

**关键优势：**
- ✅ 表达式代码自然、易读
- ✅ 支持完整的 C# 语法（LINQ、方法调用等）
- ✅ 类型安全（编译时检查）
- ✅ 性能优化（类型缓存）

---

### 2. **DynamicExpresso**

**特点：**
- ✅ 轻量级，专门用于表达式执行
- ✅ 支持大部分 C# 语法（运算符、方法调用、Lambda 等）
- ✅ 支持变量、函数注册
- ✅ 性能较好（表达式树编译）
- ✅ API 简单易用
- ❌ 不支持完整的 C# 语法（如类定义）
- ❌ 社区相对较小

**适用场景：**
- 需要轻量级表达式执行
- 主要处理数学表达式和简单逻辑
- 对性能有要求的场景

**NuGet 包：**
```
DynamicExpresso.Core
```

**示例：**
```csharp
var interpreter = new Interpreter();
interpreter.SetVariable("x", 1);
interpreter.SetVariable("y", 2);
var result = interpreter.Eval("x + y");
```

---

### 3. **NCalc**

**特点：**
- ✅ 专门用于数学表达式计算
- ✅ 轻量级，性能好
- ✅ 支持自定义函数和参数
- ✅ 支持逻辑表达式（if/then/else）
- ❌ 不支持完整的 C# 语法
- ❌ 主要用于数学计算，不适合复杂逻辑

**适用场景：**
- 数学表达式计算
- 公式引擎
- 简单的条件表达式

**NuGet 包：**
```
NCalc
```

**示例：**
```csharp
var expression = new Expression("2 + 3 * 5");
var result = expression.Evaluate();
```

---

### 4. **Flee**

**特点：**
- ✅ 轻量级表达式执行器
- ✅ 支持变量、函数、类型
- ✅ 性能较好
- ❌ 项目维护不活跃
- ❌ 文档较少

**适用场景：**
- 需要轻量级表达式执行
- 不需要完整 C# 语法

**NuGet 包：**
```
flee
```

---

### 5. **Eval-Expression.NET**

**特点：**
- ✅ 类似 JavaScript `eval` 功能
- ✅ 支持动态编译和执行 C# 代码
- ✅ 支持访问私有成员（通过反射）
- ❌ 安全性较低（可执行任意代码）
- ❌ 性能较差

**适用场景：**
- 需要执行完整 C# 代码
- 内部使用，安全性要求不高

**NuGet 包：**
```
Eval-Expression.NET
```

---

### 6. **Jint** (JavaScript 解释器)

**特点：**
- ✅ 在 .NET 中执行 JavaScript 代码
- ✅ 支持与 .NET 对象交互
- ✅ 性能较好
- ❌ 语法是 JavaScript，不是 C#

**适用场景：**
- 需要执行 JavaScript 代码
- 与现有 JavaScript 代码集成

**NuGet 包：**
```
Jint
```

---

## 性能对比

| 库 | 性能 | 体积 | 语法支持 | 易用性 |
|---|---|---|---|---|
| Roslyn | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| DynamicExpresso | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| NCalc | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| Flee | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| Eval-Expression.NET | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

## 推荐方案

### 当前项目（Quicker Expression Agent）

**当前使用：Roslyn**

**优点：**
- ✅ 已经集成，代码已实现
- ✅ 支持完整 C# 语法
- ✅ 支持强类型变量访问（通过动态生成 ScriptGlobals）

**缺点：**
- ❌ 性能相对较慢
- ❌ 依赖较大

**建议：**
1. **继续使用 Roslyn**（如果性能可接受）
   - 当前实现已经支持直接变量访问（通过动态生成 ScriptGlobals 类）
   - 功能完整，满足需求

2. **考虑 DynamicExpresso**（如果需要更高性能）
   - 如果发现 Roslyn 性能成为瓶颈
   - 可以评估迁移到 DynamicExpresso
   - 需要确认是否支持所有需要的 C# 语法特性

3. **混合方案**（如果场景不同）
   - 简单表达式使用 DynamicExpresso
   - 复杂表达式使用 Roslyn

## 迁移到 DynamicExpresso 的考虑

如果考虑迁移到 DynamicExpresso，需要注意：

1. **语法差异：**
   - DynamicExpresso 不支持完整的 C# 语法
   - 需要确认是否支持所有需要的特性

2. **变量访问：**
   - DynamicExpresso 使用 `SetVariable` 注册变量
   - 脚本中可以直接使用变量名（如 `x + y`）

3. **类型支持：**
   - 需要确认是否支持所有需要的类型（List、Dictionary 等）

4. **性能提升：**
   - 需要实际测试确认性能提升幅度

## 参考资料

- [Roslyn GitHub](https://github.com/dotnet/roslyn)
- [DynamicExpresso GitHub](https://github.com/dynamicexpresso/DynamicExpresso)
- [NCalc GitHub](https://github.com/ncalc/ncalc)
- [Flee GitHub](https://github.com/mparlak/Flee)

