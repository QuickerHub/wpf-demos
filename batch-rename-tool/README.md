# BatchRenameTool

批量重命名工具 - 一个功能强大的 WPF 文件批量重命名应用程序。

## 功能特性

- 双列表预览：左侧显示重命名前的文件名，右侧显示重命名后的文件名
- 滚动同步：左右两个列表同步滚动，方便对比查看
- 多种重命名规则：支持多种重命名模式（待扩展）
- 实时预览：修改重命名规则后实时预览结果
- MVVM 架构：使用 CommunityToolkit.Mvvm 实现清晰的代码结构

## 技术栈

- .NET Framework 4.7.2
- WPF
- CommunityToolkit.Mvvm 8.4.0
- HandyControl UI 库

## 项目结构

```
batch-rename-tool/
├── src/
│   └── BatchRenameTool/
│       ├── BatchRenameTool.csproj
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       ├── ViewModels/
│       │   └── BatchRenameViewModel.cs
│       └── Properties/
│           └── AssemblyInfo.cs
├── BatchRenameTool.sln
├── version.json
└── README.md
```

## 开发规范

本项目遵循 WPF 开发规范，使用 MVVM 模式：
- 使用 `[ObservableProperty]` 特性标记属性
- 使用 `[RelayCommand]` 特性标记命令
- UserControl 的 DataContext 设置为 this，通过 ViewModel 属性访问 ViewModel

## 构建

使用 Visual Studio 或 MSBuild 构建项目。
