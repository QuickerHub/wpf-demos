import { useEffect, useRef, useState } from 'react';
import { initWpfBridge } from '../lib/wpf-bridge';
import type { editor } from 'monaco-editor';
import DiffEditorView from '../components/DiffEditorView';
import '../App.css';

/**
 * DiffEditor Page - Shows side-by-side diff view
 */
export default function DiffEditorPage() {
  const monacoRef = useRef<typeof import('monaco-editor') | null>(null);
  const [theme, setTheme] = useState<'vs' | 'vs-dark'>('vs-dark');
  const wpfThemeRef = useRef<string | null>(null);

  // Function to update Monaco Editor theme
  const updateMonacoTheme = (newTheme: 'vs' | 'vs-dark') => {
    setTheme(newTheme);
    if (monacoRef.current) {
      monacoRef.current.editor.setTheme(newTheme);
      
      // Update body background color based on theme
      if (newTheme === 'vs') {
        document.body.style.backgroundColor = '#ffffff';
        document.body.classList.add('light-theme');
        document.body.classList.remove('dark-theme');
      } else {
        document.body.style.backgroundColor = '#1e1e1e';
        document.body.classList.add('dark-theme');
        document.body.classList.remove('light-theme');
      }
    }
  };

  // Expose function for WPF to set theme
  useEffect(() => {
    // Function to receive theme from WPF
    (window as any).setWpfTheme = (wpfTheme: string) => {
      wpfThemeRef.current = wpfTheme;
      const monacoTheme = wpfTheme === 'dark' ? 'vs-dark' : 'vs';
      updateMonacoTheme(monacoTheme);
    };

    // Check if WPF theme was set before this component loaded
    if ((window as any).wpfTheme) {
      const wpfTheme = (window as any).wpfTheme;
      wpfThemeRef.current = wpfTheme;
      const monacoTheme = wpfTheme === 'dark' ? 'vs-dark' : 'vs';
      updateMonacoTheme(monacoTheme);
    }

    return () => {
      if ((window as any).setWpfTheme) {
        delete (window as any).setWpfTheme;
      }
    };
  }, []);

  // Detect and listen to system theme changes (only if WPF theme is not set)
  useEffect(() => {
    // Only use system theme if WPF theme is not set
    if (wpfThemeRef.current) {
      return; // WPF theme takes priority
    }

    // Get initial theme
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const updateTheme = (e: MediaQueryList | MediaQueryListEvent) => {
      const newTheme = e.matches ? 'vs-dark' : 'vs';
      updateMonacoTheme(newTheme);
    };

    // Set initial theme
    updateTheme(mediaQuery);

    // Listen for theme changes
    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener('change', updateTheme);
      return () => {
        mediaQuery.removeEventListener('change', updateTheme);
      };
    } else {
      // Fallback for older browsers
      mediaQuery.addListener(updateTheme);
      return () => {
        mediaQuery.removeListener(updateTheme);
      };
    }
  }, []);

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

    // Expose Monaco DiffEditor API to window for WPF to call
    return () => {
      if (window.monacoDiffEditor) {
        delete window.monacoDiffEditor;
      }
    };
  }, []);

  const handleEditorReady = (
    _diffEditor: editor.IStandaloneDiffEditor,
    monaco: typeof import('monaco-editor')
  ) => {
    monacoRef.current = monaco;

    // Set theme based on WPF theme (if available) or system preference
    if (wpfThemeRef.current) {
      const isDark = wpfThemeRef.current === 'dark';
      const monacoTheme = isDark ? 'vs-dark' : 'vs';
      monaco.editor.setTheme(monacoTheme);
      setTheme(isDark ? 'vs-dark' : 'vs');
      
      // Update body background color
      if (isDark) {
        document.body.style.backgroundColor = '#1e1e1e';
        document.body.classList.add('dark-theme');
        document.body.classList.remove('light-theme');
      } else {
        document.body.style.backgroundColor = '#ffffff';
        document.body.classList.add('light-theme');
        document.body.classList.remove('dark-theme');
      }
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      const monacoTheme = prefersDark ? 'vs-dark' : 'vs';
      monaco.editor.setTheme(monacoTheme);
      setTheme(prefersDark ? 'vs-dark' : 'vs');
      
      // Update body background color
      if (prefersDark) {
        document.body.style.backgroundColor = '#1e1e1e';
        document.body.classList.add('dark-theme');
        document.body.classList.remove('light-theme');
      } else {
        document.body.style.backgroundColor = '#ffffff';
        document.body.classList.add('light-theme');
        document.body.classList.remove('dark-theme');
      }
    }
  };

  return (
    <div className="monaco-container">
      <DiffEditorView theme={theme} onEditorReady={handleEditorReady} />
    </div>
  );
}

