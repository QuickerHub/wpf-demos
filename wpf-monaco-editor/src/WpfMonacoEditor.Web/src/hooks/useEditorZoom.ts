import { useRef, useState } from 'react';
import type { editor } from 'monaco-editor';

const DEFAULT_FONT_SIZE = 14;
const MIN_FONT_SIZE = 8;
const MAX_FONT_SIZE = 30;

export function useEditorZoom() {
  const editorInstanceRef = useRef<editor.IStandaloneCodeEditor | editor.IStandaloneDiffEditor | null>(null);
  const monacoRef = useRef<typeof import('monaco-editor') | null>(null);
  const isSettingZoomRef = useRef<boolean>(false);
  const [fontSize, setFontSize] = useState(DEFAULT_FONT_SIZE);

  // Calculate zoom level percentage based on current fontSize relative to DEFAULT_FONT_SIZE (14)
  // This is used for display only - actual zoom setting always uses DEFAULT_FONT_SIZE
  const zoomLevel = Math.round((fontSize / DEFAULT_FONT_SIZE) * 100);

  function setZoom(zoom: number) {
    if (!editorInstanceRef.current) return;
    const editorInstance = editorInstanceRef.current;
    
    // CRITICAL: Always calculate new fontSize based on DEFAULT_FONT_SIZE (14), NOT current fontSize
    // Example: zoom=150% -> newFontSize = 14 * 150 / 100 = 21 (not currentFontSize * 150 / 100)
    // This ensures consistent zoom behavior regardless of current zoom level
    const newFontSize = Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, (DEFAULT_FONT_SIZE * zoom) / 100));
    
    // Set flag to prevent onDidChangeConfiguration from interfering
    isSettingZoomRef.current = true;
    
    // Use DiffEditor's updateOptions method directly (applies to both original and modified editors)
    // Do NOT call getOriginalEditor().updateOptions() and getModifiedEditor().updateOptions() separately
    // DiffEditor.updateOptions() handles both editors automatically
    console.log('setZoom: newFontSize:', newFontSize);
    editorInstance.updateOptions({ fontSize: newFontSize });
    
    // Update state
    setFontSize(newFontSize);
    
    // Clear flag after update has taken effect
    setTimeout(() => {
      isSettingZoomRef.current = false;
    }, 100);
  }

  /**
   * Setup zoom synchronization between Monaco Editor and React state
   * 
   * This function:
   * 1. Saves editor instance reference for setZoom() to use
   * 2. Initializes fontSize state from editor's current fontSize
   * 3. Listens to editor configuration changes (e.g., mouse wheel zoom)
   * 4. Syncs editor's fontSize changes back to React state so status bar updates automatically
   * 
   * Why needed:
   * - setZoom() handles programmatic zoom (via status bar menu)
   * - Mouse wheel zoom (Ctrl+Wheel) is handled by Monaco Editor directly
   * - We need to listen to editor changes and sync to state for status bar display
   */
  function setupZoomSync(editorInstance: editor.IStandaloneCodeEditor | editor.IStandaloneDiffEditor, monaco: typeof import('monaco-editor')) {
    // Save editor reference for setZoom() to use
    editorInstanceRef.current = editorInstance;
    monacoRef.current = monaco;
    
    // Get the editor to listen to (for DiffEditor, use original editor)
    const editorToListen = ('getOriginalEditor' in editorInstance) 
      ? (editorInstance as editor.IStandaloneDiffEditor).getOriginalEditor()
      : (editorInstance as editor.IStandaloneCodeEditor);
    
    // Initialize fontSize state from editor's current fontSize
    const initialFontSize = editorToListen.getOption(monaco.editor.EditorOption.fontSize);
    setFontSize(initialFontSize);
    
    // Listen to editor configuration changes (e.g., mouse wheel zoom)
    // When user zooms with Ctrl+Wheel, Monaco Editor changes fontSize automatically
    // We need to sync this change back to React state so status bar updates
    let syncTimeout: ReturnType<typeof setTimeout> | null = null;
    editorToListen.onDidChangeConfiguration(() => {
      // Skip if we're currently setting zoom programmatically (via setZoom)
      // This prevents circular updates when setZoom() calls updateOptions()
      if (isSettingZoomRef.current) {
        return;
      }
      
      // Debounce to avoid rapid updates during continuous wheel zoom
      if (syncTimeout) {
        clearTimeout(syncTimeout);
      }
      syncTimeout = setTimeout(() => {
        const currentFontSize = editorToListen.getOption(monaco.editor.EditorOption.fontSize);
        // Sync editor's fontSize back to React state
        // This updates zoomLevel calculation, which updates status bar display
        setFontSize(currentFontSize);
      }, 50);
    });
  }

  return {
    fontSize,
    zoomLevel,
    setZoom,
    setupZoomSync,
  };
}

