# AvalonEdit 设置高亮主题的代码实现调研

## 核心 API 调研

### 1. 获取语法高亮定义中的颜色

AvalonEdit 使用 `IHighlightingDefinition` 接口来表示语法高亮定义，可以通过 `GetNamedColor()` 方法获取命名颜色：

```csharp
using ICSharpCode.AvalonEdit.Highlighting;

// 获取语法高亮定义
IHighlightingDefinition highlighting = textEditor.SyntaxHighlighting;

// 获取命名颜色
HighlightingColor highlightingColor = highlighting.GetNamedColor("Keyword");
```

### 2. 设置颜色 - HighlightingColor.Foreground 属性

`HighlightingColor` 类有一个 `Foreground` 属性，类型为 `HighlightingBrush`。可以通过以下方式设置：

#### 方法一：使用 SimpleHighlightingBrush（推荐）

```csharp
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;

// 设置颜色
highlightingColor.Foreground = new SimpleHighlightingBrush(color);
```

**完整示例：**

```csharp
private void SetColor(IHighlightingDefinition highlighting, string name, Color color)
{
    try
    {
        var highlightingColor = highlighting.GetNamedColor(name);
        if (highlightingColor != null)
        {
            highlightingColor.Foreground = new SimpleHighlightingBrush(color);
        }
    }
    catch
    {
        // 忽略不存在的颜色名称
    }
}
```

#### 方法二：使用 SolidColorBrush（需要转换）

如果 `SimpleHighlightingBrush` 不可用，可以尝试：

```csharp
// 注意：这可能需要类型转换，具体取决于 AvalonEdit 版本
highlightingColor.Foreground = new SimpleHighlightingBrush(new SolidColorBrush(color));
```

### 3. HighlightingBrush 类型层次

AvalonEdit 中的颜色系统：

- `HighlightingBrush` - 抽象基类
  - `SimpleHighlightingBrush` - 简单颜色画刷
  - `FrozenHighlightingBrush` - 冻结的画刷
  - 其他实现

### 4. 完整的主题切换实现

```csharp
using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Wpf.Ui.Appearance;

public static class AvalonEditThemeHelper
{
    /// <summary>
    /// 应用主题到 TextEditor 的语法高亮
    /// </summary>
    public static void ApplyTheme(TextEditor editor, ApplicationTheme themeType)
    {
        if (editor?.SyntaxHighlighting == null)
            return;

        var highlighting = editor.SyntaxHighlighting;
        
        if (themeType == ApplicationTheme.Dark)
        {
            ApplyDarkTheme(highlighting);
        }
        else
        {
            ApplyLightTheme(highlighting);
        }
    }

    private static void ApplyDarkTheme(IHighlightingDefinition highlighting)
    {
        SetColor(highlighting, "Keyword", Color.FromRgb(0x56, 0x9C, 0xD6)); // 浅蓝
        SetColor(highlighting, "String", Color.FromRgb(0xCE, 0x91, 0x78)); // 浅橙
        SetColor(highlighting, "Comment", Color.FromRgb(0x6A, 0x99, 0x55)); // 浅绿
        SetColor(highlighting, "Number", Color.FromRgb(0xB5, 0xCE, 0xA8)); // 浅黄绿
        SetColor(highlighting, "Type", Color.FromRgb(0x4E, 0xC9, 0xB0)); // 青色
        SetColor(highlighting, "Method", Color.FromRgb(0xDC, 0xDC, 0xAA)); // 浅黄
        SetColor(highlighting, "Property", Color.FromRgb(0x9C, 0xDC, 0xFE)); // 浅蓝
    }

    private static void ApplyLightTheme(IHighlightingDefinition highlighting)
    {
        SetColor(highlighting, "Keyword", Color.FromRgb(0x00, 0x00, 0xFF)); // 蓝色
        SetColor(highlighting, "String", Color.FromRgb(0x00, 0x80, 0x00)); // 绿色
        SetColor(highlighting, "Comment", Color.FromRgb(0x00, 0x80, 0x00)); // 绿色
        SetColor(highlighting, "Number", Color.FromRgb(0x09, 0x81, 0xAB)); // 青色
        SetColor(highlighting, "Type", Color.FromRgb(0x2B, 0x91, 0xAF)); // 青色
        SetColor(highlighting, "Method", Color.FromRgb(0x74, 0x53, 0x1F)); // 棕色
        SetColor(highlighting, "Property", Color.FromRgb(0xFF, 0x00, 0x00)); // 红色
    }

    private static void SetColor(IHighlightingDefinition highlighting, string name, Color color)
    {
        try
        {
            var highlightingColor = highlighting.GetNamedColor(name);
            if (highlightingColor != null)
            {
                highlightingColor.Foreground = new SimpleHighlightingBrush(color);
            }
        }
        catch
        {
            // 忽略不存在的颜色名称或设置失败的情况
        }
    }
}
```

