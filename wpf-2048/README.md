# 🎮 2048 游戏 (WPF)

一个使用 WPF 和 C# 开发的经典 2048 数字游戏。

## ✨ 特性

- **经典玩法**: 滑动合并数字，目标达到 2048
- **现代界面**: 美观的 WPF 用户界面，符合原版 2048 的配色方案
- **分数系统**: 实时显示当前分数和历史最高分
- **自动保存**: 最高分自动保存到本地

## 🚀 快速开始

### 方法一：使用启动脚本（推荐）

**Windows PowerShell:**
```powershell
.\run.ps1
```

### 方法二：手动运行

1. 确保已安装 [.NET SDK](https://dotnet.microsoft.com/download)
2. 打开终端，进入项目目录
3. 构建并运行项目：

```bash
dotnet build src/Wpf2048/Wpf2048.csproj
dotnet run --project src/Wpf2048/Wpf2048.csproj
```

## 🎯 游戏说明

- **方向键控制**: 使用 ↑↓←→ 方向键移动方块
- **合并规则**: 相同数字的方块碰撞时会合并成一个，数字翻倍
- **胜利条件**: 达到 2048 即可获胜（可以继续游戏）
- **游戏结束**: 当无法移动且没有空格时游戏结束
- **重新开始**: 按 `R` 键或点击"New Game"按钮重新开始

## 🛠️ 技术栈

- **.NET Framework 4.7.2**
- **WPF (Windows Presentation Foundation)**
- **C# 11.0**

## 📁 项目结构

```
src/
├── Wpf2048/
│   ├── App.xaml / App.xaml.cs      # 应用程序入口
│   ├── MainWindow.xaml / MainWindow.xaml.cs  # 主窗口
│   ├── Game2048.cs                 # 游戏逻辑
│   ├── TileControl.xaml / TileControl.xaml.cs # 方块控件
│   └── Properties/                 # 程序集信息
├── Wpf2048.sln                    # 解决方案文件
└── build.yaml                     # 构建配置
```

## 🎮 游戏截图

游戏界面包含：
- 4×4 游戏网格
- 当前分数和最高分显示
- 游戏结束提示
- 操作说明

## 📝 开发说明

这是一个学习项目，展示了：
- WPF 应用程序开发
- 游戏逻辑实现
- 用户界面设计
- 事件处理和数据绑定
- 本地数据存储

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License

