# 注册命令语法说明

本文档说明 `ExpressionRunner.RunExpression` 方法支持的注册命令语法。

## 代码示例

```
load {assembly}
using {namespace} {assembly}
type {typeName}, {assembly}
```

## 注册命令

### 1. `load` - 加载程序集

```
load {assembly}
```

**参数**：
- `{assembly}`: 程序集名称或文件路径
  - 程序集名称：`System.Core`、`System.Windows.Forms`
  - 文件路径：`C:\Path\To\Assembly.dll` 或 `{packagePath}/MyAssembly.{version}.dll`

**示例**：
```
load System.Core
load C:\Path\To\Assembly.dll
load {packagePath}/IntelliTools.Quicker.{version}.dll
//load System.Windows.Forms
load System.Core;
```

### 2. `using` - 注册命名空间

```
using {namespace} {assembly}
```

**参数**：
- `{namespace}`: 命名空间名称，如 `System.Windows.Forms`
- `{assembly}`: 程序集名称或文件路径

**示例**：
```
using System.Windows.Forms System.Windows.Forms
using {namespace} {assembly}
//using System System.Core;
```

### 3. `type` - 注册类型

```
type {typeName}, {assemblyName}
```

**参数**：
- `{typeName}`: 完整的类型名称，如 `System.Windows.Forms.Clipboard`、`QuickerActionManage.ViewRunner`
- `{assemblyName}`: 程序集名称、文件路径或带版本信息的程序集名称
  - 程序集名称：`System.Windows.Forms`
  - 带版本信息：`System.Windows.Forms, Version=4.0.0.0`
  - 文件路径：`C:\Path\To\Assembly.dll` 或 `{packagePath}/MyAssembly.{version}.dll`

**示例**：
```
type System.Windows.Forms.Clipboard, System.Windows.Forms
type System.Windows.Forms.Clipboard, System.Windows.Forms, Version=4.0.0.0
type MyNamespace.MyClass, C:\Path\To\MyAssembly.1.0.0.dll
type {className}, {dllpath}
type IntelliTools.Quicker.AssemblyLoader, IntelliTools.Quicker.{version}
//type System.String, System.Runtime;
```

## 特性

### 变量替换

所有注册命令都支持变量替换，使用 `{变量名}` 格式。变量值从 `IActionContext` 中获取。

```
load {packagePath}/IntelliTools.Quicker.{version}.dll
type {className}, {dllpath}
```

### 注释支持

所有注册命令都支持 `//` 注释前缀。

```
//load System.Core
```

### 分号支持

所有注册命令都支持以分号 `;` 结尾。

```
load System.Core;
```


