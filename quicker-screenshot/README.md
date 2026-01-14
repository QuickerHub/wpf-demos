# Quicker Screenshot

截图工具，用于 Quicker 集成。

## 功能特性

- **截图功能**：提供截图功能（待实现）

## 使用方法

### 基本用法

```csharp
using QuickerScreenshot;

// TODO: 添加使用示例
```

## 构建

```bash
dotnet build -c Release
```

或使用构建脚本：

```powershell
.\build.ps1
```

## 依赖项

- .NET Framework 4.7.2
- WPF
- CommunityToolkit.Mvvm
- HandyControl
- log4net
- Newtonsoft.Json

## 项目结构

```
QuickerScreenshot/
├── src/
│   └── QuickerScreenshot/
│       ├── ViewModels/
│       │   └── MainWindowViewModel.cs
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       └── Properties/
│           └── AssemblyInfo.cs
├── QuickerScreenshot.slnx
├── build.yaml
├── build.ps1
└── version.json
```

## 开发规范

本项目遵循 WPF MVVM 开发规范：

- 使用 `CommunityToolkit.Mvvm` 进行 MVVM 开发
- 使用 `[ObservableProperty]` 特性标记属性
- 使用 `[RelayCommand]` 特性标记命令
- Window 的 DataContext 设置为 `this`，通过 `ViewModel` 属性访问 ViewModel
- 使用 HandyControl 的颜色资源支持暗黑模式

## 注意事项

- Release 模式输出为 Library，用于 Quicker 集成
- Debug 模式输出为 WinExe，用于独立测试
