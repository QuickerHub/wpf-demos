import { useRef, useState } from 'react';
import { DiffEditor } from '@monaco-editor/react';
import type { editor } from 'monaco-editor';
import StatusBar from './StatusBar';
import LoadingOverlay from './LoadingOverlay';

const DEFAULT_FONT_SIZE = 14;

interface DiffEditorViewProps {
  theme: 'vs' | 'vs-dark';
  onEditorReady?: (editor: editor.IStandaloneDiffEditor, monaco: typeof import('monaco-editor')) => void;
}

export default function DiffEditorView({ theme, onEditorReady }: DiffEditorViewProps) {
  const diffEditorRef = useRef<editor.IStandaloneDiffEditor | null>(null);
  const monacoRef = useRef<typeof import('monaco-editor') | null>(null);
  const [isEditorReady, setIsEditorReady] = useState(false);
  
  // Status bar state
  const [lineNumber, setLineNumber] = useState(1);
  const [columnNumber, setColumnNumber] = useState(1);
  const [language, setLanguage] = useState('plaintext');
  const [wordWrap, setWordWrap] = useState<'on' | 'off'>('on');
  const [zoomLevel, setZoomLevel] = useState(100);
  const fontSizeRef = useRef<number>(DEFAULT_FONT_SIZE);
  const [currentFontSize, setCurrentFontSize] = useState(DEFAULT_FONT_SIZE);

  // Update font size (similar to reference code)
  const updateFontSize = (newFontSize: number) => {
    if (!diffEditorRef.current) {
      return;
    }
    
    // Clamp font size between 8 and 30 (same as reference)
    const MIN_FONT_SIZE = 8;
    const MAX_FONT_SIZE = 30;
    fontSizeRef.current = Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, newFontSize));
    
    const originalEditor = diffEditorRef.current.getOriginalEditor();
    const modifiedEditor = diffEditorRef.current.getModifiedEditor();
    
    if (!originalEditor || !modifiedEditor) {
      return;
    }
    
    // Update both editors' font size
    originalEditor.updateOptions({ fontSize: fontSizeRef.current });
    modifiedEditor.updateOptions({ fontSize: fontSizeRef.current });
    
    // Calculate and update zoom percentage
    const zoomPercentage = Math.round((fontSizeRef.current / DEFAULT_FONT_SIZE) * 100);
    setZoomLevel(zoomPercentage);
    setCurrentFontSize(fontSizeRef.current);
  };

  // Set zoom level (from percentage to font size)
  const setZoom = (zoom: number) => {
    if (!diffEditorRef.current) {
      return;
    }
    
    // Calculate font size from zoom percentage (same as reference code)
    const newFontSize = (DEFAULT_FONT_SIZE * zoom) / 100;
    updateFontSize(newFontSize);
  };

  // Toggle word wrap
  const toggleWordWrap = () => {
    if (!diffEditorRef.current) return;
    
    const newWordWrap = wordWrap === 'on' ? 'off' : 'on';
    setWordWrap(newWordWrap);
    
    const originalEditor = diffEditorRef.current.getOriginalEditor();
    const modifiedEditor = diffEditorRef.current.getModifiedEditor();
    
    originalEditor.updateOptions({ wordWrap: newWordWrap });
    modifiedEditor.updateOptions({ wordWrap: newWordWrap });
  };

  const handleEditorDidMount = (
    editor: editor.IStandaloneDiffEditor | editor.IStandaloneCodeEditor,
    monaco: typeof import('monaco-editor')
  ): void => {
    // Type guard to ensure it's a DiffEditor
    if (!('getOriginalEditor' in editor) || !('getModifiedEditor' in editor)) {
      console.error('Expected DiffEditor but got CodeEditor');
      return;
    }
    
    const diffEditor = editor as editor.IStandaloneDiffEditor;
    diffEditorRef.current = diffEditor;
    monacoRef.current = monaco;
    setIsEditorReady(true);

    // Get initial language from model
    const originalModel = diffEditor.getOriginalEditor().getModel();
    if (originalModel) {
      setLanguage(originalModel.getLanguageId() || 'plaintext');
    }

    // Listen to cursor position changes
    const originalEditor = diffEditor.getOriginalEditor();
    const modifiedEditor = diffEditor.getModifiedEditor();
    
    // Ensure original editor is editable
    originalEditor.updateOptions({ readOnly: false });
    
    const updateCursorPosition = () => {
      // Use the focused editor's position
      const focusedEditor = originalEditor.hasTextFocus() ? originalEditor : modifiedEditor;
      const position = focusedEditor.getPosition();
      if (position) {
        setLineNumber(position.lineNumber);
        setColumnNumber(position.column);
      }
    };

    // Listen to cursor position changes in both editors
    originalEditor.onDidChangeCursorPosition(() => updateCursorPosition());
    modifiedEditor.onDidChangeCursorPosition(() => updateCursorPosition());
    
    // Listen to language changes
    monaco.editor.onDidChangeMarkers(() => {
      if (originalModel) {
        setLanguage(originalModel.getLanguageId() || 'plaintext');
      }
    });

    // Initialize font size
    fontSizeRef.current = DEFAULT_FONT_SIZE;
    updateFontSize(DEFAULT_FONT_SIZE);
    
    // Listen to configuration changes for zoom (font size changes)
    originalEditor.onDidChangeConfiguration(() => {
      const fontSize = originalEditor.getOption(monaco.editor.EditorOption.fontSize);
      fontSizeRef.current = fontSize;
      // Calculate zoom based on default font size (same as reference code)
      const calculatedZoom = Math.round((fontSize / DEFAULT_FONT_SIZE) * 100);
      setZoomLevel(calculatedZoom);
    });

    // Initial position update
    updateCursorPosition();

    // Expose API to window for WPF integration
    window.monacoDiffEditor = {
      setOriginalText: (text: string) => {
        const originalModel = diffEditor.getOriginalEditor().getModel();
        if (originalModel) {
          originalModel.setValue(text);
        }
      },
      setModifiedText: (text: string) => {
        const modifiedModel = diffEditor.getModifiedEditor().getModel();
        if (modifiedModel) {
          modifiedModel.setValue(text);
        }
      },
      getOriginalText: () => {
        const originalModel = diffEditor.getOriginalEditor().getModel();
        return originalModel?.getValue() || '';
      },
      getModifiedText: () => {
        const modifiedModel = diffEditor.getModifiedEditor().getModel();
        return modifiedModel?.getValue() || '';
      },
      getOriginalModel: () => {
        return diffEditor.getOriginalEditor().getModel();
      },
      getModifiedModel: () => {
        return diffEditor.getModifiedEditor().getModel();
      },
      setLanguage: (lang: string) => {
        const originalModel = diffEditor.getOriginalEditor().getModel();
        const modifiedModel = diffEditor.getModifiedEditor().getModel();
        if (originalModel && modifiedModel) {
          monaco.editor.setModelLanguage(originalModel, lang);
          monaco.editor.setModelLanguage(modifiedModel, lang);
          setLanguage(lang);
        }
      },
    };

    // Check for pending content and set it automatically when Monaco Editor is ready
    if (window.pendingDiffContent) {
      const pendingContent = window.pendingDiffContent;
      const originalModel = diffEditor.getOriginalEditor().getModel();
      const modifiedModel = diffEditor.getModifiedEditor().getModel();
      if (originalModel && modifiedModel) {
        originalModel.setValue(pendingContent.original || '');
        modifiedModel.setValue(pendingContent.modified || '');
        
        // Set language if provided
        if (pendingContent.language) {
          monaco.editor.setModelLanguage(originalModel, pendingContent.language);
          monaco.editor.setModelLanguage(modifiedModel, pendingContent.language);
          setLanguage(pendingContent.language);
        }
      }
      // Clear pending content
      delete window.pendingDiffContent;
    }

    // Call onEditorReady callback
    if (onEditorReady) {
      onEditorReady(diffEditor, monaco);
    }
  };

  return (
    <>
      {!isEditorReady && <LoadingOverlay theme={theme} />}
      <div className="editor-wrapper">
        <DiffEditor
          height="calc(100vh - 24px)"
          language="plaintext"
          theme={theme}
          loading={
            <div className={`monaco-loading ${theme === 'vs-dark' ? 'dark' : 'light'}`}>
              <div className="loading-spinner"></div>
            </div>
          }
          options={{
            readOnly: false,
            originalEditable: true, // Enable editing for original (left) editor
            minimap: { enabled: true },
            scrollBeyondLastLine: false,
            fontSize: currentFontSize,
            wordWrap: wordWrap,
            automaticLayout: true,
          }}
          onMount={handleEditorDidMount}
          original="// Original text\nfunction hello() {\n  console.log('Hello, World!');\n}"
          modified="// Modified text\nfunction hello() {\n  console.log('Hello, World!');\n  console.log('Modified!');\n}"
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
          if (diffEditorRef.current && monacoRef.current) {
            const originalModel = diffEditorRef.current.getOriginalEditor().getModel();
            const modifiedModel = diffEditorRef.current.getModifiedEditor().getModel();
            if (originalModel && modifiedModel) {
              monacoRef.current.editor.setModelLanguage(originalModel, lang);
              monacoRef.current.editor.setModelLanguage(modifiedModel, lang);
              setLanguage(lang);
            }
          }
        }}
      />
    </>
  );
}

