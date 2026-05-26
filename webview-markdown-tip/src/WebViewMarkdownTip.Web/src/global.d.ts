/// <reference types="vite/client" />

export {};

declare global {
  interface Window {
    receiveMarkdownPayload?: (payload: { markdown: string; title?: string }) => void;
    chrome?: {
      webview?: {
        postMessage(message: string): void;
      };
    };
  }
}
