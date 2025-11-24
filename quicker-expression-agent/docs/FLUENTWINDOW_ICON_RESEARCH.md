# WPF-UI FluentWindow 任务栏图标设置调研

## 问题描述

在使用 WPF-UI 的 `FluentWindow` 时，发现主窗口在任务栏上未显示图标，即使已经配置了 `ApplicationIcon`。

## 调研结果

### 1. ApplicationIcon 配置

在 `.csproj` 文件中设置 `ApplicationIcon` 属性，这会将图标嵌入到 exe 文件中：

```xml
<PropertyGroup>
    <ApplicationIcon Condition="Exists('app-icon.ico')">app-icon.ico</ApplicationIcon>
</PropertyGroup>
```

**注意**：`ApplicationIcon` 只是将图标嵌入到 exe 文件中，但不会自动应用到窗口的任务栏图标。

### 2. FluentWindow 的 Icon 属性

`FluentWindow` 继承自 `Window`，支持标准的 `Icon` 属性。需要在 XAML 或代码中显式设置。

#### 方法 1：在 XAML 中设置（推荐）

```xml
<ui:FluentWindow
    x:Class="YourNamespace.MainWindow"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    Title="Your Application Title"
    Icon="pack://application:,,,/app-icon.ico"
    Width="800" Height="600">
    <!-- 其他内容 -->
</ui:FluentWindow>
```

**资源路径格式**：
- `pack://application:,,,/app-icon.ico` - 根资源
- `pack://application:,,,/QuickerExpressionAgent.Desktop;component/app-icon.ico` - 完整程序集路径

#### 方法 2：在代码中设置

```csharp
public MainWindow()
{
    InitializeComponent();
    
    // 方式 1：使用 BitmapFrame
    try
    {
        var iconUri = new Uri("pack://application:,,,/app-icon.ico", UriKind.Absolute);
        Icon = BitmapFrame.Create(iconUri);
    }
    catch
    {
        // 处理异常
    }
    
    // 方式 2：使用 BitmapImage（需要转换为 ImageSource）
    // Icon = new BitmapImage(new Uri("pack://application:,,,/app-icon.ico"));
}
```

### 3. 资源文件配置

确保图标文件在项目文件中正确配置为资源：

```xml
<ItemGroup>
    <Resource Include="app-icon.ico">
        <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    </Resource>
    <Resource Include="app-icon.png">
        <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    </Resource>
</ItemGroup>
```

### 4. 常见问题

#### 问题 1：图标不显示

**可能原因**：
- 图标文件路径不正确
- 图标文件未正确嵌入为资源
- 图标文件格式不正确（需要 ICO 格式，包含多个尺寸）

**解决方案**：
1. 检查资源路径是否正确
2. 确认图标文件在项目目录中存在
3. 清理并重新构建项目（删除 `bin` 和 `obj` 文件夹）
4. 使用 ImageMagick 生成包含多个尺寸的 ICO 文件

#### 问题 2：普通窗口有图标，但 FluentWindow 没有

**原因**：`FluentWindow` 可能需要显式设置 `Icon` 属性，即使 `ApplicationIcon` 已配置。

**解决方案**：在 XAML 或代码中显式设置 `Icon` 属性。

### 5. 最佳实践

1. **同时配置 ApplicationIcon 和窗口 Icon**：
   - `ApplicationIcon`：用于 exe 文件图标
   - 窗口 `Icon`：用于任务栏图标

2. **使用 ICO 格式**：
   - ICO 文件应包含多个尺寸（16x16, 32x32, 48x48, 64x64, 128x128, 256x256）
   - 使用 ImageMagick 生成：`magick app-icon.svg -resize 256x256 app-icon.ico`

3. **资源路径**：
   - 优先使用 `pack://application:,,,/app-icon.ico` 格式
   - 如果失败，尝试完整程序集路径

4. **代码设置时机**：
   - 在 `InitializeComponent()` 之后设置图标
   - 添加异常处理，避免图标加载失败影响窗口显示

### 6. 当前项目实现

在 `MainWindow.xaml` 中：

```xml
<ui:FluentWindow
    ...
    Icon="pack://application:,,,/app-icon.ico"
    ...>
```

在 `QuickerExpressionAgent.Desktop.csproj` 中：

```xml
<PropertyGroup>
    <ApplicationIcon Condition="Exists('app-icon.ico')">app-icon.ico</ApplicationIcon>
</PropertyGroup>

<ItemGroup>
    <Resource Include="app-icon.ico">
        <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    </Resource>
</ItemGroup>
```

### 7. 参考资源

- [WPF-UI GitHub](https://github.com/lepoco/wpfui)
- [WPF Window.Icon Property](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.icon)
- [Pack URI in WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf)

## 总结

对于 WPF-UI 的 `FluentWindow`，要显示任务栏图标，需要：

1. ✅ 在 `.csproj` 中配置 `ApplicationIcon`（用于 exe 图标）
2. ✅ 在 XAML 中设置 `Icon` 属性（用于任务栏图标）
3. ✅ 确保图标文件正确配置为资源
4. ✅ 使用包含多个尺寸的 ICO 格式图标文件

如果仍然不显示，尝试：
- 清理并重新构建项目
- 检查图标文件是否有效
- 在代码中显式设置图标（带异常处理）

