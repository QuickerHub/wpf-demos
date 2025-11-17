# WpfLottery - 抽签程序

一个基于 WPF 的抽签程序，支持添加多个选项，随机抽取一个结果。

## 功能特性

- ✅ 添加/删除抽签选项
- ✅ 开始/停止抽签动画
- ✅ 显示抽签结果
- ✅ 清空所有选项
- ✅ MVVM 架构设计

## 项目结构

```
wpf-lottery/
├── src/
│   └── WpfLottery/
│       ├── Controls/          # 抽签控件
│       ├── ViewModels/         # ViewModel
│       ├── Windows/            # 抽签窗口
│       ├── Converters/         # 值转换器
│       └── MainWindow.xaml     # 调试主窗口
├── build.ps1
├── build.yaml
├── version.json
└── README.md
```

## 使用方法

1. 运行程序，点击"打开抽签窗口"按钮
2. 在抽签窗口中添加选项（可以输入文本后按 Enter 或点击"添加"按钮）
3. 点击"开始抽签"按钮开始抽签
4. 等待抽签结果或点击"停止"按钮提前结束

## 开发规范

- 使用 MVVM 模式
- 使用 `CommunityToolkit.Mvvm` 进行属性绑定和命令
- 遵循 WPF 开发规范

