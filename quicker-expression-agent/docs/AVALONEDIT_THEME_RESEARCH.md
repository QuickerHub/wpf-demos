# AvalonEdit 黑白主题适配调研

## 问题描述

AvalonEdit 的语法高亮颜色是预定义的，默认使用亮色主题的颜色方案。在黑色主题下，这些颜色可能不可见或对比度不足，需要适配黑白主题。

## 现成方案调研

### 1. AvalonEdit 内置主题支持

**调研结果：** AvalonEdit **没有内置的主题支持**。语法高亮定义（`.xshd` 文件）中的颜色是硬编码的，不支持动态主题切换。

### 2. 现成的主题库或 NuGet 包

**调研结果：** 
- **未找到**专门为 AvalonEdit 提供主题支持的 NuGet 包
- **未找到**现成的暗色/亮色主题定义文件库
- AvalonEdit 社区主要使用自定义实现方案

### 3. 开源项目参考

**调研结果：**
- **RoslynPad**：使用 AvalonEdit，但需要查看其实现方式
- **大多数项目**：采用自定义代码动态修改语法高亮颜色
- **常见做法**：在代码中根据主题动态设置 `IHighlightingDefinition` 的颜色

### 4. 可用的现成资源

**调研结果：**
- **VS Code 主题颜色方案**：可以参考 VS Code 的 Dark+ 和 Light+ 主题颜色值
- **AvalonEdit 内置语法定义**：可以通过 `HighlightingManager.Instance.GetDefinition()` 获取，但颜色是固定的
- **.xshd 文件模板**：可以从 AvalonEdit 源码或示例项目中获取，但需要手动修改颜色

### 5. 推荐方案

基于调研结果，**推荐使用方案一（动态修改语法高亮定义的颜色）**，因为：
- 不需要维护多个 .xshd 文件
- 可以动态切换主题
- 代码集中管理，易于维护
- 没有现成的主题库可用，自定义实现是最佳选择

## 解决方案

### 方案一：动态修改语法高亮定义的颜色（推荐）

AvalonEdit 的语法高亮定义（`IHighlightingDefinition`）中的颜色可以在运行时动态修改。这是最灵活的方案。

#### 实现步骤

1. **创建主题适配辅助类**

```csharp
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using Wpf.Ui.Appearance;

public static class AvalonEditThemeHelper
{
    /// <summary>
    /// 应用主题到 TextEditor 的语法高亮
    /// </summary>
    public static void ApplyTheme(TextEditor editor, ThemeType themeType)
    {
        if (editor.SyntaxHighlighting == null)
            return;

        var highlighting = editor.SyntaxHighlighting;
        
        // 根据主题设置颜色
        if (themeType == ThemeType.Dark)
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
        // 关键字 - 亮蓝色
        SetColor(highlighting, "Keyword", Colors.LightBlue);
        
        // 字符串 - 浅绿色
        SetColor(highlighting, "String", Colors.LightGreen);
        
        // 注释 - 灰色
        SetColor(highlighting, "Comment", Colors.Gray);
        
        // 数字 - 浅黄色
        SetColor(highlighting, "Number", Colors.LightYellow);
        
        // 类型 - 浅青色
        SetColor(highlighting, "Type", Colors.LightCyan);
        
        // 方法名 - 黄色
        SetColor(highlighting, "Method", Colors.Yellow);
        
        // 属性 - 浅紫色
        SetColor(highlighting, "Property", Colors.LightPink);
    }

    private static void ApplyLightTheme(IHighlightingDefinition highlighting)
    {
        // 关键字 - 深蓝色
        SetColor(highlighting, "Keyword", Colors.Blue);
        
        // 字符串 - 深绿色
        SetColor(highlighting, "String", Colors.DarkGreen);
        
        // 注释 - 深灰色
        SetColor(highlighting, "Comment", Colors.Gray);
        
        // 数字 - 深红色
        SetColor(highlighting, "Number", Colors.DarkRed);
        
        // 类型 - 深青色
        SetColor(highlighting, "Type", Colors.Teal);
        
        // 方法名 - 深橙色
        SetColor(highlighting, "Method", Colors.DarkOrange);
        
        // 属性 - 深紫色
        SetColor(highlighting, "Property", Colors.Purple);
    }

    private static void SetColor(IHighlightingDefinition highlighting, string name, Color color)
    {
        var highlightingColor = highlighting.GetNamedColor(name);
        if (highlightingColor != null)
        {
            highlightingColor.Foreground = new SimpleHighlightingBrush(color);
        }
    }
}
```

