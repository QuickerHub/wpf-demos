# WebViewMarkdownTip

基于 **WPF (.NET Framework 4.7.2)**、**WebView2** 与 **React + TypeScript** 的提示窗口：宿主窗口负责弹出与标题栏，页面内使用 `react-markdown` 渲染 Markdown。

## 布局与约定

- `src/WebViewMarkdownTip/`：WPF 项目（MVVM，`CommunityToolkit.Mvvm`）。
- `src/WebViewMarkdownTip.Web/`：Vite + React 前端；开发端口 **5174**（与仓库内 `wpf-webview` 的 5173 区分）。
- Release 构建会先执行 `pnpm build`，再将 `dist` 复制到输出目录 `Web/`。
- Debug 下若存在 `.vite-dev-server`（由 Vite 插件写入），WebView 优先加载开发服务器地址。

## 本地开发

1. 安装依赖并启动前端（需在仓库机器上已安装 `pnpm`）：

```powershell
cd src/WebViewMarkdownTip.Web
pnpm install
pnpm dev
```

2. 另开终端编译并运行 WPF（Debug）：

```powershell
dotnet build src/WebViewMarkdownTip/WebViewMarkdownTip.csproj
dotnet run --project src/WebViewMarkdownTip/WebViewMarkdownTip.csproj
```

首次 Debug 构建会将 `WebViewMarkdownTip.Web/.vite-dev-server` 复制到输出目录 `Web/`，便于 WebView 指向 dev server。

## Quicker 集成

Release 生成 `WebViewMarkdownTip.{Version}.dll`，入口示例：`Runner.ShowMarkdownTip(markdown, title)`。

## 构建发布包

使用仓库统一的 `qkbuild` 流程时，可执行根目录 `build.ps1`（需本机已配置 `qkbuild`）。
