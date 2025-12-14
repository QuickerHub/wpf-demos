/**
 * Type definitions for WPF Bridge
 */

// WebView2 Chrome API types
interface ChromeWebView {
  postMessage: (message: string) => void;
  hostObjects?: {
    wpfHost?: {
      ShowMessage: (message: string) => Promise<string>;
      GetCurrentTime: () => Promise<string>;
    };
  };
}

interface Chrome {
  webview?: ChromeWebView;
}

interface Window {
  chrome?: Chrome;
  wpfBridge?: {
    sendMessage: (message: string | object) => boolean;
    isReady: () => boolean;
  };
  receiveFromWpf?: (messageObj: { type: string; data: string }) => void;
  onWpfMessage?: (messageObj: { type: string; data: string }) => void;
  monacoDiffEditor?: {
    setOriginalText: (text: string) => void;
    setModifiedText: (text: string) => void;
    getOriginalText: () => string;
    getModifiedText: () => string;
    getOriginalModel: () => import('monaco-editor').editor.ITextModel | null;
    getModifiedModel: () => import('monaco-editor').editor.ITextModel | null;
    setLanguage: (language: string) => void;
  };
  pendingDiffContent?: {
    original: string;
    modified: string;
    language?: string;
  };
  MonacoEnvironment?: {
    getWorkerUrl: (moduleId: string, label: string) => string;
  };
  wpfTheme?: string;
  setWpfTheme?: (theme: string) => void;
}

declare const self: Window & typeof globalThis;