2. **在 TextEditor 加载时应用主题**

```csharp
// 在 App.xaml.cs 或 TextEditor 的 Loaded 事件中
private void SetupAvalonEditTheme(TextEditor editor)
{
    // 获取当前主题
    var currentTheme = Theme.GetAppTheme();
    
    // 应用主题
    AvalonEditThemeHelper.ApplyTheme(editor, currentTheme);
    
    // 监听主题变化
    Theme.Changed += (themeType, systemAccent) =>
    {
        AvalonEditThemeHelper.ApplyTheme(editor, themeType);
    };
}
```

#### 优点
- 不需要创建多个 .xshd 文件
- 可以动态切换主题
- 代码集中管理，易于维护

#### 缺点
- 需要了解 AvalonEdit 的颜色命名规则
- 某些颜色名称可能因语言而异

### 方案二：使用自定义 .xshd 文件

为每个主题创建独立的语法高亮定义文件。

#### 实现步骤

1. **创建暗色主题的 .xshd 文件**（`CSharp-Dark.xshd`）

```xml
<?xml version="1.0"?>
<SyntaxDefinition name="C# Dark" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
  <Color name="Keyword" foreground="#569CD6" fontWeight="bold" />
  <Color name="String" foreground="#CE9178" />
  <Color name="Comment" foreground="#6A9955" />
  <Color name="Number" foreground="#B5CEA8" />
  <Color name="Type" foreground="#4EC9B0" />
  <Color name="Method" foreground="#DCDCAA" />
  <Color name="Property" foreground="#9CDCFE" />
  <!-- 其他颜色定义 -->
</SyntaxDefinition>
```

2. **创建亮色主题的 .xshd 文件**（`CSharp-Light.xshd`）

```xml
<?xml version="1.0"?>
<SyntaxDefinition name="C# Light" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
  <Color name="Keyword" foreground="#0000FF" fontWeight="bold" />
  <Color name="String" foreground="#008000" />
  <Color name="Comment" foreground="#008000" />
  <Color name="Number" foreground="#FF0000" />
  <Color name="Type" foreground="#2B91AF" />
  <Color name="Method" foreground="#74531F" />
  <Color name="Property" foreground="#FF0000" />
  <!-- 其他颜色定义 -->
</SyntaxDefinition>
```

3. **动态加载语法高亮定义**

```csharp
using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

public static void LoadSyntaxHighlighting(TextEditor editor, ThemeType themeType)
{
    string xshdPath = themeType == ThemeType.Dark 
        ? "Resources/CSharp-Dark.xshd" 
        : "Resources/CSharp-Light.xshd";
    
    using (var stream = File.OpenRead(xshdPath))
    using (var reader = new XmlTextReader(stream))
    {
        editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
```

#### 优点
- 颜色定义清晰，易于维护
- 可以为不同语言创建不同的主题文件
- 支持完整的语法高亮规则定义

#### 缺点
- 需要维护多个文件
- 文件较大，可能影响加载性能
- 需要完整复制语法规则定义

### 方案三：使用 WPF UI 主题资源颜色

将语法高亮颜色映射到 WPF UI 的主题资源，实现自动适配。

#### 实现步骤

