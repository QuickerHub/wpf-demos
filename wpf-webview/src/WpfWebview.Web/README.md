# WPF WebView Web Frontend

React + TypeScript 前端项目，用于 WPF WebView2 集成。

## 技术栈

- **React 18** - UI 框架
- **TypeScript** - 类型安全
- **Vite** - 构建工具和开发服务器

## 项目结构

```
WpfWebview.Web/
├── src/
│   ├── App.tsx              # 主应用组件
│   ├── App.css              # 应用样式
│   ├── main.tsx             # 应用入口
│   ├── lib/
│   │   └── wpf-bridge.ts    # WPF Bridge 封装
│   └── types/
│       └── wpf-bridge.d.ts  # WPF Bridge 类型定义
├── index.html               # HTML 入口
├── package.json             # 项目配置
├── tsconfig.json            # TypeScript 配置
└── vite.config.ts           # Vite 配置
```

## 安装依赖

```bash
pnpm install
```

## 开发

启动开发服务器：

```bash
pnpm dev
```

开发服务器会在 `http://localhost:5173` 启动，并自动将 URL 写入 `.vite-dev-server` 文件供 WPF 应用读取。

## 构建

构建生产版本：

```bash
pnpm build
```

构建输出在 `dist/` 目录。

## 类型检查

运行 TypeScript 类型检查：

```bash
pnpm type-check
```

## WPF Bridge API

### 发送消息到 WPF

```typescript
import { initWpfBridge } from './lib/wpf-bridge';

// 初始化 bridge
initWpfBridge();

// 发送消息
if (window.wpfBridge) {
  window.wpfBridge.sendMessage('Hello from React!');
}
```

### 接收来自 WPF 的消息

```typescript
window.onWpfMessage = (message: { type: string; data: string }) => {
  console.log('收到 WPF 消息:', message.data);
};
```

### 调用 WPF 方法

```typescript
import { callWpfMethod } from './lib/wpf-bridge';

// 调用 WPF 方法
const result = await callWpfMethod('ShowMessage', 'Hello from React!');
console.log('WPF 响应:', result);
```

## 注意事项

- Bridge 会在应用启动时自动初始化
- 在非 WebView2 环境中，bridge 会创建 mock 实现用于开发测试
- 所有与 WPF 的通信都通过 `window.wpfBridge` 和 `window.chrome.webview` API
