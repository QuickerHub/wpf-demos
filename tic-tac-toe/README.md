# 🎮 井字棋游戏 (TicTacToe WPF)

一个使用 WPF 和 C# 开发的智能井字棋游戏，具有三种难度级别的 AI 对手。

## ✨ 特性

- **智能 AI**: 使用 Minimax 算法实现的最优策略
- **三种难度**: 简单（随机）、中等（混合）、困难（完全智能）
- **现代界面**: 美观的 WPF 用户界面
- **即时反馈**: 清晰的游戏状态显示

## 🚀 快速开始

### 方法一：使用启动脚本（推荐）

**Windows PowerShell:**
```powershell
.\run.ps1
```

**Windows 命令提示符:**
```cmd
run.bat
```

### 方法二：手动运行

1. 确保已安装 [.NET SDK](https://dotnet.microsoft.com/download)
2. 打开终端，进入项目目录
3. 构建并运行项目：

```bash
dotnet build src/TicTacToe/TicTacToe.csproj
dotnet run --project src/TicTacToe/TicTacToe.csproj
```

## 🎯 游戏说明

- **你是 X**，**AI 是 O**
- 选择难度级别后开始游戏
- 点击空位下棋
- AI 会自动响应你的移动
- 点击"重置游戏"开始新一局

## 🧠 AI 难度说明

- **简单**: AI 随机选择空位，适合新手
- **中等**: 50% 概率使用智能算法，50% 随机
- **困难**: 完全使用 Minimax 算法，几乎不可战胜

## 🛠️ 技术栈

- **.NET Framework 4.7.2**
- **WPF (Windows Presentation Foundation)**
- **C# 11.0**
- **Minimax 算法**

## 📁 项目结构

```
src/
├── TicTacToe/
│   ├── MainWindow.xaml          # 主窗口界面
│   ├── MainWindow.xaml.cs       # 主窗口逻辑
│   ├── GameButton.xaml          # 游戏按钮界面
│   ├── GameButton.xaml.cs       # 游戏按钮逻辑
│   ├── TicTacToeAI.cs           # AI 算法实现
│   └── Piece.cs                 # 棋子枚举
├── TicTacToe.sln               # 解决方案文件
└── TicTacToe.csproj            # 项目文件
```

## 🎮 游戏截图

游戏界面包含：
- 3×3 游戏棋盘
- 难度选择下拉框
- 重置游戏按钮
- 游戏状态显示

## 📝 开发说明

这是一个学习项目，展示了：
- WPF 应用程序开发
- 游戏 AI 算法实现
- 用户界面设计
- 事件处理和数据绑定

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License
# wpf-demos
