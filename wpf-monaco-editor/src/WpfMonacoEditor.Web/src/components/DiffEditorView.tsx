import { useRef, useState, useEffect } from 'react';
import type { editor } from 'monaco-editor';
import * as monaco from 'monaco-editor';
import StatusBar from './StatusBar';
import LoadingOverlay from './LoadingOverlay';
import { useEditorZoom } from '../hooks/useEditorZoom';
import { useEditorStatus } from '../hooks/useEditorStatus';

const DEFAULT_FONT_SIZE = 14;

interface DiffEditorViewProps {
  theme: 'vs' | 'vs-dark';
  onEditorReady?: (editor: editor.IStandaloneDiffEditor, monaco: typeof import('monaco-editor')) => void;
}

export default function DiffEditorView({ theme, onEditorReady }: DiffEditorViewProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const diffEditorRef = useRef<editor.IStandaloneDiffEditor | null>(null);
  const originalModelRef = useRef<editor.ITextModel | null>(null);
  const modifiedModelRef = useRef<editor.ITextModel | null>(null);
  const [isEditorReady, setIsEditorReady] = useState(false);
  
  const [originalText, setOriginalText] = useState("// Original text\nfunction hello() {\n  console.log('Hello, World!');\n}");
  const [modifiedText, setModifiedText] = useState("// Modified text\nfunction hello() {\n  console.log('Hello, World!');\n  console.log('Modified!');\n}");

  // Use custom hooks for zoom and status
  const { zoomLevel, setZoom, setupZoomSync } = useEditorZoom();
  const { lineNumber, columnNumber, selectedCharCount, language, wordWrap, setLanguage, toggleWordWrap, setupStatusListeners } = useEditorStatus();

  // Helper function to set language for both models in DiffEditor
  // Note: DiffEditor doesn't support setting language via updateOptions,
  // we must set language for each model separately
  const setDiffEditorLanguage = (lang: string) => {
    if (!diffEditorRef.current) return;
    
    const originalModel = diffEditorRef.current.getOriginalEditor().getModel();
    const modifiedModel = diffEditorRef.current.getModifiedEditor().getModel();
    
    if (originalModel) monaco.editor.setModelLanguage(originalModel, lang);
    if (modifiedModel) monaco.editor.setModelLanguage(modifiedModel, lang);
    
    setLanguage(lang);
  };

  // Initialize Monaco Editor
  useEffect(() => {
    if (!containerRef.current || diffEditorRef.current) {
      return;
    }

    // Create models
    const originalModel = monaco.editor.createModel(originalText, language);
    const modifiedModel = monaco.editor.createModel(modifiedText, language);
    originalModelRef.current = originalModel;
    modifiedModelRef.current = modifiedModel;

    // Create diff editor
    const diffEditor = monaco.editor.createDiffEditor(containerRef.current, {
      originalEditable: true,
      renderSideBySide: true,
      renderIndicators: true,
      minimap: { enabled: true },
      scrollBeyondLastLine: false,
      fontSize: DEFAULT_FONT_SIZE,
      automaticLayout: true,
      diffWordWrap: wordWrap,
      ignoreTrimWhitespace: false,
      renderOverviewRuler: true,
      mouseWheelZoom: true,
      theme: theme,
    });

    diffEditorRef.current = diffEditor;

    // Set model after editor is fully initialized
    // Use setTimeout to ensure DOM and editor are ready
    setTimeout(() => {
      if (diffEditorRef.current && originalModelRef.current && modifiedModelRef.current) {
        try {
          diffEditorRef.current.setModel({
            original: originalModelRef.current,
            modified: modifiedModelRef.current,
          });
          setIsEditorReady(true);
        } catch (error) {
          console.error('Error setting diff editor model:', error);
          setIsEditorReady(true); // Still mark as ready to show UI
        }
      }
    }, 0);

    // Cleanup
    return () => {
      if (diffEditorRef.current) {
        diffEditorRef.current.dispose();
        diffEditorRef.current = null;
      }
      if (originalModelRef.current) {
        originalModelRef.current.dispose();
        originalModelRef.current = null;
      }
      if (modifiedModelRef.current) {
        modifiedModelRef.current.dispose();
        modifiedModelRef.current = null;
      }
      if (window.monacoDiffEditor) {
        delete window.monacoDiffEditor;
      }
    };
  }, []); // Only run once on mount

  // Setup API, listeners and callbacks after editor is ready
  useEffect(() => {
    if (!isEditorReady || !diffEditorRef.current || !originalModelRef.current || !modifiedModelRef.current) {
      return;
    }

    // Listen to content changes
    originalModelRef.current.onDidChangeContent(() => {
      if (originalModelRef.current) {
        setOriginalText(originalModelRef.current.getValue());
      }
    });
    modifiedModelRef.current.onDidChangeContent(() => {
      if (modifiedModelRef.current) {
        setModifiedText(modifiedModelRef.current.getValue());
      }
    });

    // Setup zoom and status listeners
    setupZoomSync(diffEditorRef.current, monaco);
    setupStatusListeners(diffEditorRef.current);

    // Expose API to window for WPF integration
    window.monacoDiffEditor = {
      editor: diffEditorRef.current,
      setOriginalText: (text: string) => {
        if (originalModelRef.current) {
          originalModelRef.current.setValue(text);
        }
      },
      setModifiedText: (text: string) => {
        if (modifiedModelRef.current) {
          modifiedModelRef.current.setValue(text);
        }
      },
      getOriginalText: () => {
        return originalModelRef.current?.getValue() || '';
      },
      getModifiedText: () => {
        return modifiedModelRef.current?.getValue() || '';
      },
      getOriginalModel: () => {
        return originalModelRef.current;
      },
      getModifiedModel: () => {
        return modifiedModelRef.current;
      },
      setLanguage: (lang: string) => {
        setDiffEditorLanguage(lang);
      },
    };

    // Check for pending content
    if (window.pendingDiffContent) {
      const pendingContent = window.pendingDiffContent;
      if (originalModelRef.current) {
        originalModelRef.current.setValue(pendingContent.original || '');
      }
      if (modifiedModelRef.current) {
        modifiedModelRef.current.setValue(pendingContent.modified || '');
      }
      if (pendingContent.language) {
        setDiffEditorLanguage(pendingContent.language);
      }
      delete window.pendingDiffContent;
    }

    // Call onEditorReady callback
    if (onEditorReady && diffEditorRef.current) {
      onEditorReady(diffEditorRef.current, monaco);
    }
  }, [isEditorReady, onEditorReady, setupZoomSync, setupStatusListeners]);

  // Update text when state changes
  useEffect(() => {
    if (originalModelRef.current && originalModelRef.current.getValue() !== originalText) {
      originalModelRef.current.setValue(originalText);
    }
  }, [originalText]);

  useEffect(() => {
    if (modifiedModelRef.current && modifiedModelRef.current.getValue() !== modifiedText) {
      modifiedModelRef.current.setValue(modifiedText);
    }
  }, [modifiedText]);

  // Note: Language is set via setDiffEditorLanguage function, not via useEffect
  // This prevents unnecessary updates and ensures language is only changed when user explicitly selects it

  // Update diffWordWrap when wordWrap changes
  useEffect(() => {
    if (diffEditorRef.current && isEditorReady) {
      diffEditorRef.current.updateOptions({ diffWordWrap: wordWrap });
    }
  }, [wordWrap, isEditorReady]);

  // Update theme
  useEffect(() => {
    if (diffEditorRef.current) {
      monaco.editor.setTheme(theme);
    }
  }, [theme]);

  return (
    <>
      {!isEditorReady && <LoadingOverlay theme={theme} />}
      <div className="editor-wrapper" style={{ height: '100%', width: '100%' }}>
        <div ref={containerRef} style={{ height: '100%', width: '100%' }} />
      </div>
      <StatusBar
        theme={theme}
        lineNumber={lineNumber}
        columnNumber={columnNumber}
        selectedCharCount={selectedCharCount}
        language={language}
        wordWrap={wordWrap}
        zoomLevel={zoomLevel}
        onToggleWordWrap={toggleWordWrap}
        onSetZoom={setZoom}
        onSetLanguage={setDiffEditorLanguage}
      />
    </>
  );
}
