/**
 * Monaco Editor Web Workers 配置
 * 
 * Web Workers 是什么？
 * - Web Workers 是浏览器提供的多线程机制，允许在后台线程中执行 JavaScript
 * - 它们运行在独立的线程中，不会阻塞主线程（UI 线程）
 * 
 * 为什么 Monaco Editor 需要 Workers？
 * Monaco Editor 将以下计算密集型任务交给 Workers 处理：
 * 1. 语法高亮（Syntax Highlighting）- 解析代码并应用颜色
 * 2. 代码补全（IntelliSense）- 分析代码结构，提供智能提示
 * 3. 语法检查（Linting）- 检查代码错误和警告
 * 4. 代码格式化（Formatting）- 格式化代码
 * 5. 代码折叠（Folding）- 处理代码块的折叠逻辑
 * 
 * 如果不使用 Workers：
 * - 这些任务会在主线程执行，导致 UI 卡顿
 * - 用户输入时会有明显的延迟
 * - 大文件编辑时可能造成页面无响应
 * 
 * 使用 Workers 的好处：
 * - 主线程保持流畅，UI 响应迅速
 * - 可以并行处理多个任务
 * - 提供类似 VS Code 的流畅编辑体验
 */

import { loader } from '@monaco-editor/react';
import * as monaco from 'monaco-editor';

// 配置 Monaco Editor 使用本地包，而不是从 CDN 加载
loader.config({ monaco });

// 配置 Worker 路径
// Vite 会自动将这些 worker 文件打包为独立的 chunk
(window as any).MonacoEnvironment = {
  getWorker: function (_moduleId: string, label: string) {
    // 使用 Vite 的 ?worker 语法来加载 workers
    // 这样 Vite 会自动处理 worker 的打包和路径
    const getWorkerModule = (moduleUrl: string, label: string) => {
      return new Worker(
        new URL(moduleUrl, import.meta.url),
        { name: label, type: 'module' }
      );
    };

    switch (label) {
      case 'json':
        return getWorkerModule('monaco-editor/esm/vs/language/json/json.worker', label);
      case 'css':
      case 'scss':
      case 'less':
        return getWorkerModule('monaco-editor/esm/vs/language/css/css.worker', label);
      case 'html':
      case 'handlebars':
      case 'razor':
        return getWorkerModule('monaco-editor/esm/vs/language/html/html.worker', label);
      case 'typescript':
      case 'javascript':
        return getWorkerModule('monaco-editor/esm/vs/language/typescript/ts.worker', label);
      default:
        return getWorkerModule('monaco-editor/esm/vs/editor/editor.worker', label);
    }
  }
};

export default monaco;
