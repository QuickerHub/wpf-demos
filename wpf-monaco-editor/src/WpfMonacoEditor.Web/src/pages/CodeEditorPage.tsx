import { useEffect, useRef, useState } from 'react';
import { Editor } from '@monaco-editor/react';
import type { editor } from 'monaco-editor';
import StatusBar from '../components/StatusBar';
import LoadingOverlay from '../components/LoadingOverlay';
import '../App.css';

const DEFAULT_FONT_SIZE = 14;

/**
 * CodeEditor Page - Single editor view
 */
export default function CodeEditorPage() {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
  const monacoRef = useRef<typeof import('monaco-editor') | null>(null);
  const [isEditorReady, setIsEditorReady] = useState(false);
  const [theme, setTheme] = useState<'vs' | 'vs-dark'>('vs-dark');
  const wpfThemeRef = useRef<string | null>(null);
  
  // Status bar state
  const [lineNumber, setLineNumber] = useState(1);
  const [columnNumber, setColumnNumber] = useState(1);
  const [language, setLanguage] = useState('plaintext');
  const [wordWrap, setWordWrap] = useState<'on' | 'off'>('off');
  const [zoomLevel, setZoomLevel] = useState(100);
  const fontSizeRef = useRef<number>(DEFAULT_FONT_SIZE);
  const [currentFontSize, setCurrentFontSize] = useState(DEFAULT_FONT_SIZE);

  // Update font size
  const updateFontSize = (newFontSize: number) => {
    if (!editorRef.current) {
      return;
    }
    
    const MIN_FONT_SIZE = 8;
    const MAX_FONT_SIZE = 30;
    fontSizeRef.current = Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, newFontSize));
    
    editorRef.current.updateOptions({ fontSize: fontSizeRef.current });
    
    const zoomPercentage = Math.round((fontSizeRef.current / DEFAULT_FONT_SIZE) * 100);
    setZoomLevel(zoomPercentage);
    setCurrentFontSize(fontSizeRef.current);
  };

  const setZoom = (zoom: number) => {
    if (!editorRef.current) {
      return;
    }
    
    const newFontSize = (DEFAULT_FONT_SIZE * zoom) / 100;
    updateFontSize(newFontSize);
  };

  const toggleWordWrap = () => {
    setWordWrap(prev => prev === 'on' ? 'off' : 'on');
  };

  useEffect(() => {
    if (!editorRef.current || !isEditorReady) return;
    
    editorRef.current.updateOptions({ wordWrap: wordWrap });
  }, [wordWrap, isEditorReady]);

  // Theme management
  const updateMonacoTheme = (newTheme: 'vs' | 'vs-dark') => {
    setTheme(newTheme);
    if (monacoRef.current) {
      monacoRef.current.editor.setTheme(newTheme);
      
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

  const handleEditorDidMount = (
    editor: editor.IStandaloneCodeEditor,
    monaco: typeof import('monaco-editor')
  ) => {
    editorRef.current = editor;
    monacoRef.current = monaco;
    setIsEditorReady(true);

    const model = editor.getModel();
    if (model) {
      setLanguage(model.getLanguageId() || 'plaintext');
    }

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
    updateFontSize(DEFAULT_FONT_SIZE);
    
    editor.onDidChangeConfiguration(() => {
      const fontSize = editor.getOption(monaco.editor.EditorOption.fontSize);
      fontSizeRef.current = fontSize;
      const calculatedZoom = Math.round((fontSize / DEFAULT_FONT_SIZE) * 100);
      setZoomLevel(calculatedZoom);
    });

    updateCursorPosition();

    // Expose API to window for WPF integration
    (window as any).monacoEditor = {
      setValue: (text: string) => {
        const model = editor.getModel();
        if (model) {
          model.setValue(text);
        }
      },
      getValue: () => {
        const model = editor.getModel();
        return model?.getValue() || '';
      },
      setLanguage: (lang: string) => {
        const model = editor.getModel();
        if (model) {
          monaco.editor.setModelLanguage(model, lang);
          setLanguage(lang);
        }
      },
    };

    // Check for pending content
    if ((window as any).pendingEditorContent) {
      const pendingContent = (window as any).pendingEditorContent;
      const model = editor.getModel();
      if (model) {
        model.setValue(pendingContent.text || '');
        
        if (pendingContent.language) {
          monaco.editor.setModelLanguage(model, pendingContent.language);
          setLanguage(pendingContent.language);
        }
      }
      delete (window as any).pendingEditorContent;
    }

    // Set theme
    if (wpfThemeRef.current) {
      const isDark = wpfThemeRef.current === 'dark';
      const monacoTheme = isDark ? 'vs-dark' : 'vs';
      monaco.editor.setTheme(monacoTheme);
      setTheme(isDark ? 'vs-dark' : 'vs');
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      const monacoTheme = prefersDark ? 'vs-dark' : 'vs';
      monaco.editor.setTheme(monacoTheme);
      setTheme(prefersDark ? 'vs-dark' : 'vs');
    }
  };

  return (
    <>
      {!isEditorReady && <LoadingOverlay theme={theme} />}
      <div className="monaco-container">
        <div className="editor-wrapper">
          <Editor
            height="calc(100vh - 24px)"
            language="plaintext"
            theme={theme}
            loading={
              <div className={`monaco-loading ${theme === 'vs-dark' ? 'dark' : 'light'}`}>
                <div className="loading-spinner"></div>
              </div>
            }
            options={{
              minimap: { enabled: true },
              scrollBeyondLastLine: false,
              fontSize: currentFontSize,
              automaticLayout: true,
              wordWrap: wordWrap,
            }}
            onMount={handleEditorDidMount as any}
            defaultValue="// Code editor\nfunction hello() {\n  console.log('Hello, World!');\n}"
          />
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
            if (editorRef.current && monacoRef.current) {
              const model = editorRef.current.getModel();
              if (model) {
                monacoRef.current.editor.setModelLanguage(model, lang);
                setLanguage(lang);
              }
            }
          }}
        />
      </div>
    </>
  );
}

