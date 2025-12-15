# 模板编译设计文档

## 核心思想

将 AST 节点编译为 `Func<IEvaluationContext, string>`，避免运行时遍历 AST 和 switch 匹配的开销。

## 编译策略

### 1. 叶子节点（无依赖）- 直接编译

#### TextNode
- **编译前**：`TextNode("hello")`
- **编译后**：`ctx => "hello"` (常量函数，直接返回字符串)

#### VariableNode
- **编译前**：`VariableNode("name")`
- **编译后**：`ctx => ctx.Name` (直接访问 context 属性)
- **支持的变量**：name, ext, fullname, i, iv, today, now, image, file, size

### 2. 复合节点（有依赖）- 递归编译

#### FormatNode
- **编译策略**：
  1. 先编译 `InnerNode` 得到 `Func<IEvaluationContext, string>`
  2. 根据格式化类型编译格式化逻辑
  3. 组合为：`ctx => Format(CompileInner(ctx), formatString)`

- **特殊情况**：
  - `{2i+1:00}` - 表达式在编译时解析，运行时直接计算
  - `{i:001}` - offset 和 padding 在编译时计算

#### MethodNode
- **编译策略**：
  1. 先编译 `Target` 得到 `Func<IEvaluationContext, string>`
  2. 编译所有 `Arguments` 得到 `Func<IEvaluationContext, object>[]`
  3. 根据方法名编译方法调用逻辑
  4. 组合为：`ctx => Method(CompileTarget(ctx), CompileArgs(ctx))`

- **方法类型**：
  - 无参数方法：`upper`, `lower`, `trim` - 直接内联
  - 有参数方法：`replace`, `sub`, `padleft`, `padright` - 需要编译参数

#### SliceNode
- **编译策略**：
  1. 先编译 `Target` 得到 `Func<IEvaluationContext, string>`
  2. 编译 `Start` 和 `End`（如果是 LiteralNode，编译时提取值）
  3. 组合为：`ctx => Slice(CompileTarget(ctx), start, end)`

### 3. 组合策略

#### TemplateNode
- **编译策略**：
  1. 编译所有子节点得到 `Func<IEvaluationContext, string>[]`
  2. 组合为单个函数，使用 StringBuilder 拼接结果
  3. 最终函数：`ctx => { var sb = new StringBuilder(); foreach (var fn in funcs) sb.Append(fn(ctx)); return sb.ToString(); }`

## 性能优化点

### 编译时优化

1. **常量折叠**
   - `TextNode` 直接内联为字符串常量
   - `LiteralNode` 在编译时提取值

2. **表达式预编译**
   - `{2i+1:00}` 中的表达式在编译时解析为委托
   - 避免运行时使用 DataTable.Compute

3. **格式化逻辑预计算**
   - `FormatNumericIndex` 的 offset 和 padding 在编译时计算
   - 避免运行时字符串解析

4. **方法调用优化**
   - 无参数方法直接内联（如 `upper`, `lower`）
   - 有参数方法预编译参数提取逻辑

### 运行时优化

1. **直接函数调用**
   - 避免 switch 模式匹配
   - 减少方法调用栈深度

2. **减少对象分配**
   - StringBuilder 可以在编译时预分配容量
   - 减少临时对象创建

3. **减少分支判断**
   - 编译时确定分支，运行时直接执行

## 实现步骤

1. 创建 `TemplateCompiler` 类
2. 实现 `CompileNode` 方法，为每种 AST 节点类型编译
3. 实现 `Compile` 方法，编译整个 TemplateNode
4. 在 ViewModel 中使用编译后的函数替代 AST 节点评估

## 示例

### 示例 1: 简单模板
- **模板**：`"prefix_{name}.{ext}"`
- **编译后**：`ctx => "prefix_" + ctx.Name + "." + ctx.Ext`

### 示例 2: 格式化模板
- **模板**：`"{i:001}_{name}.{ext}"`
- **编译后**：`ctx => FormatIndex(ctx.Index + 1, "000") + "_" + ctx.Name + "." + ctx.Ext`

### 示例 3: 方法调用模板
- **模板**：`"{name.upper}.{ext}"`
- **编译后**：`ctx => ctx.Name.ToUpper() + "." + ctx.Ext`

### 示例 4: 复合模板
- **模板**：`"{name[:5].upper}_{i:00}.{ext}"`
- **编译后**：`ctx => { var name = ctx.Name; var sliced = name.Substring(0, Math.Min(5, name.Length)); return sliced.ToUpper() + "_" + FormatIndex(ctx.Index, "00") + "." + ctx.Ext; }`

