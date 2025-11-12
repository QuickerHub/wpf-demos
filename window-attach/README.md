# 窗口吸附 (WindowAttach)

一个用于 Windows 的窗口吸附工具，可以让一个窗口永久跟随另一个窗口移动，实现窗口之间的绑定关系。

## 功能特性

### 核心功能

#### 1. 窗口吸附
- **窗口2永久跟随窗口1**：当窗口1移动时，窗口2会自动跟随移动，保持相对位置不变
- **实时位置跟踪**：使用 `WinEventHook` 监听窗口位置变化事件，实现实时、低延迟的位置同步
- **像素级精确计算**：所有位置计算基于物理像素坐标，不受 DPI 缩放影响

#### 2. 12种位置计算方式
支持窗口2相对于窗口1的12种不同位置：

- **左侧**：`LeftTop`、`LeftCenter`、`LeftBottom`
- **上方**：`TopLeft`、`TopCenter`、`TopRight`
- **右侧**：`RightTop`、`RightCenter`、`RightBottom`
- **下方**：`BottomLeft`、`BottomCenter`、`BottomRight`

#### 3. 窗口状态同步
- **最小化同步**：当窗口1最小化时，窗口2也会自动最小化
- **恢复同步**：当窗口1恢复显示时，窗口2也会自动恢复
- **工具窗口特殊处理**：如果窗口2不在任务栏显示（`WS_EX_TOOLWINDOW`），最小化时会隐藏窗口2，恢复时显示但不激活

#### 4. 位置偏移和限制
- **自定义偏移量**：支持设置水平偏移（`offsetX`）和垂直偏移（`offsetY`）
- **同屏限制**：可选择限制窗口2与窗口1在同一屏幕内，防止窗口超出屏幕边界

### 高级功能

#### 5. 多窗口管理
- **多组窗口绑定**：支持同时管理多组窗口的吸附关系
- **动态数据管理**：使用 `DynamicData.SourceCache` 实现响应式数据管理，支持实时更新和排序
- **统一服务管理**：通过 `WindowAttachManagerService` 统一管理所有窗口绑定

#### 6. 自动解绑机制
- **窗口销毁检测**：使用 `WinEventHook` 监听窗口销毁事件（`EVENT_OBJECT_DESTROY`）
- **自动解绑**：当窗口1或窗口2被销毁时，自动解除绑定关系并清理资源
- **资源自动释放**：解绑时自动释放事件钩子和相关资源

#### 7. 弹出式解绑按钮
- **自动显示**：当建立主绑定（Main）时，自动在窗口2附近显示半透明的"取消吸附"按钮
- **智能定位**：根据主绑定的位置自动计算弹出按钮的最佳位置，避免与窗口1和窗口2重叠
- **一键解绑**：点击按钮即可解除主绑定，同时自动销毁弹出窗口

#### 8. 黑名单机制
- **防止误绑定**：弹出窗口的句柄会被加入黑名单，防止被其他主绑定使用
- **智能过滤**：黑名单仅对主绑定生效，弹出绑定不受影响

#### 9. 管理界面
- **可视化列表**：提供 WPF 管理窗口，显示所有已注册的窗口绑定
- **MVVM 架构**：使用 MVVM 设计模式，代码结构清晰
- **实时更新**：使用 `DynamicData` 实现响应式列表，绑定状态变化时自动更新
- **过滤显示**：自动过滤弹出绑定，只显示主绑定
- **排序功能**：按注册时间倒序排列，最新绑定的显示在最上方
- **一键解绑**：在管理界面中可以方便地解除任意绑定

### 技术特性

#### 10. Win32 API 集成
- **CsWin32 支持**：使用 `Microsoft.Windows.CsWin32` 自动生成类型安全的 Win32 API 封装
- **统一事件钩子**：`WindowEventHook` 类统一管理窗口位置变化和销毁事件
- **自定义 WindowRect**：提供易用的 `WindowRect` 结构，封装窗口矩形信息

#### 11. 依赖管理
- **Costura.Fody 嵌入**：使用 `Costura.Fody` 将 `DynamicData` 及其依赖嵌入到主程序集中，简化部署
- **单文件部署**：最终只需要一个 DLL 文件即可运行

#### 12. 线程安全
- **UI 线程调度**：所有窗口操作都通过 `Dispatcher` 确保在 UI 线程执行
- **线程安全的事件处理**：事件回调自动切换到 UI 线程

## API 使用

### 基本用法

