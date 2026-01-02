# 打包体积分析报告

## 当前打包体积（根据之前分析）

| 文件 | 大小 | 说明 |
|------|------|------|
| ts.worker | ~6.7 MB | TypeScript/JavaScript 语言服务 Worker |
| monaco-vendor | ~4.1 MB | Monaco Editor 核心库 |
| css.worker | ~1.0 MB | CSS 语言服务 Worker |
| html.worker | ~677 KB | HTML 语言服务 Worker |
| json.worker | ~375 KB | JSON 语言服务 Worker |
| editor.worker | ~246 KB | 编辑器基础 Worker |
| react-vendor | ~172 KB | React 库 |
| monaco-vendor.css | ~143 KB | Monaco Editor 样式 |
| codicon.ttf | ~119 KB | 图标字体 |
| index.js | ~19 KB | 应用代码 |
| index.css | ~4 KB | 应用样式 |

**总计：约 14.8 MB**

## 主要问题

1. **ts.worker 过大（6.7MB）**：包含完整的 TypeScript 语言服务
2. **所有 Workers 都被打包**：即使可能不需要所有语言支持
3. **Monaco Editor 完整打包**：包含所有语言和功能

## 优化建议

### 1. 按需加载 Workers（最重要）

当前所有 workers 都被打包，可以改为按需加载：

```typescript
// 只加载实际使用的语言 workers
// 如果用户只使用 plaintext，就不需要加载 ts.worker
```

### 2. 使用压缩插件

添加 vite-plugin-compression 进行 gzip/brotli 压缩：

```bash
pnpm add -D vite-plugin-compression
```

### 3. 检查未使用的依赖

- `react-router-dom` - 如果路由简单，可以考虑移除
- 检查是否有其他未使用的依赖

### 4. 优化 Vite 配置

- 启用更激进的 tree-shaking
- 使用 terser 替代 esbuild（可能更小的体积）
- 检查是否有重复的代码

### 5. Monaco Editor 配置优化

- 禁用不需要的功能（如某些语言服务）
- 考虑使用更轻量的编辑器（如果功能需求不高）

## 预期优化效果

- 按需加载 workers：可减少 50-70% 体积（如果只使用部分语言）
- 压缩：可减少 60-80% 传输体积（gzip/brotli）
- 优化配置：可减少 10-20% 体积

