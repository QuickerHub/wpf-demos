# 动态类型生成性能分析

## 概述

本文档分析使用 `Reflection.Emit` 动态生成 ScriptGlobals 类的性能特征。

## 性能特点

### 1. 类型生成（首次）

**操作：** `CreateDynamicGlobalsType()` - 使用 Reflection.Emit 生成类

**耗时估算：**
- **1-3 个变量：** ~0.1-0.5ms
- **5-10 个变量：** ~0.5-2ms
- **10+ 个变量：** ~2-5ms

**影响因素：**
- 变量数量（主要因素）
- 变量类型复杂度
- JIT 编译时间

**优化：** ✅ 已实现类型缓存，相同变量组合只生成一次

### 2. 类型查找（缓存命中）

**操作：** 从 `_globalsTypeCache` 查找已生成的类型

**耗时：** ~0.001-0.01ms（字典查找，非常快）

**优化：** ✅ 已实现，使用字典缓存

### 3. 实例创建

**操作：** `Activator.CreateInstance(globalsType)`

**耗时：** ~0.01-0.1ms

**影响因素：**
- 类型复杂度
- 构造函数复杂度（当前为默认构造函数，很快）

### 4. 属性设置

**操作：** 使用反射设置属性值

**耗时：**
- **1-3 个变量：** ~0.01-0.05ms
- **5-10 个变量：** ~0.05-0.2ms
- **10+ 个变量：** ~0.2-0.5ms

**影响因素：**
- 变量数量（主要因素）
- 类型转换开销

**优化建议：** ⚠️ 可以优化（见下文）

## 总体性能

### 首次执行（需要生成类型）

```
总耗时 = 类型生成 + 实例创建 + 属性设置 + Roslyn 编译 + Roslyn 执行

典型场景（3个变量）：
- 类型生成：     ~0.3ms
- 实例创建：     ~0.05ms
- 属性设置：     ~0.03ms
- Roslyn 编译：  ~5-20ms（主要耗时）
- Roslyn 执行：  ~0.1-1ms
─────────────────────────
总计：          ~5-22ms
```

### 后续执行（类型已缓存）

```
总耗时 = 类型查找 + 实例创建 + 属性设置 + Roslyn 编译 + Roslyn 执行

典型场景（3个变量）：
- 类型查找：     ~0.001ms（可忽略）
- 实例创建：     ~0.05ms
- 属性设置：     ~0.03ms
- Roslyn 编译：  ~5-20ms（主要耗时）
- Roslyn 执行：  ~0.1-1ms
─────────────────────────
总计：          ~5-21ms
```

**结论：** 动态类型生成的开销相对于 Roslyn 编译和执行来说很小（<5%）

## 性能瓶颈分析

### 主要瓶颈

1. **Roslyn 编译**（~5-20ms）
   - 这是最大的性能瓶颈
   - 动态类型生成只占很小一部分

2. **Roslyn 执行**（~0.1-1ms）
   - 取决于表达式复杂度

3. **动态类型生成**（~0.3-5ms，仅首次）
   - 后续执行通过缓存优化

### 次要瓶颈

1. **属性设置（反射）**（~0.01-0.5ms）
   - 每次执行都需要设置
   - 可以通过委托缓存优化

## 优化建议

### 1. ✅ 已实现：类型缓存

**当前实现：**
```csharp
private readonly Dictionary<string, Type> _globalsTypeCache = new();
```

**效果：** 相同变量组合只生成一次类型，后续直接使用

**性能提升：** 避免重复生成类型，节省 ~0.3-5ms

### 2. ⚠️ 可优化：属性设置器缓存

**当前实现：**
```csharp
// 每次执行都使用反射
var property = globalsType.GetProperty(kvp.Key);
property.SetValue(instance, kvp.Value);
```

**优化方案：**
```csharp
// 缓存属性设置器委托
private readonly Dictionary<(Type, string), Action<object, object>> _propertySetters = new();

// 使用委托设置属性（比反射快 10-100 倍）
var setter = GetOrCreatePropertySetter(globalsType, kvp.Key);
setter(instance, kvp.Value);
```

**预期性能提升：** 属性设置速度提升 10-100 倍（~0.5ms → ~0.005ms）

### 3. ⚠️ 可优化：实例创建缓存（对象池）

