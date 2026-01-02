import React from 'react';
import ReactDOM from 'react-dom/client';
import * as monaco from 'monaco-editor';
import App from './App';

// Configure Monaco Editor Web Workers
// Only use editor worker for syntax highlighting, exclude language service workers to reduce bundle size
import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';

self.MonacoEnvironment = {
  getWorker: function () {
    // Only use editor worker for basic syntax highlighting
    // No language services (autocomplete, error checking, etc.) to save ~8+ MB
    return new EditorWorker();
  }
};

// Make monaco available globally for components
(window as any).monaco = monaco;

// Initialize the app after Monaco is configured
ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

