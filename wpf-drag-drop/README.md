# WPF Drag Drop

WPF 控件库，支持在管理员权限进程下进行文件拖拽操作。

## 功能特性

- 支持管理员权限下的文件拖拽
- 使用 Win32 API 直接处理拖拽消息
- 提供两种使用方式：控件和附加属性
- 自动处理窗口消息过滤，确保在管理员权限下正常工作

## 使用方法

### 方式一：使用 FileDropControl 控件

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:dd="clr-namespace:WpfDragDrop;assembly=WpfDragDrop"
        Title="MainWindow" Height="450" Width="800">
    <dd:FileDropControl FilesDropped="FileDropControl_FilesDropped">
        <Border Background="LightGray" Padding="20">
            <TextBlock Text="拖拽文件到这里" FontSize="16"/>
        </Border>
    </dd:FileDropControl>
</Window>
```

```csharp
private void FileDropControl_FilesDropped(object sender, FileDropRoutedEventArgs e)
{
    MessageBox.Show($"收到了 {e.FileCount} 个文件");
    foreach (var filePath in e.FilePaths)
    {
        // 处理文件路径
        Console.WriteLine(filePath);
    }
}
```

### 方式二：使用附加属性

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:dd="clr-namespace:WpfDragDrop;assembly=WpfDragDrop"
        Title="MainWindow" Height="450" Width="800">
    <Border dd:FileDropBehavior.IsEnabled="True"
            dd:FileDropBehavior.FilesDropped="Border_FilesDropped"
            Background="LightGray" Padding="20">
        <TextBlock Text="拖拽文件到这里" FontSize="16"/>
    </Border>
</Window>
```

```csharp
private void Border_FilesDropped(object sender, FileDropRoutedEventArgs e)
{
    MessageBox.Show($"收到了 {e.FileCount} 个文件");
}
```

### 方式三：直接使用 FileDropHandler

```csharp
public partial class MainWindow : Window
{
    private FileDropHandler? _fileDropHandler;

    public MainWindow()
    {
        InitializeComponent();
        
        // 在窗口加载后初始化
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var label = (Label)FindName("MyLabel");
        _fileDropHandler = new FileDropHandler(label);
        _fileDropHandler.FilesDropped += FileDropHandler_FilesDropped;
    }

    private void FileDropHandler_FilesDropped(object? sender, FileDropEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"收到了 {e.FileCount} 个文件");
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _fileDropHandler?.Dispose();
        base.OnClosed(e);
    }
}
```

## API 说明

### FileDropHandler

核心处理类，负责处理文件拖拽操作。

**构造函数：**
- `FileDropHandler(FrameworkElement containerElement)` - 创建文件拖拽处理器
- `FileDropHandler(FrameworkElement containerElement, bool releaseControl)` - 创建文件拖拽处理器，指定是否在释放时释放控件

**事件：**
- `FilesDropped` - 当文件被拖拽时触发

**属性：**
- `ContainerElement` - 接受文件拖拽的容器元素

### FileDropControl

WPF 控件，封装了 FileDropHandler 的功能。

**属性：**
- `IsFileDropEnabled` - 是否启用文件拖拽（默认：true）

**事件：**
- `FilesDropped` - 当文件被拖拽时触发（路由事件）

### FileDropBehavior

附加属性，可以在任何 FrameworkElement 上启用文件拖拽。

**附加属性：**
- `IsEnabled` - 是否启用文件拖拽

**附加事件：**
- `FilesDropped` - 当文件被拖拽时触发

## 技术实现

- 使用 `ChangeWindowMessageFilterEx` 允许管理员权限下的窗口消息
- 使用 `DragAcceptFiles` 接受文件拖拽
- 使用 `DragQueryFile` 获取文件路径列表
- 使用 `HwndSource` 和消息钩子拦截 `WM_DROPFILES` 消息

## 注意事项

1. 此控件专门设计用于在管理员权限下工作的进程
2. 需要确保窗口句柄已创建（通常在 Loaded 事件后）
3. 使用完毕后应调用 `Dispose()` 释放资源

