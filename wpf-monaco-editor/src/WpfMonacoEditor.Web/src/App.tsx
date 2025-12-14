import { useEffect, useRef } from 'react';
import Editor from '@monaco-editor/react';
import { initWpfBridge } from './lib/wpf-bridge';
import type { editor } from 'monaco-editor';
import './App.css';

function App() {
  const diffEditorRef = useRef<editor.IStandaloneDiffEditor | null>(null);

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

  const handleEditorDidMount = (
    editor: editor.IStandaloneDiffEditor,
    monaco: typeof import('monaco-editor')
  ): void => {
    diffEditorRef.current = editor;

    // Expose API to window for WPF integration
    window.monacoDiffEditor = {
      setOriginalText: (text: string) => {
        const originalModel = editor.getOriginalEditor().getModel();
        if (originalModel) {
          originalModel.setValue(text);
        }
      },
      setModifiedText: (text: string) => {
        const modifiedModel = editor.getModifiedEditor().getModel();
        if (modifiedModel) {
          modifiedModel.setValue(text);
        }
      },
      getOriginalText: () => {
        const originalModel = editor.getOriginalEditor().getModel();
        return originalModel?.getValue() || '';
      },
      getModifiedText: () => {
        const modifiedModel = editor.getModifiedEditor().getModel();
        return modifiedModel?.getValue() || '';
      },
    };

    // Set default theme based on system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    monaco.editor.setTheme(prefersDark ? 'vs-dark' : 'vs');
  };

  return (
    <div className="monaco-container">
      <Editor
        height="100vh"
        defaultLanguage="plaintext"
        theme="vs-dark"
        options={{
          readOnly: false,
          minimap: { enabled: true },
          scrollBeyondLastLine: false,
          fontSize: 14,
          wordWrap: 'on',
          automaticLayout: true,
        }}
        onMount={handleEditorDidMount}
        original="// Original text\nfunction hello() {\n  console.log('Hello, World!');\n}"
        modified="// Modified text\nfunction hello() {\n  console.log('Hello, World!');\n  console.log('Modified!');\n}"
      />
    </div>
  );
}

export default App;

