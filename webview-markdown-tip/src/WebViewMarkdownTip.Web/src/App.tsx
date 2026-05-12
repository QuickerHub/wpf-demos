import { useCallback, useEffect, useMemo, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import 'github-markdown-css/github-markdown.css';
import './App.css';

export type MarkdownPayload = {
  markdown: string;
  title?: string;
};

function getWebViewPostMessage(): ((message: string) => void) | undefined {
  return (
    window as unknown as {
      chrome?: { webview?: { postMessage: (message: string) => void } };
    }
  ).chrome?.webview?.postMessage;
}

function postCloseToHost(): boolean {
  const post = getWebViewPostMessage();
  if (!post) {
    return false;
  }
  post(JSON.stringify({ type: 'action', action: 'close' }));
  return true;
}

// Shown when opening the Vite page directly; WPF replaces this after navigation via receiveMarkdownPayload.
const defaultMarkdown = `## Markdown 预览（本地演示）

这是 **直接打开前端** 时的示例正文，用于检查排版与主题；嵌入 **WebView2** 后会被宿主注入的内容替换。

### 功能预览

| 项目 | 说明 |
| --- | --- |
| GFM | 表格、任务列表、删除线 |
| 代码 | 行内 \`code\` 与高亮块 |

- [x] \`remark-gfm\` 已启用  
- [ ] 宿主注入后即切换为业务文案  

> 引用块：浅色 / 深色主题随系统 **prefers-color-scheme** 变化。

---

#### 列表示例

1. 有序条目  
2. **粗体** 与 *斜体*

\`\`\`typescript
function greet(name: string): string {
  return \`Hello, \${name}\`;
}
\`\`\`

---

*提示：在浏览器中打开 \`http://localhost:5174\` 可查看本演示；从 WPF WebView2 注入 markdown 后，正文将替换为实际提示内容。*`;

declare global {
  interface Window {
    receiveMarkdownPayload?: (payload: MarkdownPayload) => void;
  }
}

export default function App() {
  const [markdown, setMarkdown] = useState(defaultMarkdown);

  const applyPayload = useCallback((payload: MarkdownPayload) => {
    setMarkdown(payload.markdown ?? '');
    if (payload.title) {
      document.title = payload.title;
    }
  }, []);

  useEffect(() => {
    window.receiveMarkdownPayload = (payload) => {
      applyPayload(payload);
    };
    return () => {
      delete window.receiveMarkdownPayload;
    };
  }, [applyPayload]);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key !== 'Escape') {
        return;
      }
      if (!postCloseToHost()) {
        return;
      }
      e.preventDefault();
    };
    document.addEventListener('keydown', onKeyDown, true);
    return () => document.removeEventListener('keydown', onKeyDown, true);
  }, []);

  const extraPlugins = useMemo(() => [remarkGfm], []);

  return (
    <div className="md-tip-root">
      <main className="md-tip-main markdown-body">
        <ReactMarkdown remarkPlugins={extraPlugins}>{markdown}</ReactMarkdown>
      </main>
    </div>
  );
}
