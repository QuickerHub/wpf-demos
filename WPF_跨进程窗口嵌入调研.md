# WPF 跨进程窗口嵌入调研报告

## 概述

在 WPF 中，**原生不支持**在进程 A 的窗口 A 中直接嵌入并显示进程 B 的窗口 B。WPF 的 `Window` 对象在设计上无法跨进程进行嵌套或托管。

## 技术方案

### 方案一：使用 Win32 API `SetParent`（不推荐）

#### 原理
通过 Win32 API 的 `SetParent` 函数，将进程 B 的窗口 B 设置为进程 A 的窗口 A 的子窗口。

#### 实现步骤
1. 获取进程 B 窗口的句柄（HWND）
   - 使用 `FindWindow`、`EnumWindows` 或通过进程 ID 查找
2. 获取进程 A 窗口的句柄（HWND）
   - 使用 `WindowInteropHelper` 获取 WPF 窗口的句柄
3. 调用 `SetParent` 设置父子关系
4. 调整窗口样式和位置

#### 代码示例
```csharp
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

public class WindowEmbedder
{
    public static bool EmbedWindow(IntPtr parentHwnd, IntPtr childHwnd)
    {
        var parent = new HWND(parentHwnd);
        var child = new HWND(childHwnd);
        
        if (!IsWindow(parent) || !IsWindow(child))
            return false;
        
        // Set parent-child relationship
        var previousParent = SetParent(child, parent);
        
        if (previousParent.Value == IntPtr.Zero)
            return false;
        
        // Adjust window style to make it a child window
        var style = GetWindowLongPtr(child, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style = (nint)((long)style & ~(long)WINDOW_STYLE.WS_POPUP);
        style = (nint)((long)style | (long)WINDOW_STYLE.WS_CHILD);
        SetWindowLongPtr(child, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style);
        
        // Resize and position the child window
        SetWindowPos(child, HWND.Null, 0, 0, 800, 600, 
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        
        return true;
    }
}
```

#### 严重问题
1. **消息循环冲突**：跨进程窗口的消息循环不同步，可能导致消息丢失或死锁
2. **输入焦点问题**：键盘和鼠标输入可能无法正确传递到子窗口
3. **渲染问题**：WPF 窗口使用 DirectX/DirectComposition，与其他进程的窗口渲染机制冲突
4. **窗口样式冲突**：子窗口的样式可能与 WPF 布局系统冲突
5. **稳定性问题**：可能导致应用程序崩溃或窗口无法正常显示

#### 注意事项
- 代码库中的 `WindowAttachManagerService.cs` 明确注释：**"Using GWLP_HWNDPARENT instead of SetParent to avoid WPF rendering issues"**
- 这表明项目已经意识到 `SetParent` 会导致 WPF 渲染问题

---

### 方案二：使用 `HwndHost` 类（有限支持）

#### 原理
WPF 提供了 `HwndHost` 类，允许在 WPF 窗口中嵌入非托管的 Win32 窗口。理论上可以用于跨进程窗口嵌入。

#### 实现步骤
1. 继承 `HwndHost` 类
2. 重写 `BuildWindowCore` 方法，在其中附加目标窗口
3. 重写 `DestroyWindowCore` 方法，确保正确释放资源
4. 在 XAML 中使用自定义的 `HwndHost`

#### 代码示例
```csharp
using System;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

public class CrossProcessHwndHost : HwndHost
{
    private readonly IntPtr _childWindowHandle;
    
    public CrossProcessHwndHost(IntPtr childWindowHandle)
    {
        _childWindowHandle = childWindowHandle;
    }
    
    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var parent = new HWND(hwndParent.Handle);
        var child = new HWND(_childWindowHandle);
        
        if (!IsWindow(parent) || !IsWindow(child))
            return new HandleRef(this, IntPtr.Zero);
        
        // Set parent-child relationship
        SetParent(child, parent);
        
        // Adjust window style
        var style = GetWindowLongPtr(child, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style = (nint)((long)style & ~(long)WINDOW_STYLE.WS_POPUP);
        style = (nint)((long)style | (long)WINDOW_STYLE.WS_CHILD);
        SetWindowLongPtr(child, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style);
        
        // Position and show
        SetWindowPos(child, HWND.Null, 0, 0, 
            (int)ActualWidth, (int)ActualHeight,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        
        return new HandleRef(this, _childWindowHandle);
    }
    
    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        // Restore original parent if needed
        var child = new HWND(_childWindowHandle);
        if (IsWindow(child))
        {
            SetParent(child, HWND.Null);
        }
    }
}
```

#### 限制
- **主要用于同一进程**：`HwndHost` 设计用于托管同一进程内的 Win32 控件
- **跨进程问题依然存在**：即使使用 `HwndHost`，跨进程窗口嵌入的所有问题仍然存在
- **不适用于 WPF 窗口**：如果进程 B 的窗口也是 WPF 窗口，问题会更严重

---

### 方案三：使用 `SetWindowOwner`（窗口跟随，非嵌入）

