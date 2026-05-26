import { useCallback, useEffect, useMemo, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import rehypeKatex from 'rehype-katex';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import 'github-markdown-css/github-markdown.css';
import 'katex/dist/katex.min.css';
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

function postUiReadyToHost(): void {
  const post = getWebViewPostMessage();
  if (!post) {
    return;
  }
  post(JSON.stringify({ type: 'host', action: 'uiReady' }));
}

// Shown only in Vite dev when opening the page in a browser or before host injects markdown.
const devDemoMarkdown = `## Markdown 预览（本地演示）

这是 **直接打开前端** 时的示例正文，用于检查排版与主题；嵌入 **WebView2** 后会被宿主注入的内容替换。

### 功能预览

| 项目 | 说明 |
| --- | --- |
| GFM | 表格、任务列表、删除线 |
| 代码 | 行内 \`code\` 与高亮块 |
| 公式 | 行内与块级 LaTeX |

- [x] \`remark-gfm\` 已启用  
- [x] KaTeX 公式渲染已启用  
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

行内公式示例：$E = mc^2$，以及 $\int_0^1 x^2\,dx = \frac{1}{3}$。

块级公式示例：

$$
\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
$$

---

*提示：在浏览器中打开 \`http://localhost:5174\` 可查看本演示；从 WPF WebView2 注入 markdown 后，正文将替换为实际提示内容。*`;

const defaultMarkdown = import.meta.env.DEV ? devDemoMarkdown : '';

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
    postUiReadyToHost();
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

  const remarkPlugins = useMemo(() => [remarkGfm, remarkMath], []);
  const rehypePlugins = useMemo(() => [rehypeKatex], []);

  return (
    <div className="md-tip-root">
      <main className="md-tip-main markdown-body">
        <ReactMarkdown remarkPlugins={remarkPlugins} rehypePlugins={rehypePlugins}>
          {markdown}
        </ReactMarkdown>
      </main>
    </div>
  );
}
