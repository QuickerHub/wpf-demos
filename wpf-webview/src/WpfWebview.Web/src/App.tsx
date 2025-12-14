import { useState, useEffect, useRef, useCallback } from 'react';
import { initWpfBridge, callWpfMethod } from './lib/wpf-bridge';
import './App.css';

interface LogEntry {
  time: string;
  message: string;
}

function App() {
  const [messageToWpf, setMessageToWpf] = useState('');
  const [messageFromWpf, setMessageFromWpf] = useState('');
  const [wpfResponse, setWpfResponse] = useState('');
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [isSending, setIsSending] = useState(false);
  const logRef = useRef<HTMLDivElement>(null);

  // Add log entry
  const addLog = useCallback((message: string) => {
    const time = new Date().toLocaleTimeString();
    setLogs((prev: LogEntry[]) => [...prev, { time, message }]);
  }, []);

  // Scroll log to bottom
  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [logs]);

  // Initialize WPF Bridge with callbacks
  useEffect(() => {
    initWpfBridge(
      // Log callback - all bridge logs will come here
      (logMessage: string) => {
        addLog(logMessage);
      },
      // Message callback - received messages from WPF
      (messageText: string) => {
        setMessageFromWpf(`收到 WPF 消息: ${messageText}`);
      }
    );
  }, [addLog]);

  // Send message to WPF
  const handleSendToWpf = useCallback(() => {
    if (isSending) return;
    setIsSending(true);

    const message = messageToWpf.trim() || 'Hello from WebView!';
    
    // Bridge handles all checks and logging internally
    window.wpfBridge?.sendMessage(message);
    setMessageToWpf('');
    
    setTimeout(() => setIsSending(false), 200);
  }, [messageToWpf, isSending]);

  // Call WPF method
  const handleCallWpfMethod = useCallback(async () => {
    try {
      // Bridge handles all checks and logging internally
      const result = await callWpfMethod('ShowMessage', '这是从 JavaScript 调用的 C# 方法！');
      if (result) {
        setWpfResponse(`WPF 响应: ${result}`);
      }
    } catch (error) {
      // Error already logged by bridge
    }
  }, []);

  // Handle Enter key in input
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>): void => {
      if (e.key === 'Enter') {
        handleSendToWpf();
      }
    },
    [handleSendToWpf]
  );

  return (
    <div className="container">
      <h1>🌐 WPF WebView 交互演示</h1>

      <div className="section">
        <h2>📤 发送消息到 WPF</h2>
        <div className="input-group">
          <input
            type="text"
            value={messageToWpf}
            onChange={(e) => setMessageToWpf(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="输入要发送到 WPF 的消息"
          />
          <button className="primary" onClick={handleSendToWpf} disabled={isSending}>
            发送到 WPF
          </button>
          <button className="secondary" onClick={handleCallWpfMethod}>
            调用 WPF 方法
          </button>
        </div>
        <div className="message-box">{wpfResponse || '等待响应...'}</div>
      </div>

      <div className="section">
        <h2>📥 接收来自 WPF 的消息</h2>
        <div className="message-box">{messageFromWpf || '等待消息...'}</div>
      </div>

      <div className="section">
        <h2>📋 交互日志</h2>
        <div className="log" ref={logRef}>
          {logs.map((log, index) => (
            <div key={index}>
              [{log.time}] {log.message}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default App;

