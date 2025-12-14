/**
 * Type definitions for WPF WebView2 Bridge
 */

export interface WpfBridge {
  /**
   * Send message to WPF application
   * @param message - Message to send (string or object)
   * @returns true if message was sent successfully
   */
  sendMessage(message: string | object): boolean;

  /**
   * Check if bridge is ready
   * @returns true if bridge is ready
   */
  isReady(): boolean;
}

export interface WpfMessage {
  type: string;
  data: string;
}

interface ChromeWebView {
  postMessage(message: string): void;
  hostObjects?: {
    wpfHost?: {
      ShowMessage(message: string): Promise<string>;
      GetCurrentTime(): Promise<string>;
    };
  };
}

declare global {
  interface Window {
    wpfBridge?: WpfBridge;
    receiveFromWpf?: (message: WpfMessage) => void;
    onWpfMessage?: (message: WpfMessage) => void;
    chrome?: {
      webview?: ChromeWebView;
    };
  }
}

export {};

