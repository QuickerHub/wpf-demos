import { useState } from 'react';
import type { editor } from 'monaco-editor';

export function useEditorStatus() {
  const [lineNumber, setLineNumber] = useState(1);
  const [columnNumber, setColumnNumber] = useState(1);
  const [selectedCharCount, setSelectedCharCount] = useState(0);
  const [language, setLanguage] = useState('plaintext');
  const [wordWrap, setWordWrap] = useState<'on' | 'off'>('on');

  function setupStatusListeners(
    editorInstance: editor.IStandaloneCodeEditor | editor.IStandaloneDiffEditor
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

    // Update cursor position and selection
    const updateCursorPosition = () => {
      const focusedEditor = modifiedEditor && modifiedEditor.hasTextFocus() 
        ? modifiedEditor 
        : originalEditor;
      const position = focusedEditor.getPosition();
      if (position) {
        setLineNumber(position.lineNumber);
        setColumnNumber(position.column);
      }
      
      // Update selected character count
      const selections = focusedEditor.getSelections();
      if (selections && selections.length > 0) {
        const model = focusedEditor.getModel();
        if (model) {
          let totalChars = 0;
          for (const selection of selections) {
            const selectedText = model.getValueInRange(selection);
            totalChars += selectedText.length;
          }
          setSelectedCharCount(totalChars);
        }
      } else {
        setSelectedCharCount(0);
      }
    };

    // Listen to cursor position and selection changes
    originalEditor.onDidChangeCursorPosition(() => updateCursorPosition());
    originalEditor.onDidChangeCursorSelection(() => updateCursorPosition());
    if (modifiedEditor) {
      modifiedEditor.onDidChangeCursorPosition(() => updateCursorPosition());
      modifiedEditor.onDidChangeCursorSelection(() => updateCursorPosition());
    }

    // Note: We don't listen to language changes via onDidChangeMarkers because:
    // 1. onDidChangeMarkers is a global event that fires for ALL editors, causing unnecessary updates
    // 2. Language changes are user-initiated actions (via menu selection)
    // 3. We already update language state via setLanguage callback in onSetLanguage

    // Initial position update
    updateCursorPosition();
  }

  function toggleWordWrap() {
    setWordWrap(prev => prev === 'on' ? 'off' : 'on');
  }

  return {
    lineNumber,
    columnNumber,
    selectedCharCount,
    language,
    wordWrap,
    setLanguage,
    toggleWordWrap,
    setupStatusListeners,
  };
}

