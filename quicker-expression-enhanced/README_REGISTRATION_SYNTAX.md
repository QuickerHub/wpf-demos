# ExpressionRunner 注册语法说明

本文档说明 `ExpressionRunner.RunExpression` 方法中 `code` 参数和 `registrationCommands` 参数的使用，以及支持的注册语法。

## 参数说明

### `code` 参数
表达式代码，可以包含：
- **注册命令**：在代码开头使用 `load`、`using`、`type` 命令注册程序集、命名空间和类型
- **C# 表达式代码**：注册命令会被自动解析并移除，剩余部分作为表达式执行
- **变量占位符**：使用 `{变量名}` 格式，会在执行前替换为实际变量值

### `registrationCommands` 参数
独立的注册命令字符串，用于在表达式执行前预先注册程序集、命名空间和类型。支持与 `code` 参数中相同的注册语法。

**执行顺序**：
1. 先执行 `registrationCommands` 中的注册命令
2. 再执行 `code` 中的注册命令
3. 最后执行 `code` 中剩余的表达式代码

## 支持的注册命令

### 1. `load` - 加载程序集

加载指定的程序集到 `EvalContext` 中。

#### 语法
```
load {assembly}
```

#### 参数说明
- `{assembly}`: 程序集名称或文件路径
  - **程序集名称**：如 `System.Core`、`System.Windows.Forms`
  - **文件路径**：如 `C:\Path\To\Assembly.dll` 或 `{packagePath}/MyAssembly.{version}.dll`

#### 示例

```csharp
// 加载系统程序集
load System.Core

// 加载指定路径的程序集
load C:\Path\To\Assembly.dll

// 使用变量替换
load {packagePath}/IntelliTools.Quicker.{version}.dll

// 支持注释前缀
//load System.Windows.Forms

// 支持分号结尾
load System.Core;
```

### 2. `using` - 注册命名空间

注册命名空间，使表达式代码中可以直接使用该命名空间下的类型，无需完整限定名。

#### 语法
```
using {namespace} {assembly}
```

#### 参数说明
- `{namespace}`: 命名空间名称，如 `System.Windows.Forms`
- `{assembly}`: 程序集名称或文件路径（与 `load` 命令相同）

#### 示例

```csharp
// 注册命名空间
using System.Windows.Forms System.Windows.Forms

// 使用变量替换
using {namespace} {assembly}

// 支持注释和分号
//using System System.Core;
```

### 3. `type` - 注册类型

注册指定的类型，使表达式代码中可以直接使用该类型。

#### 语法
```
type {typeName}, {assemblyName}
```

#### 参数说明
- `{typeName}`: 完整的类型名称，如 `System.Windows.Forms.Clipboard`、`QuickerActionManage.ViewRunner`
- `{assemblyName}`: 程序集名称、文件路径或带版本信息的程序集名称
  - **程序集名称**：`System.Windows.Forms`
  - **带版本信息**：`System.Windows.Forms, Version=4.0.0.0`
  - **文件路径**：`C:\Path\To\Assembly.dll` 或 `{packagePath}/MyAssembly.{version}.dll`

#### 示例

```csharp
// 注册系统类型（使用程序集名称）
type System.Windows.Forms.Clipboard, System.Windows.Forms

// 注册类型（带版本信息）
type System.Windows.Forms.Clipboard, System.Windows.Forms, Version=4.0.0.0

// 注册类型（使用文件路径）
type MyNamespace.MyClass, C:\Path\To\MyAssembly.1.0.0.dll

// 使用变量替换
type {className}, {dllpath}
type IntelliTools.Quicker.AssemblyLoader, IntelliTools.Quicker.{version}

// 支持注释和分号
//type System.String, System.Runtime;
```

## 特性说明

### 变量替换

所有注册命令都支持变量替换，使用 `{变量名}` 格式。变量值从 `IActionContext` 中获取。

