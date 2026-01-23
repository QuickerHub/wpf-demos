# XML 四元数检测工具

## 项目简介

XML 四元数检测工具是一个 WPF 应用程序，用于检测 XML 文件中每个 `<Node>` 元素的四元数旋转角度，判断是否为 90 度旋转，并返回不符合条件的 Node name。

## 功能特性

- 读取标准 XML 文件
- 自动检测每个 `<Node>` 元素中 `<Animation>` 下 `<KeyFrame>` 的 `rotate` 属性（四元数格式：x,y,z,w）
- 判断四元数是否表示 90 度旋转（包括无旋转 0 度）
- 返回不符合 90 度旋转条件的 Node name 列表
- 支持复制结果到剪贴板
- 支持保存结果到文件
- 集成 Quicker 框架，可作为 Quicker 动作运行

## 使用方法

### 作为独立应用程序

1. 运行应用程序
2. 点击"选择文件"按钮，选择要检测的 XML 文件
3. 点击"开始检测"按钮
4. 查看检测结果，不符合 90 度旋转条件的 Node name 会显示在结果列表中
5. 可以点击"复制结果"将结果复制到剪贴板，或点击"保存结果"保存到文件

### 作为 Quicker 动作

在 Quicker 中配置动作，调用 `Runner.Run()` 方法即可显示主窗口。

## 技术说明

### 四元数格式

四元数格式为：`x,y,z,w`，例如：`1.968132E-10,-5.810807E-09,-3.567104E-09,1`

### 90 度旋转判断

工具会检测以下情况：

1. **无旋转（0 度）**：四元数接近 `(0, 0, 0, 1)`
2. **绕 X 轴 90 度旋转**：四元数接近 `(±0.707, 0, 0, ±0.707)`
3. **绕 Y 轴 90 度旋转**：四元数接近 `(0, ±0.707, 0, ±0.707)`
4. **绕 Z 轴 90 度旋转**：四元数接近 `(0, 0, ±0.707, ±0.707)`

判断时允许小的浮点误差（默认 epsilon = 0.01）。

### XML 文件结构

工具期望的 XML 结构：

```xml
<Node name="node_name" ...>
  <Animation>
    <KeyFrame time="0" rotate="x,y,z,w" ... />
    <KeyFrame time="1" rotate="x,y,z,w" ... />
  </Animation>
</Node>
```

工具会检查每个 Node 元素，如果找到 `Animation` 下的 `KeyFrame` 元素，会使用第一个有效的 `rotate` 属性进行检测。

## 项目结构

```
xml-extract-tool/
├── src/
│   └── XmlExtractTool/
│       ├── Models/
│       │   ├── Quaternion.cs      # 四元数模型和工具类
│       │   └── NodeInfo.cs        # Node 信息模型
│       ├── Services/
│       │   └── XmlQuaternionChecker.cs  # XML 解析和四元数检测服务
│       ├── Utils/
│       │   └── XmlHelper.cs       # XML 处理工具
│       ├── ViewModels/
│       │   └── XmlExtractViewModel.cs  # 主视图模型
│       ├── Converters/
│       │   └── InverseBooleanConverter.cs  # 布尔值反转转换器
│       ├── MainWindow.xaml         # 主窗口 UI
│       ├── MainWindow.xaml.cs      # 主窗口代码
│       ├── App.xaml               # 应用程序资源
│       ├── App.xaml.cs            # 应用程序入口
│       └── Runner.cs              # Quicker 集成入口
├── XmlExtractTool.slnx           # 解决方案文件
├── build.ps1                      # 构建脚本
├── build.yaml                    # 构建配置
├── version.json                  # 版本信息
└── README.md                     # 项目说明
```

## 开发规范

- 使用 MVVM 架构模式
- 使用 `CommunityToolkit.Mvvm` 进行属性绑定和命令处理
- 使用 HandyControl 组件库，支持暗黑模式
- XML 处理使用 `System.Xml.Linq`（LINQ to XML）
- 遵循 WPF 开发规范（DataContext 设置为 this，使用 ViewModel 属性访问）

## 构建说明

使用 `build.ps1` 脚本进行构建：

```powershell
.\build.ps1
```

或使用 qkbuild 工具：

```powershell
qkbuild build -c "build.yaml" --project-path "src\XmlExtractTool"
```

## 版本信息

当前版本：1.0.0.0

## 许可证

本项目为定制开发项目。