```csharp
public static void ApplyThemeFromResources(TextEditor editor)
{
    if (editor.SyntaxHighlighting == null)
        return;

    var highlighting = editor.SyntaxHighlighting;
    var resources = Application.Current.Resources;
    
    // 从 WPF UI 主题资源获取颜色
    var primaryBrush = resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
    var accentBrush = resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush;
    var secondaryBrush = resources["TextFillColorSecondaryBrush"] as SolidColorBrush;
    
    // 应用颜色
    if (primaryBrush != null)
        SetColor(highlighting, "Keyword", primaryBrush.Color);
    
    if (accentBrush != null)
        SetColor(highlighting, "String", accentBrush.Color);
    
    if (secondaryBrush != null)
        SetColor(highlighting, "Comment", secondaryBrush.Color);
}
```

#### 优点
- 与 WPF UI 主题系统集成
- 自动跟随主题变化
- 颜色风格统一

#### 缺点
- 颜色选择可能不够丰富
- 需要手动映射颜色名称

### 方案四：修改 TextArea 的默认颜色

虽然不能完全解决语法高亮问题，但可以确保默认文本颜色正确。

```csharp
// 在 TextEditor 的 Loaded 事件中
editor.TextArea.DefaultTextBrush = new SolidColorBrush(
    Theme.GetAppTheme() == ThemeType.Dark ? Colors.White : Colors.Black
);
```

## 推荐实现方案

### 综合方案：方案一 + 方案四

1. **使用动态修改语法高亮定义**（方案一）处理语法高亮颜色
2. **设置 TextArea 默认文本颜色**（方案四）确保未高亮的文本正确显示
3. **监听主题变化**，自动切换颜色

### 完整实现示例

```csharp
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Wpf.Ui.Appearance;

public static class AvalonEditThemeManager
{
    /// <summary>
    /// 为 TextEditor 设置主题适配
    /// </summary>
    public static void SetupTheme(TextEditor editor)
    {
        if (editor == null) return;
        
        // 应用初始主题
        ApplyTheme(editor);
        
        // 监听主题变化
        Theme.Changed += (themeType, systemAccent) =>
        {
            ApplyTheme(editor);
        };
    }
    
    private static void ApplyTheme(TextEditor editor)
    {
        var themeType = Theme.GetAppTheme();
        var resources = Application.Current.Resources;
        
        // 设置默认文本颜色
        var textBrush = resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
        if (textBrush != null)
        {
            editor.TextArea.DefaultTextBrush = textBrush;
        }
        
        // 应用语法高亮颜色
        if (editor.SyntaxHighlighting != null)
        {
            ApplySyntaxHighlightingTheme(editor.SyntaxHighlighting, themeType, resources);
        }
    }
    
    private static void ApplySyntaxHighlightingTheme(
        IHighlightingDefinition highlighting, 
        ThemeType themeType, 
        ResourceDictionary resources)
    {
        if (themeType == ThemeType.Dark)
        {
            // 暗色主题颜色方案（参考 VS Code Dark+）
            SetColor(highlighting, "Keyword", Color.FromRgb(0x56, 0x9C, 0xD6)); // 浅蓝
            SetColor(highlighting, "String", Color.FromRgb(0xCE, 0x91, 0x78)); // 浅橙
            SetColor(highlighting, "Comment", Color.FromRgb(0x6A, 0x99, 0x55)); // 浅绿
            SetColor(highlighting, "Number", Color.FromRgb(0xB5, 0xCE, 0xA8)); // 浅黄绿
            SetColor(highlighting, "Type", Color.FromRgb(0x4E, 0xC9, 0xB0)); // 青色
            SetColor(highlighting, "Method", Color.FromRgb(0xDC, 0xDC, 0xAA)); // 浅黄
            SetColor(highlighting, "Property", Color.FromRgb(0x9C, 0xDC, 0xFE)); // 浅蓝
        }
        else
        {
            // 亮色主题颜色方案（参考 VS Code Light+）
            SetColor(highlighting, "Keyword", Color.FromRgb(0x00, 0x00, 0xFF)); // 蓝色
            SetColor(highlighting, "String", Color.FromRgb(0x00, 0x80, 0x00)); // 绿色
            SetColor(highlighting, "Comment", Color.FromRgb(0x00, 0x80, 0x00)); // 绿色
            SetColor(highlighting, "Number", Color.FromRgb(0x00, 0x00, 0x00)); // 黑色
            SetColor(highlighting, "Type", Color.FromRgb(0x2B, 0x91, 0xAF)); // 青色
            SetColor(highlighting, "Method", Color.FromRgb(0x74, 0x53, 0x1F)); // 棕色
            SetColor(highlighting, "Property", Color.FromRgb(0xFF, 0x00, 0x00)); // 红色
        }
    }
    
    private static void SetColor(IHighlightingDefinition highlighting, string name, Color color)
    {
        var highlightingColor = highlighting.GetNamedColor(name);
        if (highlightingColor != null)
        {
            highlightingColor.Foreground = new SimpleHighlightingBrush(color);
        }
    }
}
```