#### 原理
使用 `GWLP_HWNDPARENT` 设置窗口所有者关系，让窗口 B 跟随窗口 A，但不创建真正的父子关系。

#### 实现
代码库中已有实现，参考：
- `WindowHelper.SetWindowOwner` 方法
- `WindowAttachService` 类

#### 特点
- ✅ **安全**：不会导致 WPF 渲染问题
- ✅ **稳定**：不会破坏窗口的消息循环
- ❌ **不是真正的嵌入**：窗口 B 仍然是独立窗口，只是跟随窗口 A
- ✅ **适用于虚拟桌面同步**：可以让窗口跟随父窗口的虚拟桌面

#### 代码示例（来自代码库）
```csharp
// 来自 WindowHelper.cs
public static bool SetWindowOwner(IntPtr hWnd, IntPtr hWndOwner, bool preventActivation = true)
{
    // Set GWLP_HWNDPARENT to make the window follow the owner's virtual desktop
    // This is safer than SetParent for WPF windows as it doesn't create a parent-child relationship
    var previousOwner = SetWindowLongPtr(hwnd.Value, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, hWndOwner);
    
    // Set WS_EX_NOACTIVATE to prevent focus stealing
    if (preventActivation)
    {
        bool setNoActivate = hWndOwner != IntPtr.Zero;
        SetWindowLongPtrFlags(hwnd.Value, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 
            (uint)WINDOW_EX_STYLE.WS_EX_NOACTIVATE, setNoActivate);
    }
    
    return previousOwner != IntPtr.Zero || hWndOwner == IntPtr.Zero;
}
```

---

## 替代方案

### 方案四：进程间通信（IPC）+ 重新渲染

如果需要在窗口 A 中显示窗口 B 的内容，可以考虑：

1. **使用 IPC 传递数据**
   - 命名管道、WCF、gRPC 等
   - 进程 B 将窗口内容（截图、数据）发送给进程 A

2. **进程 A 重新渲染**
   - 进程 A 接收数据后，在自己的窗口中重新渲染内容
   - 使用 WPF 控件显示内容

3. **实时更新**
   - 通过定时器或事件驱动，实时同步窗口 B 的状态

#### 优点
- ✅ 完全稳定，无跨进程窗口问题
- ✅ 可以完全控制渲染和交互
- ✅ 跨平台兼容性更好

#### 缺点
- ❌ 需要重新实现 UI
- ❌ 可能有性能开销
- ❌ 需要进程 B 支持数据导出

---

### 方案五：窗口覆盖（Overlay）

如果只是需要视觉上的"嵌入"效果，可以使用窗口覆盖：

1. **窗口 B 设置为无边框、透明背景**
2. **窗口 B 跟随窗口 A 的位置和大小**
3. **窗口 B 设置为窗口 A 的子窗口（使用 SetWindowOwner）**
4. **使用窗口层级控制显示顺序**

代码库中的 `WindowAttachService` 实现了类似的功能。

---

## 总结与建议

### 技术可行性

| 方案 | 可行性 | 稳定性 | 推荐度 |
|------|--------|--------|--------|
| `SetParent` | ⚠️ 技术上可行 | ❌ 不稳定 | ❌ 不推荐 |
| `HwndHost` | ⚠️ 有限支持 | ❌ 不稳定 | ❌ 不推荐 |
| `SetWindowOwner` | ✅ 可行 | ✅ 稳定 | ⚠️ 仅窗口跟随 |
| IPC + 重新渲染 | ✅ 可行 | ✅ 稳定 | ✅ 推荐 |
| 窗口覆盖 | ✅ 可行 | ✅ 稳定 | ⚠️ 视觉嵌入 |

### 最佳实践建议

1. **避免真正的跨进程窗口嵌入**
   - WPF 架构不支持，强制实现会导致各种问题
   - 代码库中已经明确避免使用 `SetParent`

2. **使用窗口跟随（SetWindowOwner）**
   - 如果只需要窗口 B 跟随窗口 A，使用 `SetWindowOwner`
   - 这是最安全的方式，代码库中已有成熟实现

3. **使用 IPC + 重新渲染**
   - 如果需要真正的"嵌入"效果，使用进程间通信
   - 进程 A 接收数据后重新渲染，完全控制 UI

4. **考虑架构调整**
   - 如果可能，将两个窗口合并到同一进程
   - 使用插件架构或模块化设计

### 代码库参考

- `window-attach` 项目：实现了窗口跟随功能
- `WindowHelper.SetWindowOwner`：安全的窗口所有者设置方法
- `WindowAttachService`：窗口附着服务的实现

---

## 参考资料

- [MSDN: SetParent function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setparent)
- [MSDN: HwndHost Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.hwndhost)
- [MSDN: Window Relationships](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#window-relationships)
- 代码库：`window-attach/src/WindowAttach/Utils/WindowHelper.cs`
- 代码库：`window-attach/src/WindowAttach/Services/WindowAttachService.cs`

