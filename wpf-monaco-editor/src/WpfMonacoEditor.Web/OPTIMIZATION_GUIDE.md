# 打包体积优化指南

## 当前问题

打包体积约 **14.8 MB**，主要来自：
- `ts.worker`: 6.7 MB (TypeScript 语言服务)
- `monaco-vendor`: 4.1 MB (Monaco Editor 核心)
- 其他 workers: ~2.3 MB

## 已实施的优化

1. ✅ **代码分割**：React、Monaco、Router 分别打包
2. ✅ **禁用 sourcemap**：减少构建产物大小
3. ✅ **CSS 代码分割**：样式文件独立打包

## 进一步优化建议

### 1. 使用压缩（推荐）

安装压缩插件，在服务器端启用 gzip/brotli 压缩：

```bash
# 可选：添加压缩插件（用于生成压缩文件）
pnpm add -D vite-plugin-compression
```

然后在服务器配置中启用 gzip/brotli 压缩，可减少 **60-80%** 的传输体积。

### 2. 使用 Terser（可选）

Terser 可能比 esbuild 产生更小的包：

```bash
pnpm add -D terser
```

然后在 `vite.config.ts` 中：
```typescript
minify: 'terser',
terserOptions: {
  compress: {
    drop_console: true,
    drop_debugger: true,
  },
}
```

### 3. 移除未使用的语言支持（如果可能）

如果应用只使用部分语言，可以考虑：
- 只加载需要的 workers
- 但这需要修改 Monaco Editor 的加载方式，可能比较复杂

### 4. 考虑使用 CDN（不推荐）

对于 WPF 应用，本地打包更合适，CDN 不适合。

## 实际优化效果

- **当前体积**：~14.8 MB（未压缩）
- **启用 gzip 后**：~3-5 MB（传输体积）
- **启用 brotli 后**：~2-4 MB（传输体积）

## 注意事项

Monaco Editor 本身就是一个大型库，包含完整的编辑器功能和语言服务。6-7 MB 的 TypeScript worker 是正常的，因为它包含完整的 TypeScript 编译器。

如果体积仍然是问题，可以考虑：
1. 使用更轻量的编辑器（如 CodeMirror）
2. 只加载实际使用的语言 workers（需要自定义 Monaco 加载逻辑）