### 使用方式

```csharp
// 在 TextEditor 的 Loaded 事件中
textEditor.Loaded += (s, e) =>
{
    AvalonEditThemeManager.SetupTheme(textEditor);
};
```

或者在 App.xaml.cs 中全局设置：

```csharp
EventManager.RegisterClassHandler(
    typeof(TextEditor),
    FrameworkElement.LoadedEvent,
    new RoutedEventHandler((sender, args) =>
    {
        if (sender is TextEditor editor)
        {
            AvalonEditThemeManager.SetupTheme(editor);
        }
    }));
```

## 注意事项

1. **颜色名称可能因语言而异**：不同语言的语法高亮定义可能使用不同的颜色名称，需要根据实际情况调整。

2. **性能考虑**：频繁切换主题可能影响性能，建议在主题变化时统一更新所有 TextEditor。

3. **颜色对比度**：确保选择的颜色在对应主题下有足够的对比度，保证可读性。

4. **测试覆盖**：需要测试所有使用的语法高亮语言（C#、XML、JSON 等）在不同主题下的显示效果。

## 现成实现方案总结

### 方案对比

| 方案 | 是否有现成实现 | 推荐度 | 说明 |
|------|--------------|--------|------|
| 动态修改颜色 | ❌ 无现成库 | ⭐⭐⭐⭐⭐ | 需要自己实现，但最灵活 |
| 自定义 .xshd 文件 | ❌ 无现成文件 | ⭐⭐⭐ | 需要手动创建和维护 |
| 使用 WPF UI 资源 | ❌ 无现成实现 | ⭐⭐⭐⭐ | 需要自己实现，但能与主题系统集成 |

### 结论

**没有现成的 AvalonEdit 主题实现方案**，需要自己实现。推荐使用**动态修改语法高亮定义的颜色**方案，这是最灵活且易于维护的方式。

### 实现建议

1. **使用方案一（动态修改颜色）**作为主要实现方式
2. **参考 VS Code 主题颜色**：使用 VS Code Dark+ 和 Light+ 的颜色值
3. **监听主题变化**：在 WPF UI 主题变化时自动更新所有 TextEditor 的颜色
4. **统一管理**：创建一个全局的 `AvalonEditThemeManager` 类来管理所有 TextEditor 的主题

## 参考资料

- [AvalonEdit 官方文档](https://github.com/icsharpcode/AvalonEdit)
- [AvalonEdit Highlighting 文档](https://github.com/icsharpcode/AvalonEdit/wiki/Syntax-Highlighting)
- [VS Code 主题颜色参考](https://code.visualstudio.com/api/references/theme-color)
- [AvalonEdit GitHub 仓库](https://github.com/icsharpcode/AvalonEdit)
- [RoslynPad 项目](https://github.com/roslynpad/roslynpad) - 使用 AvalonEdit 的参考项目

