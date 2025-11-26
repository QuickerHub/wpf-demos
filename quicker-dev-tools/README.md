# Quicker Dev Tools

Quicker 开发工具

## 项目结构

```
quicker-dev-tools/
├── src/
│   └── quicker-dev-tools/
│       └── quicker-dev-tools.csproj
├── quicker-dev-tools.sln
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

