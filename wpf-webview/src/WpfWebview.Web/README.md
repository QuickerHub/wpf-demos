# WpfWebview.Web

WPF WebView 演示项目的前端部分。

## 技术栈

- **Vite** - 快速的前端构建工具和开发服务器
- **原生 HTML/CSS/JavaScript** - 轻量级，无需框架

## 开发

### 安装依赖

```bash
pnpm install
```

### 启动开发服务器

```bash
pnpm dev
```

开发服务器将在 `http://localhost:5173` 启动。

### 构建生产版本

```bash
pnpm build
```

构建输出将在 `dist/` 目录中。

## 与 WPF 交互

### JavaScript 调用 C# 方法

```javascript
const wpfHost = window.chrome.webview.hostObjects.wpfHost;
const result = await wpfHost.ShowMessage('Hello from JavaScript!');
```

### 发送消息到 WPF

```javascript
window.wpfBridge.sendMessage('Hello from WebView!');
```

### 接收来自 WPF 的消息

```javascript
window.onWpfMessage = function(message) {
    console.log('收到 WPF 消息:', message);
};
```

## 项目结构

```
WpfWebview.Web/
├── index.html          # 主 HTML 文件
├── styles/
│   └── main.css        # 样式文件
├── scripts/
│   └── main.js         # 主 JavaScript 文件
├── package.json        # 项目配置
├── vite.config.js      # Vite 配置
└── README.md           # 项目说明
```

