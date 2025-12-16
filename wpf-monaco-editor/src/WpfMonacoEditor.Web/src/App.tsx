import { useEffect } from 'react';
import { initWpfBridge } from './lib/wpf-bridge';
import AppRouter from './router/AppRouter';
import './App.css';

/**
 * Main App Component - Initializes WPF Bridge and provides routing
 */
function App() {
  useEffect(() => {
    // Initialize WPF Bridge
    initWpfBridge(
      // Log callback
      (logMessage: string) => {
        console.log(logMessage);
      },
      // Message callback
      (messageText: string) => {
        console.log('收到 WPF 消息:', messageText);
      }
    );
  }, []);

  return <AppRouter />;
}

export default App;

