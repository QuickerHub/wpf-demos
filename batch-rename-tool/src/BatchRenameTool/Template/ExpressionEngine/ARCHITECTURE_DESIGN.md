# 模板表达式执行器架构设计

## 核心思想

将模板系统重构为面向对象的表达式执行器，类似于 C# 表达式执行器，支持：
- 值类型系统（StringValue, NumberValue, DateValue 等）
- 每个值类型有自己的方法和格式化功能
- 表达式树执行
- 方法链式调用
- 嵌套表达式支持

## 架构设计

### 1. 值类型系统 (Value Types)

#### ITemplateValue 接口
```csharp
public interface ITemplateValue
{
    string ToString(string? format = null);
    ITemplateValue InvokeMethod(string methodName, IReadOnlyList<ITemplateValue> arguments);
    bool HasMethod(string methodName);
}
```

#### 具体值类型

**StringValue**
- 表示字符串值
- 方法：`upper()`, `lower()`, `trim()`, `replace(old, new)`, `sub(start, end)`, `padLeft(width, char)`, `padRight(width, char)`, `slice(start, end)`
- 格式化：支持字符串格式化

**NumberValue**
- 表示数字值
- 方法：`format(formatString)` - 数字格式化
- 格式化：支持数字格式化（如 `{i:000}`, `{i:001}`）

**DateValue**
- 表示日期值
- 方法：`format(formatString)` - 日期格式化
- 格式化：支持日期格式化（如 `{today:yyyyMMdd}`）

**IndexValue**
- 表示索引值（特殊数字）
- 继承 NumberValue，支持表达式计算（如 `{2i+1:00}`）

### 2. 表达式树增强 (Enhanced AST)

#### 现有 AST 节点保持不变
- `TextNode` - 文本节点
- `VariableNode` - 变量节点
- `FormatNode` - 格式化节点
- `MethodNode` - 方法调用节点
- `SliceNode` - 切片节点
- `LiteralNode` - 字面量节点

#### 新增节点类型
- `ExpressionNode` - 表达式节点（支持复杂表达式）
- `BinaryOperatorNode` - 二元运算符节点（+, -, *, /）
- `UnaryOperatorNode` - 一元运算符节点

### 3. 执行器系统 (Executor System)

#### IExpressionExecutor 接口
```csharp
public interface IExpressionExecutor
{
    ITemplateValue Execute(AstNode node, IEvaluationContext context);
}
```

#### TemplateExpressionExecutor
- 实现表达式树执行
- 将 AST 节点转换为 ITemplateValue
- 支持方法调用和方法链
- 支持嵌套表达式

### 4. 方法系统 (Method System)

#### 方法注册
- 每个值类型注册自己的方法
- 方法可以是实例方法或扩展方法
- 支持方法重载

#### 方法调用流程
1. 执行目标表达式，得到 ITemplateValue
2. 在值类型上查找方法
3. 执行参数表达式
4. 调用方法
5. 返回新的 ITemplateValue

### 5. 格式化系统 (Format System)

#### IFormatter 接口
```csharp
public interface IFormatter
{
    bool CanFormat(ITemplateValue value, string format);
    string Format(ITemplateValue value, string format);
}
```

#### 具体格式化器
- `StringFormatter` - 字符串格式化
- `NumberFormatter` - 数字格式化（包括中文数字）
- `DateFormatter` - 日期格式化
- `IndexFormatter` - 索引格式化（支持表达式）

## 实现步骤

### Phase 1: 值类型系统
1. 定义 `ITemplateValue` 接口
2. 实现 `StringValue`, `NumberValue`, `DateValue`, `IndexValue`
3. 为每个值类型实现方法

### Phase 2: 执行器系统
1. 定义 `IExpressionExecutor` 接口
2. 实现 `TemplateExpressionExecutor`
3. 实现 AST 节点到值的转换

### Phase 3: 格式化系统
1. 定义 `IFormatter` 接口
2. 实现各种格式化器
3. 集成到值类型系统

### Phase 4: 编译器优化
1. 更新编译器以使用新的值类型系统
2. 优化方法调用编译
3. 优化格式化编译

## 示例

### 示例 1: 简单表达式
```
模板: "prefix_{name}.{ext}"
执行:
  - TextNode("prefix_") -> StringValue("prefix_")
  - VariableNode("name") -> StringValue("test")
  - TextNode(".") -> StringValue(".")
  - VariableNode("ext") -> StringValue("txt")
结果: "prefix_test.txt"
```

### 示例 2: 方法调用
```
模板: "{name.upper()}"
执行:
  - VariableNode("name") -> StringValue("test")
  - MethodNode("upper") -> StringValue("test").InvokeMethod("upper", []) -> StringValue("TEST")
结果: "TEST"
```

### 示例 3: 嵌套表达式
```
模板: "{name.replace(name[:1], 'a')}"
执行:
  - VariableNode("name") -> StringValue("test")
  - SliceNode(name, 0, 1) -> StringValue("t")
  - LiteralNode("a") -> StringValue("a")
  - MethodNode("replace", [StringValue("t"), StringValue("a")])
    -> StringValue("test").InvokeMethod("replace", [StringValue("t"), StringValue("a")])
    -> StringValue("aest")
结果: "aest"
```

### 示例 4: 格式化
```
模板: "{i:001}"
执行:
  - VariableNode("i") -> IndexValue(0)
  - FormatNode(IndexValue(0), "001")
    -> IndexValue(0).ToString("001")
    -> NumberFormatter.Format(0, "001")
    -> "001"
结果: "001"
```

## 优势

1. **面向对象设计**：每个值类型封装自己的行为
2. **易于扩展**：添加新的值类型或方法很容易
3. **类型安全**：值类型系统提供类型检查
4. **方法链支持**：`{name.upper().replace('E','e')}`
5. **嵌套表达式支持**：`{name.replace(name[:1], 'a')}`
6. **统一的格式化系统**：所有值类型使用统一的格式化接口

