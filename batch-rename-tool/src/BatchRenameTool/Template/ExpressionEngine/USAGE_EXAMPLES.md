# 表达式执行器使用示例

## 基本使用

### 1. 创建执行器

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

## 值类型系统

### StringValue - 字符串值

```csharp
var str = new StringValue("hello");
str.ToString(); // "hello"
str.InvokeMethod("upper", []); // StringValue("HELLO")
str.InvokeMethod("replace", [new StringValue("e"), new StringValue("E")]); // StringValue("hEllo")
```

### IndexValue - 索引值（支持表达式）

```csharp
var index = new IndexValue(0, 10);
index.ToString("001"); // "001"
index.EvaluateExpression("2i+1", "00"); // StringValue("01")
```

### DateValue - 日期值

```csharp
var date = new DateValue(DateTime.Today);
date.ToString("yyyyMMdd"); // "20241213"
date.ToString(); // "2024-12-13"
```

## 复杂表达式示例

### 示例 1: 方法链

```
模板: "{name.upper().replace('E','e')}"
执行流程:
  1. VariableNode("name") -> StringValue("test")
  2. MethodNode("upper") -> StringValue("TEST")
  3. MethodNode("replace", ["E", "e"]) -> StringValue("TeST")
结果: "TeST"
```

### 示例 2: 嵌套表达式（需要解析器支持）

```
模板: "{name.replace(name[:1], 'a')}"
执行流程:
  1. VariableNode("name") -> StringValue("test")
  2. SliceNode(name, 0, 1) -> StringValue("t")
  3. LiteralNode("a") -> StringValue("a")
  4. MethodNode("replace", [StringValue("t"), StringValue("a")])
     -> StringValue("test").InvokeMethod("replace", [...])
     -> StringValue("aest")
结果: "aest"
```

### 示例 3: 格式化

```
模板: "{i:001}"
执行流程:
  1. VariableNode("i") -> IndexValue(0, 10)
  2. FormatNode(IndexValue(0, 10), "001")
     -> IndexValue(0, 10).ToString("001")
     -> StringValue("001")
结果: "001"
```

## 扩展性

### 添加新的值类型

```csharp
public class CustomValue : ITemplateValue
{
    private readonly object _value;

    public CustomValue(object value)
    {
        _value = value;
    }

    public object? GetValue() => _value;

    public string ToString(string? format = null)
    {
        // 实现格式化逻辑
        return _value.ToString();
    }

    public bool HasMethod(string methodName)
    {
        // 返回是否支持该方法
        return methodName == "customMethod";
    }

    public ITemplateValue InvokeMethod(string methodName, IReadOnlyList<ITemplateValue> arguments)
    {
        // 实现方法调用逻辑
        if (methodName == "customMethod")
        {
            return new StringValue("custom result");
        }
        throw new NotSupportedException();
    }
}
```

### 在执行器中注册新值类型

在 `TemplateExpressionExecutor.ExecuteVariable` 中添加：

```csharp
"custom" => new CustomValue(context.CustomProperty),
```

## 与现有系统的集成

### 替换 TemplateEvaluator

```csharp
// 旧方式
var evaluator = new TemplateEvaluator();
var result = evaluator.Evaluate(templateNode, context);

// 新方式
var executor = new TemplateExpressionExecutor();
var templateExecutor = new TemplateNodeExecutor(executor);
var result = templateExecutor.Execute(templateNode, context);
```

### 编译器集成

编译器可以编译为使用值类型的函数：

```csharp
// 编译后的函数
Func<IEvaluationContext, string> compiled = ctx =>
{
    var executor = new TemplateExpressionExecutor();
    var templateExecutor = new TemplateNodeExecutor(executor);
    return templateExecutor.Execute(templateNode, ctx);
};
```

## 优势

1. **类型安全**：每个值类型封装自己的行为
2. **易于扩展**：添加新值类型或方法很简单
3. **方法链支持**：`{name.upper().replace('E','e')}`
4. **嵌套表达式支持**：`{name.replace(name[:1], 'a')}`
5. **统一的接口**：所有值类型实现 `ITemplateValue`
6. **格式化系统**：每个值类型有自己的格式化逻辑

