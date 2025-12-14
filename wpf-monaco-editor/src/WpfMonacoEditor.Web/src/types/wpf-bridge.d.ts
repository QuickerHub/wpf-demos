/**
 * Type definitions for WPF Bridge
 */

interface Window {
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
  };
}

