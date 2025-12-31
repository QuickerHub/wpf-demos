import { useEffect, useRef, useState } from 'react';
import type { editor } from 'monaco-editor';
import * as monaco from 'monaco-editor';
import StatusBar from '../components/StatusBar';
import LoadingOverlay from '../components/LoadingOverlay';
import '../App.css';

const DEFAULT_FONT_SIZE = 14;

/**
 * CodeEditor Page - Single editor view
 */
export default function CodeEditorPage() {
  const containerRef = useRef<HTMLDivElement>(null);
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
  const modelRef = useRef<editor.ITextModel | null>(null);
  const [isEditorReady, setIsEditorReady] = useState(false);
  const [theme, setTheme] = useState<'vs' | 'vs-dark'>('vs-dark');
  const wpfThemeRef = useRef<string | null>(null);
  
  // Status bar state
  const [lineNumber, setLineNumber] = useState(1);
  const [columnNumber, setColumnNumber] = useState(1);
  const [language, setLanguage] = useState('plaintext');
  const [wordWrap, setWordWrap] = useState<'on' | 'off'>('on');
  const [zoomLevel, setZoomLevel] = useState(100);
  const fontSizeRef = useRef<number>(DEFAULT_FONT_SIZE);
  const [currentFontSize, setCurrentFontSize] = useState(DEFAULT_FONT_SIZE);

  // Initialize Monaco Editor
  useEffect(() => {
    if (!containerRef.current || editorRef.current) {
      return;
    }

    // Create model
    const model = monaco.editor.createModel(
      "// Code editor\nfunction hello() {\n  console.log('Hello, World!');\n}",
      language
    );
    modelRef.current = model;

    // Create editor
    const editor = monaco.editor.create(containerRef.current, {
      model: model,
      minimap: { enabled: true },
      scrollBeyondLastLine: false,
      fontSize: DEFAULT_FONT_SIZE,
      automaticLayout: true,
      wordWrap: wordWrap,
      mouseWheelZoom: true,
      theme: theme,
    });

    editorRef.current = editor;
    setIsEditorReady(true);

    // Get initial language
    setLanguage(model.getLanguageId() || 'plaintext');

    // Update cursor position
    const updateCursorPosition = () => {
      const position = editor.getPosition();
      if (position) {
        setLineNumber(position.lineNumber);
        setColumnNumber(position.column);
      }
    };

    editor.onDidChangeCursorPosition(() => updateCursorPosition());
    
    monaco.editor.onDidChangeMarkers(() => {
      if (model) {
        setLanguage(model.getLanguageId() || 'plaintext');
      }
    });

    fontSizeRef.current = DEFAULT_FONT_SIZE;
    setCurrentFontSize(DEFAULT_FONT_SIZE);
    
    editor.onDidChangeConfiguration(() => {
      const fontSize = editor.getOption(monaco.editor.EditorOption.fontSize);
      fontSizeRef.current = fontSize;
      const calculatedZoom = Math.round((fontSize / DEFAULT_FONT_SIZE) * 100);
      setZoomLevel(calculatedZoom);
      setCurrentFontSize(fontSize);
    });

    updateCursorPosition();

    // Expose API to window for WPF integration
    (window as any).monacoEditor = {
      setValue: (text: string) => {
        if (model) {
          model.setValue(text);
        }
      },
      getValue: () => {
        return model?.getValue() || '';
      },
      setLanguage: (lang: string) => {
        if (model) {
          monaco.editor.setModelLanguage(model, lang);
          setLanguage(lang);
        }
      },
    };

    // Check for pending content
    if ((window as any).pendingEditorContent) {
      const pendingContent = (window as any).pendingEditorContent;
      if (model) {
        model.setValue(pendingContent.text || '');
        
        if (pendingContent.language) {
          monaco.editor.setModelLanguage(model, pendingContent.language);
          setLanguage(pendingContent.language);
        }
      }
      delete (window as any).pendingEditorContent;
    }

    // Cleanup
    return () => {
      if (editorRef.current) {
        editorRef.current.dispose();
        editorRef.current = null;
      }
      if (modelRef.current) {
        modelRef.current.dispose();
        modelRef.current = null;
      }
      if ((window as any).monacoEditor) {
        delete (window as any).monacoEditor;
      }
    };
  }, []); // Only run once on mount

  // Update font size
  const updateFontSize = (newFontSize: number) => {
    if (!editorRef.current) {
      return;
    }
    
    const MIN_FONT_SIZE = 8;
    const MAX_FONT_SIZE = 30;
    fontSizeRef.current = Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, newFontSize));
    
    editorRef.current.updateOptions({ fontSize: fontSizeRef.current });
    
    // Calculate zoom level percentage based on current fontSize relative to DEFAULT_FONT_SIZE (14)
    // This is used for display only - actual zoom setting always uses DEFAULT_FONT_SIZE
    const zoomPercentage = Math.round((fontSizeRef.current / DEFAULT_FONT_SIZE) * 100);
    setZoomLevel(zoomPercentage);
    setCurrentFontSize(fontSizeRef.current);
  };

  const setZoom = (zoom: number) => {
    if (!editorRef.current) {
      return;
    }
    
    // CRITICAL: Always calculate new fontSize based on DEFAULT_FONT_SIZE (14), NOT current fontSize
    // Example: zoom=150% -> newFontSize = 14 * 150 / 100 = 21 (not currentFontSize * 150 / 100)
    // This ensures consistent zoom behavior regardless of current zoom level
    const newFontSize = (DEFAULT_FONT_SIZE * zoom) / 100;
    updateFontSize(newFontSize);
  };

  const toggleWordWrap = () => {
    setWordWrap(prev => prev === 'on' ? 'off' : 'on');
  };

  // Update wordWrap
  useEffect(() => {
    if (!editorRef.current || !isEditorReady) return;
    
    editorRef.current.updateOptions({ wordWrap: wordWrap });
  }, [wordWrap, isEditorReady]);

  // Theme management
  const updateMonacoTheme = (newTheme: 'vs' | 'vs-dark') => {
    setTheme(newTheme);
    monaco.editor.setTheme(newTheme);
    
    if (newTheme === 'vs') {
      document.body.style.backgroundColor = '#ffffff';
      document.body.classList.add('light-theme');
      document.body.classList.remove('dark-theme');
    } else {
      document.body.style.backgroundColor = '#1e1e1e';
      document.body.classList.add('dark-theme');
      document.body.classList.remove('light-theme');
    }
  };

  useEffect(() => {
    (window as any).setWpfTheme = (wpfTheme: string) => {
      wpfThemeRef.current = wpfTheme;
      const monacoTheme = wpfTheme === 'dark' ? 'vs-dark' : 'vs';
      updateMonacoTheme(monacoTheme);
    };

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

  useEffect(() => {
    if (wpfThemeRef.current) {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const updateTheme = (e: MediaQueryList | MediaQueryListEvent) => {
      const newTheme = e.matches ? 'vs-dark' : 'vs';
      updateMonacoTheme(newTheme);
    };

    updateTheme(mediaQuery);

    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener('change', updateTheme);
      return () => {
        mediaQuery.removeEventListener('change', updateTheme);
      };
    } else {
      mediaQuery.addListener(updateTheme);
      return () => {
        mediaQuery.removeListener(updateTheme);
      };
    }
  }, []);

  return (
    <>
      {!isEditorReady && <LoadingOverlay theme={theme} />}
      <div className="monaco-container">
        <div className="editor-wrapper" style={{ height: 'calc(100vh - 24px)', width: '100%' }}>
          <div ref={containerRef} style={{ height: '100%', width: '100%' }} />
        </div>
        <StatusBar
          theme={theme}
          lineNumber={lineNumber}
          columnNumber={columnNumber}
          language={language}
          wordWrap={wordWrap}
          zoomLevel={zoomLevel}
          onToggleWordWrap={toggleWordWrap}
          onSetZoom={setZoom}
          onSetLanguage={(lang) => {
            if (editorRef.current && modelRef.current) {
              monaco.editor.setModelLanguage(modelRef.current, lang);
              setLanguage(lang);
            }
          }}
        />
      </div>
    </>
  );
}