```csharp
using WindowAttach;
using WindowAttach.Models;

// 绑定窗口2到窗口1的右侧顶部
bool attached = Runner.AttachWindow(
    window1Handle, 
    window2Handle, 
    placement: WindowPlacement.RightTop
);

// 注册绑定（不切换状态）
bool registered = Runner.Register(
    window1Handle, 
    window2Handle, 
    placement: WindowPlacement.LeftCenter,
    offsetX: 10,  // 水平偏移10像素
    offsetY: 5,   // 垂直偏移5像素
    restrictToSameScreen: true  // 限制在同一屏幕
);

// 解除绑定
bool unregistered = Runner.Unregister(window1Handle, window2Handle);

// 检查是否已绑定
bool isAttached = Runner.IsRegistered(window1Handle, window2Handle);

// 显示管理窗口
Runner.ShowWindowList();
```

### 位置选项

```csharp
// 12种位置选项
WindowPlacement.LeftTop      // 左侧顶部
WindowPlacement.LeftCenter   // 左侧居中
WindowPlacement.LeftBottom   // 左侧底部
WindowPlacement.TopLeft      // 上方左侧
WindowPlacement.TopCenter    // 上方居中
WindowPlacement.TopRight     // 上方右侧
WindowPlacement.RightTop     // 右侧顶部（默认）
WindowPlacement.RightCenter  // 右侧居中
WindowPlacement.RightBottom  // 右侧底部
WindowPlacement.BottomLeft   // 下方左侧
WindowPlacement.BottomCenter // 下方居中
WindowPlacement.BottomRight  // 下方右侧
```

## 架构设计

### 核心类

- **`WindowAttachService`**：单个窗口绑定的核心服务，负责位置计算和状态同步
- **`WindowAttachManagerService`**：多窗口绑定管理服务，使用 `DynamicData.SourceCache` 管理
- **`WindowEventHook`**：统一的事件钩子，监听窗口位置变化和销毁事件
- **`WindowHelper`**：Win32 API 封装工具类
- **`PlacementHelper`**：弹出窗口位置计算辅助类
- **`Runner`**：对外 API 接口，供 Quicker 等外部程序调用

### 数据模型

- **`WindowAttachPair`**：窗口绑定对，包含两个窗口句柄、位置、偏移量等信息
- **`WindowPlacement`**：12种位置枚举
- **`AttachType`**：绑定类型（Main/Popup）
- **`WindowRect`**：窗口矩形结构

### UI 组件

- **`WindowAttachListWindow`**：管理窗口视图
- **`WindowAttachListViewModel`**：管理窗口视图模型
- **`WindowAttachItemViewModel`**：单个绑定项视图模型
- **`DetachPopupWindow`**：弹出式解绑按钮窗口

## 构建和部署

### 构建要求

- .NET Framework 4.7.2
- Visual Studio 2019 或更高版本
- Windows SDK

### 构建步骤

1. 使用 `build.ps1` 脚本自动构建：
```powershell
.\build.ps1
```

2. 或使用 `qkbuild` 工具（如果已配置）：
```powershell
qkbuild
```

### 输出文件

- **Release 模式**：生成 `WindowAttach.{version}.dll` 库文件
- **Debug 模式**：生成 `WindowAttach.exe` 可执行文件（用于测试）

所有依赖（包括 `DynamicData`）已通过 `Costura.Fody` 嵌入到主程序集中，无需额外部署依赖文件。

## 版本信息

当前版本：1.0.3.0

版本历史：
- 1.0.3.0：添加工具窗口特殊处理（最小化时隐藏）
- 1.0.2.0：添加弹出式解绑按钮
- 1.0.1.0：添加管理界面
- 1.0.0.0：初始版本

## 许可证

本项目为 Quicker 插件，遵循 Quicker 的插件开发规范。

## 注意事项

1. **窗口句柄有效性**：确保传入的窗口句柄有效，否则绑定会失败
2. **线程安全**：所有 API 调用会自动切换到 UI 线程，但建议在 UI 线程中调用
3. **资源释放**：窗口销毁时会自动解绑，但建议手动调用 `Unregister` 释放资源
4. **DPI 缩放**：所有位置计算基于物理像素，不受系统 DPI 缩放影响
5. **多显示器**：如果启用 `restrictToSameScreen`，窗口2会被限制在窗口1所在的屏幕内

## 技术栈

- **.NET Framework 4.7.2**
- **WPF**：用户界面框架
- **CommunityToolkit.Mvvm**：MVVM 工具包
- **DynamicData**：响应式数据管理
- **Microsoft.Windows.CsWin32**：Win32 API 封装
- **Costura.Fody**：依赖嵌入工具
- **log4net**：日志记录

