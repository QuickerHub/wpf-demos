# Windows Tools

Windows 工具集

## 项目结构

```
WindowsTools/
├── src/
│   └── WindowsTools/
│       └── WindowsTools.csproj
├── WindowsTools.sln
└── README.md
├── build.yaml
├── build.ps1
└── version.json
```

## 开发规范

本项目遵循 WPF MVVM 开发规范，使用：
- `CommunityToolkit.Mvvm` - MVVM 特性
- `DependencyPropertyGenerator` - DependencyProperty 源生成器（如需要）

## 构建

运行构建脚本：

```powershell
.\build.ps1
```

## 技术栈

- .NET Framework 4.7.2
- WPF
- CommunityToolkit.Mvvm 8.4.0