**示例**：
```csharp
// 假设 context 中有变量：
// packagePath = "C:\Packages"
// version = "1.0.0"
// className = "MyNamespace.MyClass"
// dllpath = "C:\Path\To\Assembly.dll"

load {packagePath}/IntelliTools.Quicker.{version}.dll
// 实际解析为：load C:\Packages/IntelliTools.Quicker.1.0.0.dll

type {className}, {dllpath}
// 实际解析为：type MyNamespace.MyClass, C:\Path\To\Assembly.dll
```

### 注释支持

所有注册命令都支持 `//` 注释前缀，可以用于临时禁用或注释说明。

**示例**：
```csharp
//load System.Core  // 这行会被解析为注册命令
load System.Core    // 这行也会被解析为注册命令
```

### 分号支持

所有注册命令都支持以分号 `;` 结尾，分号会被自动移除。

**示例**：
```csharp
load System.Core;
using System System.Core;
type System.String, System.Runtime;
```

### 文件路径 vs 程序集名称

当指定文件路径时，系统会：
1. **优先从指定文件加载程序集**，确保使用正确的版本
2. 如果文件路径无效，才会尝试从已加载的程序集中查找

当指定程序集名称时，系统会：
1. 先尝试 `Type.GetType` 解析
2. 在已加载的程序集中搜索（按匹配度排序）
3. 最后尝试加载指定的程序集

**重要提示**：使用文件路径可以确保加载指定版本的程序集，避免版本冲突。

## 完整示例

### 示例 1：基本使用

```csharp
string code = @"
load System.Windows.Forms
type System.Windows.Forms.Clipboard, System.Windows.Forms
var text = Clipboard.GetText();
return text;
";

ExpressionRunner.RunExpression(context, eval, code, false);
```

### 示例 2：使用变量替换

```csharp
// 在 context 中设置变量
context.SetVarValue("packagePath", @"C:\Packages\MyApp");
context.SetVarValue("version", "1.0.0");

string code = @"
load {packagePath}/MyAssembly.{version}.dll
type MyNamespace.MyClass, {packagePath}/MyAssembly.{version}.dll
MyClass.DoSomething();
";

ExpressionRunner.RunExpression(context, eval, code, false);
```

### 示例 3：使用 registrationCommands 参数

```csharp
string registrationCommands = @"
load System.Windows.Forms
type System.Windows.Forms.Clipboard, System.Windows.Forms
";

string code = @"
var text = Clipboard.GetText();
return text;
";

ExpressionRunner.RunExpression(context, eval, code, false, false, false, registrationCommands);
```

### 示例 4：混合使用

```csharp
string registrationCommands = @"
load System.Core
";

string code = @"
// 在 code 中也可以添加注册命令
load System.Windows.Forms
type System.Windows.Forms.Clipboard, System.Windows.Forms

// 表达式代码
var text = Clipboard.GetText();
return text;
";

// 执行顺序：
// 1. 执行 registrationCommands 中的 load System.Core
// 2. 执行 code 中的 load System.Windows.Forms 和 type 命令
// 3. 执行 code 中的表达式代码
ExpressionRunner.RunExpression(context, eval, code, false, false, false, registrationCommands);
```

## 注意事项

1. **版本控制**：当需要特定版本的程序集时，建议使用文件路径而不是程序集名称，以确保加载正确的版本。

2. **执行顺序**：`registrationCommands` 中的命令会先于 `code` 中的命令执行。

3. **命令移除**：注册命令会被从 `code` 中移除，不会作为表达式代码执行。

4. **错误处理**：如果程序集加载失败或类型找不到，会抛出异常。建议在调用前确保程序集路径正确。

5. **性能考虑**：程序集加载是相对耗时的操作，建议将常用的注册命令放在 `registrationCommands` 参数中，避免每次执行表达式时重复解析。

6. **变量替换时机**：变量替换在命令解析时进行，确保在调用 `RunExpression` 前，`context` 中已设置好所需的变量。

