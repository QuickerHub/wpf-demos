# WPF C# 代码编辑器调研

## 1. AvalonEdit ScrollViewer 问题

### 问题描述
AvalonEdit 内部包含自己的 `ScrollViewer`，当嵌套在外部滚动容器（如消息列表）中时，会拦截鼠标滚轮事件，导致外部滚动无法正常工作。

### 解决方案

#### 方案一：移除 AvalonEdit 内置 ScrollViewer（推荐）

通过自定义 ControlTemplate，移除 AvalonEdit 的默认 ScrollViewer，只保留 TextArea：

```xml
<Style TargetType="{x:Type avalonedit:TextEditor}">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type avalonedit:TextEditor}">
                <ContentPresenter 
                    Focusable="False"
                    Content="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TextArea}" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

**优点：**
- 彻底解决滚动冲突
- 让外部 ScrollViewer 完全控制滚动
- 性能更好（少一层 ScrollViewer）

**缺点：**
- 需要自定义样式
- 失去 AvalonEdit 内置的滚动功能（但可以通过外部 ScrollViewer 实现）

#### 方案二：PreviewMouseWheel 事件处理

在 PreviewMouseWheel 事件中设置 `e.Handled = false`，让事件继续向上冒泡：

```csharp
textEditor.PreviewMouseWheel += (sender, e) =>
{
    e.Handled = false; // 允许事件继续传播
};
```

**优点：**
- 实现简单
- 不需要修改模板

**缺点：**
- 可能不够彻底，某些情况下仍可能被拦截

## 2. 其他编辑器方案调研

### 2.1 RoslynPad
- **使用的编辑器：** AvalonEdit
- **说明：** RoslynPad 也是基于 AvalonEdit 实现的，说明 AvalonEdit 是 WPF 中 C# 编辑器的标准选择

### 2.2 Monaco Editor（VS Code 编辑器）
- **类型：** Web 技术（基于 TypeScript）
- **集成方式：** 通过 WebView2 嵌入 WPF
- **优点：**
  - 功能强大（VS Code 的完整编辑器功能）
  - 语法高亮、自动完成、代码折叠等
  - 性能优秀
- **缺点：**
  - 需要 WebView2 运行时
  - 集成复杂度较高
  - 资源占用较大

### 2.3 ScintillaNET
- **类型：** .NET 包装的 Scintilla 编辑器
- **优点：**
  - 功能丰富
  - 性能好
  - 支持多种语言
- **缺点：**
  - 需要原生 DLL
  - 在 WPF 中集成不如 AvalonEdit 自然
  - 维护不如 AvalonEdit 活跃

### 2.4 简单 TextBox + 语法高亮库
- **方案：** 使用普通的 TextBox 配合语法高亮库（如 FastColoredTextBox）
- **优点：**
  - 轻量级
  - 无滚动冲突问题
- **缺点：**
  - 功能有限
  - 性能可能不如专业编辑器

## 3. 推荐方案

### 对于当前项目（表达式编辑器）

**推荐：继续使用 AvalonEdit + 移除内置 ScrollViewer**

**理由：**
1. AvalonEdit 是 WPF 中 C# 编辑器的标准选择
2. 功能足够（语法高亮、撤销/重做等）
3. 移除 ScrollViewer 后可以完美解决滚动冲突
4. 集成简单，维护成本低

**实现步骤：**

1. 在 App.xaml 或控件资源中定义样式：

```xml
<Style TargetType="{x:Type avalonedit:TextEditor}" x:Key="NoScrollTextEditor">
    <Setter Property="Foreground" Value="{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}" />
    <Setter Property="Background" Value="{DynamicResource {x:Static SystemColors.WindowBrushKey}}" />
    <Setter Property="FlowDirection" Value="LeftToRight"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type avalonedit:TextEditor}">
                <ContentPresenter 
                    Focusable="False"
                    Content="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TextArea}" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

2. 在 SyntaxHighlightedTextBox.xaml 中应用样式：

```xml
<avalonedit:TextEditor
    x:Name="TextEditor"
    Style="{StaticResource NoScrollTextEditor}"
    ... />
```

### 如果 AvalonEdit 确实不满足需求

**备选方案：Monaco Editor（WebView2）**

如果未来需要更强大的编辑器功能（如智能提示、代码折叠、多光标等），可以考虑使用 Monaco Editor。

**集成示例：**
```csharp
// 需要安装 Microsoft.Web.WebView2 NuGet 包
<wv2:WebView2 
    x:Name="MonacoEditor"
    Source="monaco-editor.html" />
```

## 4. 参考资料

- [AvalonEdit 移除自身 ScrollViewer](https://www.cnblogs.com/twzy/p/5586160.html)
- [RoslynPad GitHub](https://github.com/roslynpad/roslynpad)
- [Monaco Editor](https://microsoft.github.io/monaco-editor/)
- [ScintillaNET](https://github.com/jacobslusser/ScintillaNET)

