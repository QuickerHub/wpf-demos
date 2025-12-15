# 模板表达式执行器

## 概述

新的表达式执行器架构采用面向对象设计，将模板系统重构为类似 C# 表达式执行器的实现。

## 核心特性

### 1. 值类型系统
每个值类型封装自己的行为和方法：

- **StringValue** - 字符串值，支持 `upper()`, `lower()`, `trim()`, `replace()`, `sub()`, `padLeft()`, `padRight()`, `slice()`
- **NumberValue** - 数字值
- **IndexValue** - 索引值，支持表达式计算（如 `{2i+1:00}`）和格式化
- **DateValue** - 日期值，支持日期格式化
- **ImageValue** - 图片值
- **FileValue** - 文件值
- **SizeValue** - 文件大小值

### 2. 表达式执行
- 支持基础变量访问
- 支持格式化
- 支持方法调用
- 支持切片操作
- 支持表达式计算

### 3. 兼容性
- ✅ 与旧 `TemplateEvaluator` 完全兼容
- ✅ 所有现有模板都能正常工作
- ✅ 所有测试通过（140 个测试）

## 使用示例

### 基本使用

```csharp
var parser = new TemplateParser(Enumerable.Empty<Type>());
var executor = new TemplateExpressionExecutor();
var templateExecutor = new TemplateNodeExecutor(executor);

// 解析模板
var templateNode = parser.Parse("prefix_{name}.{ext}");

// 创建上下文
var context = new EvaluationContext(
    name: "test",
    ext: "txt",
    fullName: "test.txt",
    fullPath: @"C:\test\test.txt",
    index: 0,
    totalCount: 10
);

// 执行模板
var result = templateExecutor.Execute(templateNode, context);
// 结果: "prefix_test.txt"
```

### 方法调用

```csharp
// {name.upper()}
var templateNode = parser.Parse("{name.upper()}");
var result = templateExecutor.Execute(templateNode, context);
// 结果: "TEST"
```

### 格式化

```csharp
// {i:001}
var templateNode = parser.Parse("{i:001}");
var result = templateExecutor.Execute(templateNode, context);
// 结果: "001"
```

### 表达式计算

```csharp
// {2i+1:00}
var templateNode = parser.Parse("{2i+1:00}");
var result = templateExecutor.Execute(templateNode, context);
// 结果: "01" (当 index=0 时)
```

## 架构优势

1. **面向对象设计**：每个值类型封装自己的行为
2. **易于扩展**：添加新值类型或方法很简单
3. **类型安全**：值类型系统提供类型检查
4. **统一接口**：所有值类型实现 `ITemplateValue`
5. **格式化系统**：每个值类型有自己的格式化逻辑
6. **方法链支持**：为未来的方法链功能做好准备

## 测试结果

- ✅ **总计：140 个测试**
- ✅ **成功：140 个**
- ✅ **失败：0 个**

包括：
- 45 个新表达式执行器测试
- 95 个现有测试（全部通过）

## 文件结构

```
Template/ExpressionEngine/
├── README.md                        # 本文档
├── ARCHITECTURE_DESIGN.md           # 架构设计文档
├── USAGE_EXAMPLES.md                # 使用示例
├── IMPLEMENTATION_STATUS.md         # 实现状态
├── TEST_RESULTS.md                  # 测试结果
├── ITemplateValue.cs                # 值类型接口
├── IExpressionExecutor.cs           # 执行器接口
├── TemplateExpressionExecutor.cs    # 表达式执行器
├── TemplateNodeExecutor.cs          # 模板节点执行器
├── StringValue.cs                   # 字符串值类型
├── NumberValue.cs                   # 数字值类型
├── IndexValue.cs                    # 索引值类型
├── DateValue.cs                     # 日期值类型
├── ImageValue.cs                    # 图片值类型
├── FileValue.cs                     # 文件值类型
└── SizeValue.cs                     # 文件大小值类型
```

## 下一步工作

1. ✅ 基础架构已完成并测试通过
2. 🔲 增强解析器以支持嵌套表达式（如 `{name.replace(name[:1], 'a')}`）
3. 🔲 更新编译器以使用新系统
4. 🔲 集成到 ViewModel
5. 🔲 性能优化和对比

## 相关文档

- [架构设计](ARCHITECTURE_DESIGN.md)
- [使用示例](USAGE_EXAMPLES.md)
- [实现状态](IMPLEMENTATION_STATUS.md)
- [测试结果](TEST_RESULTS.md)

