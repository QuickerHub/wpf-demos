# WPF Monaco Editor DiffEditor

基于 WPF 和 WebView2 的 Monaco Editor DiffEditor 演示项目。

## 项目结构

```
wpf-monaco-editor/
├── src/
│   ├── WpfMonacoEditor/          # WPF 主项目
│   └── WpfMonacoEditor.Web/      # Web 前端项目（Monaco Editor）
├── WpfMonacoEditor.sln
├── build.yaml
├── build.ps1
└── version.json
```

## 功能特性

- ✅ 集成 Monaco Editor DiffEditor
- ✅ WPF 与 WebView2 双向通信
- ✅ 支持亮色/暗色主题自动切换
- ✅ 开发模式支持热重载
- ✅ 生产模式自动打包 Web 资源

## 开发环境要求

- .NET Framework 4.7.2
- Node.js 和 pnpm
- Visual Studio 2022 或更高版本

## 开发步骤

### 1. 安装 Web 项目依赖

```powershell
cd src/WpfMonacoEditor.Web
pnpm install
```

### 2. 启动开发服务器

```powershell
cd src/WpfMonacoEditor.Web
pnpm dev
```

开发服务器将在 `http://localhost:5174` 启动。

### 3. 运行 WPF 项目

在 Visual Studio 中打开 `WpfMonacoEditor.sln`，然后按 F5 运行。

在 Debug 模式下，WPF 应用会自动连接到开发服务器，支持热重载。

## 构建发布版本

```powershell
.\build.ps1
```

构建脚本会：
1. 自动构建 Web 项目（生产模式）
2. 将 Web 资源复制到 WPF 输出目录
3. 构建 WPF 项目

## Monaco Editor API

Web 项目暴露了 `window.monacoDiffEditor` 对象，WPF 可以通过 JavaScript 调用：

```javascript
// 设置原始文本
window.monacoDiffEditor.setOriginalText("原始内容");

// 设置修改后的文本
window.monacoDiffEditor.setModifiedText("修改后的内容");

// 获取原始文本
const original = window.monacoDiffEditor.getOriginalText();

// 获取修改后的文本
const modified = window.monacoDiffEditor.getModifiedText();
```

## 技术栈

### WPF 项目
- .NET Framework 4.7.2
- WPF
- WebView2
- CommunityToolkit.Mvvm
- HandyControl

### Web 项目
- React 18
- TypeScript
- Monaco Editor
- Vite
- @monaco-editor/react

## 参考项目

本项目参考了 `wpf-webview` 项目的架构和实现方式。

