import { useState } from 'react';
import type { editor } from 'monaco-editor';

export function useEditorStatus() {
  const [lineNumber, setLineNumber] = useState(1);
  const [columnNumber, setColumnNumber] = useState(1);
  const [language, setLanguage] = useState('plaintext');
  const [wordWrap, setWordWrap] = useState<'on' | 'off'>('on');

  function setupStatusListeners(
    editorInstance: editor.IStandaloneCodeEditor | editor.IStandaloneDiffEditor,
    monaco: typeof import('monaco-editor')
  ) {
    // Get models and editors
    let originalModel: editor.ITextModel | null = null;
    let originalEditor: editor.IStandaloneCodeEditor;
    let modifiedEditor: editor.IStandaloneCodeEditor | null = null;

    if ('getOriginalEditor' in editorInstance) {
      // DiffEditor
      const diffEditor = editorInstance as editor.IStandaloneDiffEditor;
      originalEditor = diffEditor.getOriginalEditor();
      modifiedEditor = diffEditor.getModifiedEditor();
      originalModel = originalEditor.getModel();
      // modifiedModel is not used, only modifiedEditor is needed for cursor position
    } else {
      // CodeEditor
      originalEditor = editorInstance as editor.IStandaloneCodeEditor;
      originalModel = originalEditor.getModel();
    }

    // Get initial language
    if (originalModel) {
      setLanguage(originalModel.getLanguageId() || 'plaintext');
    }

    // Update cursor position
    const updateCursorPosition = () => {
      const focusedEditor = modifiedEditor && modifiedEditor.hasTextFocus() 
        ? modifiedEditor 
        : originalEditor;
      const position = focusedEditor.getPosition();
      if (position) {
        setLineNumber(position.lineNumber);
        setColumnNumber(position.column);
      }
    };

    // Listen to cursor position changes
    originalEditor.onDidChangeCursorPosition(() => updateCursorPosition());
    if (modifiedEditor) {
      modifiedEditor.onDidChangeCursorPosition(() => updateCursorPosition());
    }

    // Listen to language changes
    monaco.editor.onDidChangeMarkers(() => {
      if (originalModel) {
        setLanguage(originalModel.getLanguageId() || 'plaintext');
      }
    });

    // Initial position update
    updateCursorPosition();
  }

  function toggleWordWrap() {
    setWordWrap(prev => prev === 'on' ? 'off' : 'on');
  }

  return {
    lineNumber,
    columnNumber,
    language,
    wordWrap,
    setLanguage,
    toggleWordWrap,
    setupStatusListeners,
  };
}

