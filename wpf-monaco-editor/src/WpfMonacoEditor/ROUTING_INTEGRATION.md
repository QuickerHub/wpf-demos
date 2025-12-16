# 路由系统集成说明

## 修改内容

### DiffEditorViewModel.cs

已更新 `DiffEditorViewModel` 以使用新的路由系统：

1. **InitializeAsync 方法**
   - 初始化 WebView 后，自动导航到 `/diff` 页面
   - 使用 `window.wpfRouter.navigate('/diff')` 或回退到 `window.location.hash = '#/diff'`
   - 等待路由导航完成后再设置内容

2. **SetContentAsync 方法**
   - 在设置内容前，检查当前是否在 `/diff` 页面
   - 如果不在，先导航到 `/diff` 页面
   - 确保内容设置到正确的页面

## 工作流程

### 初始化流程

```
1. DiffEditorWindow 创建
   ↓
2. DiffEditorViewModel.InitializeAsync() 被调用
   ↓
3. WebViewManager 初始化 WebView2
   ↓
4. WebView 加载完成 (NavigationCompleted)
   ↓
5. 导航到 /diff 页面 (使用路由 API)
   ↓
6. 等待路由导航完成
   ↓
7. 设置主题和内容
   ↓
8. DiffEditorPage 组件加载并接收内容
```

### 更新内容流程

```
1. Runner.ShowDiffEditor() 或 UpdateContentAsync() 被调用
   ↓
2. DiffEditorViewModel.SetContentAsync() 被调用
   ↓
3. 检查当前路由是否为 /diff
   ↓
4. 如果不在 /diff，先导航到 /diff
   ↓
5. 设置编辑器内容
```

## 使用方式

### Runner 使用（无需修改）

Runner 的使用方式保持不变：

```csharp
// 显示 Diff Editor
Runner.ShowDiffEditor(
    originalText: "原始文本",
    modifiedText: "修改后的文本",
    language: "csharp",
    editorId: "editor1"  // 可选，用于复用窗口
);
```

### 直接使用 DiffEditorWindow

```csharp
var window = new DiffEditorWindow();
await window.InitializeAsync(originalText, modifiedText, language);

// 更新内容
await window.UpdateContentAsync(newOriginal, newModified, "javascript");
```

## 路由页面

DiffEditor 现在使用 `/diff` 路由页面：

- **前端路由**: `#/diff`
- **WPF 导航**: 自动导航到 `/diff` 页面
- **页面组件**: `DiffEditorPage.tsx`

## 兼容性

- ✅ 向后兼容：如果路由 API 不可用，会回退到直接 hash 导航
- ✅ 内容设置：支持在编辑器就绪前和就绪后设置内容
- ✅ 多窗口：每个 DiffEditorWindow 独立管理自己的 WebView

## 注意事项

1. **路由初始化时间**
   - 路由系统需要时间初始化（约 100-200ms）
   - 代码中已添加适当的延迟等待

2. **内容设置时机**
   - 如果编辑器已就绪，立即设置内容
   - 如果编辑器未就绪，内容存储在 `window.pendingDiffContent` 中
   - DiffEditorPage 会在编辑器就绪时自动加载待处理内容

3. **主题同步**
   - 主题会在导航完成后自动发送到前端
   - 使用 `SendThemeToWebAsync()` 方法

## 测试建议

1. **基本功能测试**
   ```csharp
   Runner.ShowDiffEditor("原始", "修改", "plaintext");
   ```

2. **更新内容测试**
   ```csharp
   var window = new DiffEditorWindow();
   await window.InitializeAsync("原始1", "修改1", "csharp");
   await Task.Delay(1000);
   await window.UpdateContentAsync("原始2", "修改2", "javascript");
   ```

3. **多窗口测试**
   ```csharp
   Runner.ShowDiffEditor("文本1", "文本2", "plaintext", "editor1");
   Runner.ShowDiffEditor("文本3", "文本4", "plaintext", "editor2");
   ```

## 故障排除

### 问题：内容没有显示

**可能原因**：
- 路由导航未完成
- Monaco Editor 未就绪

**解决方案**：
- 检查浏览器控制台是否有错误
- 增加延迟等待时间
- 检查 `window.monacoDiffEditor` 是否存在

### 问题：路由导航失败

**可能原因**：
- `window.wpfRouter` 未初始化
- 路由系统未加载

**解决方案**：
- 代码已包含回退机制（直接 hash 导航）
- 检查前端路由是否正确初始化

