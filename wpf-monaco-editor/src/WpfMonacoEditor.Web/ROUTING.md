# 路由系统使用指南

## 概述

本项目使用 React Router 实现多页面路由，同时适配网页浏览器和 WPF WebView2。

## 路由配置

### 使用 HashRouter

项目使用 `HashRouter` 而不是 `BrowserRouter`，原因：
- **WebView 兼容性**：WebView2 不需要服务器端路由配置
- **部署简单**：可以直接打开 HTML 文件，不需要 Web 服务器
- **URL 格式**：使用 `#/path` 格式，例如 `index.html#/diff`

### 可用路由

- `/` 或 `#/` - 首页（导航页面）
- `/diff` 或 `#/diff` - Diff Editor 页面（并排对比视图）
- `/editor` 或 `#/editor` - Code Editor 页面（单编辑器视图）

## WPF 端使用

### JavaScript API

路由系统通过 `window.wpfRouter` 对象暴露以下 API：

#### 1. 导航到指定页面

```javascript
// 导航到 Diff Editor 页面
window.wpfRouter.navigate('/diff');

// 导航到 Code Editor 页面
window.wpfRouter.navigate('/editor');

// 导航到首页
window.wpfRouter.navigate('/');
```

#### 2. 获取当前路径

```javascript
const currentPath = window.wpfRouter.getCurrentPath();
console.log(currentPath); // 例如: '/diff'
```

#### 3. 浏览器历史记录导航

```javascript
// 后退
window.wpfRouter.goBack();

// 前进
window.wpfRouter.goForward();
```

### C# 端调用示例

```csharp
// 导航到 Diff Editor 页面
await webView.CoreWebView2.ExecuteScriptAsync(
    "window.wpfRouter.navigate('/diff');"
);

// 获取当前路径
var currentPath = await webView.CoreWebView2.ExecuteScriptAsync(
    "window.wpfRouter.getCurrentPath();"
);
var path = JsonConvert.DeserializeObject<string>(currentPath);

// 监听路由变化（可选）
webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
webView.CoreWebView2.NavigationCompleted += async (sender, e) =>
{
    var path = await webView.CoreWebView2.ExecuteScriptAsync(
        "window.wpfRouter.getCurrentPath();"
    );
    Console.WriteLine($"Current route: {path}");
};
```

## 网页端使用

### 直接访问

在浏览器中打开：
- `http://localhost:5174/` - 首页
- `http://localhost:5174/#/diff` - Diff Editor
- `http://localhost:5174/#/editor` - Code Editor

### 使用 Link 组件

在 React 组件中使用 `Link` 组件进行导航：

```tsx
import { Link } from 'react-router-dom';

<Link to="/diff">Go to Diff Editor</Link>
<Link to="/editor">Go to Code Editor</Link>
```

### 使用 useNavigate Hook

在 React 组件中使用编程式导航：

```tsx
import { useNavigate } from 'react-router-dom';

function MyComponent() {
  const navigate = useNavigate();
  
  const handleClick = () => {
    navigate('/diff');
  };
  
  return <button onClick={handleClick}>Go to Diff</button>;
}
```

## 添加新页面

### 1. 创建页面组件

在 `src/pages/` 目录下创建新页面组件：

```tsx
// src/pages/MyNewPage.tsx
export default function MyNewPage() {
  return <div>My New Page</div>;
}
```

### 2. 添加路由

在 `src/router/AppRouter.tsx` 中添加路由：

```tsx
import MyNewPage from '../pages/MyNewPage';

// 在 Routes 中添加
<Route path="/mynewpage" element={<MyNewPage />} />
```

### 3. 使用新路由

- 网页端：访问 `http://localhost:5174/#/mynewpage`
- WPF 端：调用 `window.wpfRouter.navigate('/mynewpage')`

## 注意事项

1. **HashRouter 路径格式**：所有路径都以 `#` 开头，例如 `#/diff`
2. **路径匹配**：路由路径区分大小写
3. **默认路由**：未知路径会自动重定向到首页 `/`
4. **WPF API 可用性**：`window.wpfRouter` 在页面加载完成后才可用

## 故障排除

### WPF 无法导航

确保：
1. 页面已完全加载（监听 `NavigationCompleted` 事件）
2. `window.wpfRouter` 对象存在（检查控制台）

### 路由不工作

检查：
1. 是否正确使用 HashRouter（不是 BrowserRouter）
2. 路径是否正确（包含前导斜杠 `/`）
3. 浏览器控制台是否有错误