**当前实现：**
```csharp
// 每次执行都创建新实例
var instance = Activator.CreateInstance(globalsType);
```

**优化方案：**
```csharp
// 使用对象池复用实例
var instance = _objectPool.Get(globalsType);
// 使用后归还
_objectPool.Return(instance);
```

**预期性能提升：** 实例创建速度提升 2-5 倍（~0.05ms → ~0.01ms）

**注意：** 需要确保线程安全，且实例状态需要清理

### 4. ⚠️ 可优化：使用 Expression 树生成设置器

**更激进的优化：**
```csharp
// 使用 Expression 树编译为委托
var setter = CreatePropertySetterExpression(property);
// 编译后的委托执行速度接近直接属性访问
```

**预期性能提升：** 属性设置速度提升 100-1000 倍

## 性能测试建议

### 测试场景

1. **首次执行（类型生成）**
   ```csharp
   var variables = new Dictionary<string, object> { { "x", 1 }, { "y", 2 } };
   var sw = Stopwatch.StartNew();
   await ExecuteExpressionAsync("x + y", variables);
   sw.Stop();
   Console.WriteLine($"首次执行: {sw.ElapsedMilliseconds}ms");
   ```

2. **后续执行（缓存命中）**
   ```csharp
   // 使用相同的变量组合
   var sw = Stopwatch.StartNew();
   await ExecuteExpressionAsync("x + y", variables);
   sw.Stop();
   Console.WriteLine($"后续执行: {sw.ElapsedMilliseconds}ms");
   ```

3. **不同变量组合**
   ```csharp
   // 测试不同变量组合的性能
   var variables1 = new Dictionary<string, object> { { "a", 1 } };
   var variables2 = new Dictionary<string, object> { { "b", 2 } };
   // 每个组合首次执行会生成新类型
   ```

### 基准测试

建议使用 BenchmarkDotNet 进行精确测试：

```csharp
[MemoryDiagnoser]
public class DynamicTypeGenerationBenchmark
{
    private readonly RoslynExpressionService _service = new();
    private readonly Dictionary<string, object> _variables = new()
    {
        { "x", 1 },
        { "y", 2 },
        { "z", 3 }
    };

    [Benchmark]
    public async Task ExecuteWithDynamicType()
    {
        await _service.ExecuteExpressionAsync("x + y + z", _variables);
    }
}
```

## 实际性能数据（估算）

基于典型场景（3-5 个变量，简单表达式）：

| 操作 | 首次执行 | 后续执行 | 占比 |
|------|---------|---------|------|
| 类型生成 | 0.3ms | 0.001ms | <1% |
| 实例创建 | 0.05ms | 0.05ms | <1% |
| 属性设置 | 0.03ms | 0.03ms | <1% |
| Roslyn 编译 | 10ms | 10ms | ~90% |
| Roslyn 执行 | 0.5ms | 0.5ms | ~5% |
| **总计** | **~11ms** | **~11ms** | **100%** |

**结论：**
- 动态类型生成的开销很小（<5%）
- 主要性能瓶颈是 Roslyn 编译
- 类型缓存有效避免了重复生成

## 与其他方案对比

| 方案 | 类型生成耗时 | 实例创建耗时 | 属性访问速度 |
|------|------------|------------|------------|
| **Reflection.Emit（当前）** | 0.3-5ms（首次） | 0.05ms | 接近原生（编译后） |
| **ExpandoObject** | 0ms | 0.01ms | 慢（动态绑定） |
| **静态类** | 0ms（编译时） | 0.01ms | 最快（原生） |
| **字典访问** | 0ms | 0ms | 中等（字典查找） |

## 总结

1. **动态类型生成性能良好**
   - 首次生成：~0.3-5ms（取决于变量数量）
   - 后续使用：~0.001ms（缓存命中）

2. **性能瓶颈不在类型生成**
   - Roslyn 编译是主要瓶颈（~90%）
   - 类型生成只占很小一部分（<5%）

3. **优化空间有限**
   - 类型缓存已实现
   - 属性设置器缓存可以进一步优化，但收益有限

4. **建议**
   - 当前实现性能已足够好
   - 如果性能成为问题，优先考虑优化 Roslyn 编译（如脚本缓存）
   - 属性设置器缓存可以作为次要优化点

