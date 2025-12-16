import React from 'react';
import ReactDOM from 'react-dom/client';
import 'monaco-editor/min/vs/editor/editor.main.css'; // Ensure Monaco styles (diff highlights, themes)
import './config/monaco'; // Initialize Monaco Editor configuration
import App from './App';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

