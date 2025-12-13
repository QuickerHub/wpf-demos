# 🌐 WPF WebView 交互演示

一个使用 WPF 和 WebView2 开发的演示项目，展示网页与 WPF 应用程序之间的双向交互。

## ✨ 特性

- **WebView2 集成**: 使用 Microsoft Edge WebView2 控件展示网页内容
- **双向交互**: 
  - JavaScript 调用 C# 方法（通过 Host Objects）
  - C# 调用 JavaScript 方法（通过 ExecuteScriptAsync）
  - 消息传递（WebMessageReceived）
- **MVVM 架构**: 使用 CommunityToolkit.Mvvm 实现 MVVM 模式
- **现代界面**: 使用 HandyControl 实现美观的界面，支持暗黑模式

## 🚀 快速开始

### 前置要求

1. **.NET Framework 4.7.2** 或更高版本
2. **WebView2 Runtime**: 需要安装 [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
3. **Visual Studio 2022** 或 **.NET SDK**（用于构建）
4. **Node.js** 和 **pnpm**（用于前端开发）
   - 安装 Node.js: https://nodejs.org/
   - 安装 pnpm: `npm install -g pnpm`

### 开发模式（推荐）

**使用开发脚本启动前端服务器:**
```powershell
cd src\WpfWebview.Web
.\dev.ps1
```

这个脚本会：
1. 启动 Vite 开发服务器（`pnpm dev`）
2. 等待服务器就绪
3. 将服务器 URL 写入 `.vite-dev-server` 文件
4. 保持运行（按 Ctrl+C 停止服务器）

**在另一个终端启动 WPF 应用程序:**
```powershell
cd src\WpfWebview
dotnet run --configuration Debug
```

WPF 应用程序会自动从 `.vite-dev-server` 文件读取开发服务器 URL 并连接。

**停止开发服务器:**
```powershell
cd src\WpfWebview.Web
.\stop-dev.ps1
```

### 生产模式

**使用启动脚本:**
```powershell
.\run.ps1
```

这个脚本会自动检查并构建前端项目（如果需要），然后启动 WPF 应用程序。

**手动启动:**

1. **构建前端项目**:
```powershell
cd src\WpfWebview.Web
pnpm install
pnpm build
```

2. **运行 WPF 应用程序**:
```powershell
cd src\WpfWebview
dotnet run
```

WPF 应用程序会从 `src/WpfWebview.Web/dist` 目录加载网页。

### 注意事项

如果前端未构建或开发服务器未运行，WPF 应用程序会显示错误提示，指导你如何启动开发服务器或构建生产版本。

## 📖 功能说明

### 1. JavaScript 调用 C# 方法

网页可以通过 `window.chrome.webview.hostObjects.wpfHost` 调用 C# 方法：

```javascript
// 调用 C# 方法
const wpfHost = window.chrome.webview.hostObjects.wpfHost;
const result = await wpfHost.ShowMessage('Hello from JavaScript!');
console.log(result); // 输出: "WPF 已收到消息: Hello from JavaScript!"
```

### 2. C# 调用 JavaScript 方法

WPF 应用程序可以通过 `ExecuteScriptAsync` 调用 JavaScript 函数：

```csharp
// 调用 JavaScript 函数
await webView.CoreWebView2.ExecuteScriptAsync(
    "window.receiveFromWpf({type: 'wpfMessage', data: 'Hello from WPF!'});"
);
```

### 3. 消息传递

网页可以通过 `postMessage` 发送消息到 WPF：

```javascript
// 从网页发送消息
window.chrome.webview.postMessage(JSON.stringify({
    type: 'message',
    data: 'Hello from WebView!'
}));
```

WPF 通过 `WebMessageReceived` 事件接收消息：

```csharp
webView.CoreWebView2.WebMessageReceived += (sender, e) =>
{
    var message = e.TryGetWebMessageAsString();
    // 处理消息
};
```

## 🛠️ 技术栈

- **.NET Framework 4.7.2**
- **WPF (Windows Presentation Foundation)**
- **Microsoft.Web.WebView2** - WebView2 控件
- **CommunityToolkit.Mvvm** - MVVM 框架
- **HandyControl** - UI 控件库
- **C# 11.0** (preview)

## 📁 项目结构

```
wpf-webview/
├── src/
│   ├── WpfWebview/                     # WPF 应用程序
│   │   ├── App.xaml / App.xaml.cs      # 应用程序入口
│   │   ├── MainWindow.xaml / MainWindow.xaml.cs  # 主窗口
│   │   ├── MainViewModel.cs            # 主视图模型（MVVM）
│   │   ├── InputDialog.xaml / InputDialog.xaml.cs  # 输入对话框
│   │   └── Properties/                 # 程序集信息
│   └── WpfWebview.Web/                 # 前端项目（独立文件夹）
│       ├── index.html                  # 主 HTML 文件
│       ├── styles/                     # 样式文件
│       ├── scripts/                     # JavaScript 文件
│       ├── package.json                # pnpm 项目配置
│       ├── vite.config.js              # Vite 配置
│       └── dist/                       # 构建输出（运行 pnpm build 后生成）
├── WpfWebview.sln                      # 解决方案文件
├── build.yaml                          # 构建配置
└── README.md                            # 项目说明
```

## 🎯 使用示例

### 在网页中调用 WPF 方法

1. 打开应用程序
2. 在网页的输入框中输入消息
3. 点击"发送到 WPF"按钮，消息会显示在 WPF 界面上
4. 点击"调用 WPF 方法"按钮，会调用 C# 方法并显示消息框

### 从 WPF 发送消息到网页

1. 在 WPF 界面的"发送消息到网页"输入框中输入消息
2. 点击"发送"按钮
3. 消息会显示在网页的"接收来自 WPF 的消息"区域

## 📝 开发说明

这个项目展示了：

- **WebView2 集成**: 如何在 WPF 应用中集成 WebView2
- **Host Objects**: 如何暴露 C# 对象给 JavaScript
- **JavaScript 注入**: 如何在网页加载时注入 JavaScript 代码
- **消息传递**: 如何在网页和 WPF 之间传递消息
- **MVVM 模式**: 如何使用 MVVM 模式组织代码
- **异步操作**: 如何处理 WebView2 的异步操作

## 🔧 配置说明

### 自动构建流程

**Debug 模式构建：**
- 仅构建 WPF 项目，不构建前端
- 前端通过开发服务器提供（需要先运行 `dev.ps1`）
- WPF 应用从 `.vite-dev-server` 文件读取开发服务器 URL

**Release 模式构建：**
- 自动构建前端项目（运行 `pnpm build`）
- 将构建后的 `dist` 文件夹内容复制到输出目录的 `Web` 文件夹
- WPF 应用从本地 `Web` 文件夹加载网页

**构建输出结构：**
```
bin/Debug/net472/
├── WpfWebview.exe
├── Web/                    # 自动复制的网页文件
│   ├── index.html
│   └── assets/
│       ├── main-*.js
│       └── main-*.css
└── ...
```

### WebView2 环境

项目使用 `CoreWebView2Environment.CreateAsync()` 创建 WebView2 环境。如果系统未安装 WebView2 Runtime，首次运行时会自动下载。

### 网页加载策略

**Debug 模式：**
1. 从 `.vite-dev-server` 文件读取开发服务器 URL
2. 检查开发服务器是否运行
3. 如果运行，连接到开发服务器
4. 如果未运行，回退到本地文件加载

**Release 模式：**
1. 从输出目录的 `Web` 文件夹加载（构建时自动复制）
2. 使用虚拟主机映射（`http://app.webview.local/`）确保相对路径正常工作

### 依赖项

**WPF 项目依赖的 NuGet 包：**
- `Microsoft.Web.WebView2` - WebView2 控件
- `CommunityToolkit.Mvvm` - MVVM 框架
- `DependencyPropertyGenerator` - 依赖属性生成器
- `log4net` - 日志框架
- `Newtonsoft.Json` - JSON 序列化

**前端项目依赖的 npm 包：**
- `vite` - 前端构建工具和开发服务器

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License

