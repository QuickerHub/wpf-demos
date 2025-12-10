# Action Path Convert

播放列表路径转换工具 - 将播放列表中的文件路径转换为目标目录中的实际路径

## 功能说明

### 核心功能

本工具用于将播放列表（如 M3U、M3U8）中的文件路径替换为目标目录中的实际文件路径，支持以下功能：

1. **路径匹配**
   - 在目标目录中递归搜索匹配的音频文件
   - 通过文件名（不含扩展名）进行匹配，忽略扩展名差异
   - 支持自定义音频文件扩展名筛选（如 `*.mp3,*.flac,*.mp4`）

2. **路径转换**
   - 将匹配到的文件路径转换为相对路径（可选）
   - 支持指定路径前缀移除，生成相对路径

3. **格式优先级**
   - 当输入文件列表中有多个同名文件（不同扩展名）时，优先选择指定格式的文件
   - 如果目标目录中未找到匹配文件，从输入文件中选择优先格式的文件

4. **结果输出**
   - 输出成功匹配的文件路径列表（可用于生成新的播放列表）
   - 输出未找到的文件列表（用于提示用户）

### 使用场景

- **播放列表迁移**：将旧播放列表中的路径替换为新目录中的路径
- **路径标准化**：将绝对路径转换为相对路径，便于播放列表在不同环境使用
- **格式统一**：支持多种音频格式，可指定优先格式

## 项目结构

```
action-path-convert/
├── src/
│   └── ActionPathConvert/
│       ├── ActionPathConvert.csproj
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── ViewModels/
│       │   └── MainWindowViewModel.cs
│       ├── Services/
│       │   └── PathConvertService.cs  (核心处理逻辑)
│       ├── Models/
│       │   └── PathConvertResult.cs   (处理结果模型)
│       └── Runner.cs                  (Quicker 接口)
├── ActionPathConvert.sln
├── build.yaml
├── build.ps1
├── version.json
└── README.md
```

## 开发规范

本项目遵循 WPF MVVM 开发规范，使用：
- `CommunityToolkit.Mvvm` - MVVM 特性
- `DependencyPropertyGenerator` - DependencyProperty 源生成器（如需要）

## 核心处理逻辑

### PathConvertService

核心处理服务，实现以下功能：

```csharp
public class PathConvertResult
{
    public List<string> OutputFiles { get; set; }      // 成功匹配的文件路径列表
    public List<string> NotFoundFiles { get; set; }     // 未找到的文件列表
}

public class PathConvertService
{
    public PathConvertResult ProcessFilePaths(
        List<string> inputFiles,           // 输入文件路径列表
        string searchDirectory,              // 目标搜索目录
        string audioExtensions,              // 音频扩展名（如 "*.mp3,*.flac,*.mp4"）
        string preferredExtension,           // 优先选择的扩展名（如 ".mp3"）
        string removePathPrefix              // 要移除的路径前缀（用于相对路径转换）
    )
}
```

### 处理流程

1. **建立目标文件索引**
   - 递归扫描目标目录
   - 查找匹配指定扩展名的文件
   - 以文件名（不含扩展名）为 key，完整路径为 value 建立字典

2. **处理输入文件列表**
   - 按文件名（不含扩展名）分组，处理重复文件名
   - 对每个唯一文件名：
     - 在目标目录中找到：使用找到的路径，并转换为相对路径（移除指定前缀）
     - 未找到：从输入文件组中选择优先格式的文件，加入未找到列表

3. **返回结果**
   - `OutputFiles`：成功匹配并转换后的路径列表
   - `NotFoundFiles`：在目标目录中未找到的文件列表

## Quicker 集成

### Runner 接口

提供 `Runner` 类供 Quicker 调用：

```csharp
public static class Runner
{
    /// <summary>
    /// Process file paths and return result
    /// </summary>
    public static PathConvertResult ProcessPaths(
        List<string> inputFiles,
        string searchDirectory,
        string audioExtensions = "*.mp3,*.flac,*.mp4",
        string preferredExtension = ".mp3",
        string removePathPrefix = ""
    )
    
    /// <summary>
    /// Show main window (UI mode)
    /// </summary>
    public static void ShowMainWindow()
}
```

### Quicker 动作调用示例

在 Quicker 动作中，可以这样调用：

```csharp
// 调用 Runner 处理路径
var result = Runner.ProcessPaths(
    inputFiles: fileList,
    searchDirectory: targetDir,
    audioExtensions: "*.mp3,*.flac,*.mp4",
    preferredExtension: ".mp3",
    removePathPrefix: basePath
);

// 获取结果
var outputFiles = result.OutputFiles;      // 成功匹配的文件路径
var notFoundFiles = result.NotFoundFiles;  // 未找到的文件
```

## UI 功能

### 主窗口功能

1. **参数配置**
   - 目标文件夹选择
   - 音频扩展名筛选（如 `*.mp3,*.flac,*.mp4`）
   - 优先保留的扩展名（如 `.mp3`）
   - 相对路径前缀（用于路径转换）

2. **输入处理**
   - 支持从文本输入文件路径列表
   - 支持从文件读取播放列表（M3U/M3U8）
   - 支持从剪贴板读取

3. **结果展示**
   - 显示处理后的文件路径列表
   - 显示未找到的文件列表
   - 支持复制结果到剪贴板
   - 支持保存为播放列表文件

## 构建

运行构建脚本：

```powershell
.\build.ps1
```

## 技术栈

- .NET Framework 4.7.2
- WPF
- CommunityToolkit.Mvvm 8.4.0

## 参考

- 原始实现：`D:\source\repos\quicker\build-action\PlaylistPathReplacer\Program.cs`
- Quicker 动作：`C:\Users\ldy\Desktop\路径转换_20251210_132900.qka`
