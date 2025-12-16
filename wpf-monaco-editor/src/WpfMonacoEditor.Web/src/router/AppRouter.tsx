import { HashRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useRouterBridge } from '../lib/router-bridge';
import HomePage from '../pages/HomePage';
import DiffEditorPage from '../pages/DiffEditorPage';
import CodeEditorPage from '../pages/CodeEditorPage';

/**
 * Router Content - Internal component that has access to router hooks
 */
function RouterContent() {
  // Initialize router bridge for WPF navigation
  useRouterBridge();

  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/diff" element={<DiffEditorPage />} />
      <Route path="/editor" element={<CodeEditorPage />} />
      {/* Redirect unknown routes to home */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

/**
 * App Router - Uses HashRouter for WebView compatibility
 * HashRouter works better with WebView2 as it doesn't require server-side routing
 * 
 * Routes:
 * - / (or #/) - Home page with navigation
 * - /diff (or #/diff) - Diff Editor page
 * - /editor (or #/editor) - Code Editor page
 */
export default function AppRouter() {
  return (
    <HashRouter>
      <RouterContent />
    </HashRouter>
  );
}