## 关键 API 说明

### IHighlightingDefinition.GetNamedColor(string name)

- **功能**：根据名称获取语法高亮定义中的颜色
- **参数**：`name` - 颜色名称（如 "Keyword", "String", "Comment" 等）
- **返回**：`HighlightingColor` 对象，如果不存在则返回 `null`
- **注意**：不同语言的语法高亮定义可能使用不同的颜色名称

### HighlightingColor.Foreground

- **类型**：`HighlightingBrush`
- **功能**：设置或获取该高亮颜色的前景色（文本颜色）
- **设置方式**：`highlightingColor.Foreground = new SimpleHighlightingBrush(color);`
- **注意**：此属性是可读写的，可以在运行时修改

### SimpleHighlightingBrush

- **命名空间**：`ICSharpCode.AvalonEdit.Highlighting`
- **构造函数**：`SimpleHighlightingBrush(Color color)`
- **功能**：创建一个简单的颜色画刷，用于语法高亮

## 颜色名称参考

不同语言的语法高亮定义可能使用不同的颜色名称，常见的有：

- **通用**：
  - `Keyword` - 关键字
  - `String` - 字符串
  - `Comment` - 注释
  - `Number` - 数字
  - `Type` - 类型
  - `Method` - 方法
  - `Property` - 属性
  - `Preprocessor` - 预处理器指令

- **C# 特定**：
  - `UserTypes` - 用户定义类型
  - `UserTypesInterfaces` - 接口
  - `UserTypesDelegates` - 委托
  - `UserTypesEnums` - 枚举

- **XML/HTML**：
  - `XmlAttribute` - XML 属性
  - `XmlAttributeValue` - XML 属性值
  - `XmlTag` - XML 标签

## 注意事项

1. **颜色名称可能不存在**：某些颜色名称可能在某些语言的语法高亮定义中不存在，需要捕获异常或检查 null

2. **Foreground 属性是可写的**：`HighlightingColor.Foreground` 属性可以在运行时修改，这是动态切换主题的基础

3. **SimpleHighlightingBrush 的正确用法**：直接传入 `Color` 对象，不需要 `SolidColorBrush`

4. **性能考虑**：频繁修改颜色可能影响性能，建议在主题变化时统一更新

5. **颜色持久化**：修改后的颜色不会保存到 `.xshd` 文件中，只在运行时有效

## 实际项目中的使用

在 `AvalonEditThemeService.cs` 中的实现：

```csharp
private void SetColor(IHighlightingDefinition highlighting, string name, Color color)
{
    try
    {
        var highlightingColor = highlighting.GetNamedColor(name);
        if (highlightingColor != null)
        {
            highlightingColor.Foreground = new SimpleHighlightingBrush(color);
        }
    }
    catch
    {
        // 忽略不存在的颜色名称或设置失败的情况
    }
}
```

这个实现：
- ✅ 使用 `GetNamedColor()` 获取颜色
- ✅ 检查 null 值
- ✅ 使用 `SimpleHighlightingBrush` 设置颜色
- ✅ 捕获异常处理错误情况

## 参考资料

- [AvalonEdit GitHub 仓库](https://github.com/icsharpcode/AvalonEdit)
- [AvalonEdit Highlighting 文档](https://github.com/icsharpcode/AvalonEdit/wiki/Syntax-Highlighting)
- [AvalonEdit API 文档](https://www.nuget.org/packages/AvalonEdit)

