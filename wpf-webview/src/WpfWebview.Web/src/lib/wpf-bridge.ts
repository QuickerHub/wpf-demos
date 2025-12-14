/**
 * WPF WebView2 Bridge
 * 
 * This bridge provides communication between JavaScript and WPF application.
 * It uses WebView2's native APIs (chrome.webview.postMessage and chrome.webview.hostObjects).
 */

type LogCallback = (message: string) => void;
type MessageCallback = (message: string) => void;

let logCallback: LogCallback | null = null;
let messageCallback: MessageCallback | null = null;

/**
 * Internal logging function
 */
function log(message: string): void {
  const time = new Date().toLocaleTimeString();
  const logMessage = `[${time}] ${message}`;
  console.log(logMessage);
  if (logCallback) {
    logCallback(logMessage);
  }
}

/**
 * Check if running in WebView2 environment
 */
function isWebView2(): boolean {
  return (
    typeof window.chrome !== 'undefined' &&
    window.chrome.webview !== undefined &&
    window.chrome.webview.postMessage !== undefined
  );
}

/**
 * Initialize WPF Bridge
 * @param onLog - Optional callback for log messages
 * @param onMessage - Optional callback for received messages
 */
export function initWpfBridge(onLog?: LogCallback, onMessage?: MessageCallback): void {
  logCallback = onLog || null;
  messageCallback = onMessage || null;

  if (!isWebView2()) {
    log('⚠️ WPF Bridge: Not running in WebView2 environment (using mock mode)');
    // Create a mock bridge for development/testing outside WebView2
    window.wpfBridge = {
      sendMessage: (message: string | object): boolean => {
        log(`[Mock] 发送消息到 WPF: ${typeof message === 'string' ? message : JSON.stringify(message)}`);
        return false;
      },
      isReady: (): boolean => false,
    };
    log('❌ WPF Bridge 初始化失败（非 WebView2 环境）');
    return;
  }

  /**
   * WPF Bridge object
   * Provides methods to communicate with WPF application
   */
  window.wpfBridge = {
    /**
     * Send message to WPF application
     */
    sendMessage: (message: string | object): boolean => {
      try {
        // Convert message to string if needed
        const messageStr =
          message === null || message === undefined
            ? ''
            : typeof message === 'string'
            ? message
            : JSON.stringify(message);

        // Create message object
        const messageObj: { type: string; data: string } = {
          type: 'message',
          data: messageStr,
        };

        // Send message via WebView2 postMessage API
        const jsonStr = JSON.stringify(messageObj);
        window.chrome!.webview!.postMessage(jsonStr);

        log(`📤 发送消息到 WPF: ${messageStr}`);
        return true;
      } catch (error) {
        log(`❌ 发送消息失败: ${error instanceof Error ? error.message : String(error)}`);
        return false;
      }
    },

    /**
     * Check if bridge is ready
     */
    isReady: (): boolean => isWebView2(),
  };

  /**
   * Function for WPF to call to send messages to JavaScript
   * This function is called by WPF via ExecuteScriptAsync
   */
  window.receiveFromWpf = (messageObj: { type: string; data: string }): void => {
    try {
      const messageText = messageObj?.data || '';
      log(`📥 收到 WPF 消息: ${messageText}`);
      if (messageCallback) {
        messageCallback(messageText);
      }
      if (typeof window.onWpfMessage === 'function') {
        window.onWpfMessage(messageObj);
      }
    } catch (error) {
      log(`❌ 接收 WPF 消息失败: ${error instanceof Error ? error.message : String(error)}`);
    }
  };

  // Dispatch custom event to notify that bridge is ready
  if (typeof window.dispatchEvent !== 'undefined') {
    window.dispatchEvent(
      new CustomEvent('wpfBridgeReady', {
        detail: { bridge: window.wpfBridge },
      })
    );
  }

  log('✅ WPF Bridge 初始化成功');
}

/**
 * Call WPF method via Host Objects
 */
export async function callWpfMethod(
  methodName: 'ShowMessage' | 'GetCurrentTime',
  ...args: string[]
): Promise<string | null> {
  try {
    if (!window.chrome?.webview?.hostObjects?.wpfHost) {
      log('❌ Host Objects 不可用');
      return null;
    }

    const wpfHost = window.chrome.webview.hostObjects.wpfHost;
    let result: string | null = null;

    if (methodName === 'ShowMessage' && args.length > 0) {
      log(`🔧 调用 WPF 方法: ShowMessage("${args[0]}")`);
      result = await wpfHost.ShowMessage(args[0]);
    } else if (methodName === 'GetCurrentTime') {
      log('🔧 调用 WPF 方法: GetCurrentTime()');
      result = await wpfHost.GetCurrentTime();
    }

    if (result) {
      log(`✅ 调用 WPF 方法成功，返回值: ${result}`);
    } else {
      log('⚠️ WPF 方法返回空值');
    }

    return result;
  } catch (error) {
    log(`❌ 调用 WPF 方法失败: ${error instanceof Error ? error.message : String(error)}`);
    throw error;
  }
}

