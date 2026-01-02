import { useState, useRef } from 'react';
import ZoomMenu from './ZoomMenu';
import LanguageMenu from './LanguageMenu';

interface StatusBarProps {
  theme: 'vs' | 'vs-dark';
  lineNumber: number;
  columnNumber: number;
  selectedCharCount: number;
  language: string;
  wordWrap: 'on' | 'off';
  zoomLevel: number;
  onToggleWordWrap: () => void;
  onSetZoom: (zoom: number) => void;
  onSetLanguage: (language: string) => void;
}

export default function StatusBar({
  theme,
  lineNumber,
  columnNumber,
  selectedCharCount,
  language,
  wordWrap,
  zoomLevel,
  onToggleWordWrap,
  onSetZoom,
  onSetLanguage,
}: StatusBarProps) {
  const [showZoomMenu, setShowZoomMenu] = useState(false);
  const [showLanguageMenu, setShowLanguageMenu] = useState(false);
  const languageMenuRef = useRef<HTMLDivElement>(null);

  const handleZoomClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    setShowZoomMenu(!showZoomMenu);
    setShowLanguageMenu(false); // Close language menu if open
  };

  const handleZoomSelect = (zoom: number) => {
    onSetZoom(zoom);
    setShowZoomMenu(false);
  };

  const handleLanguageClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    setShowLanguageMenu(!showLanguageMenu);
    setShowZoomMenu(false); // Close zoom menu if open
  };

  const handleLanguageSelect = (lang: string) => {
    onSetLanguage(lang);
    setShowLanguageMenu(false);
  };

  // Get language display name
  const getLanguageDisplayName = (langId: string): string => {
    const langMap: Record<string, string> = {
      'plaintext': 'Plaintext',
      'javascript': 'JavaScript',
      'typescript': 'TypeScript',
      'json': 'JSON',
      'python': 'Python',
      'java': 'Java',
      'csharp': 'C#',
      'cpp': 'C++',
      'c': 'C',
      'html': 'HTML',
      'css': 'CSS',
      'xml': 'XML',
      'sql': 'SQL',
      'markdown': 'Markdown',
      'yaml': 'YAML',
      'shell': 'Shell',
      'powershell': 'PowerShell',
      'go': 'Go',
      'rust': 'Rust',
      'php': 'PHP',
    };
    return langMap[langId] || langId;
  };

  return (
    <div className={`status-bar ${theme === 'vs-dark' ? 'dark' : 'light'}`}>
      <div className="status-bar-content">
        <span className="status-item">
          行: {lineNumber}, 列: {columnNumber}
          {selectedCharCount > 0 && ` (已选择${selectedCharCount})`}
        </span>
        <span className="status-separator">|</span>
        <div className="language-container" ref={languageMenuRef}>
          <span 
            className="status-item status-clickable" 
            onClick={handleLanguageClick}
            title="点击选择语言"
          >
            {getLanguageDisplayName(language)}
          </span>
          <LanguageMenu
            theme={theme}
            currentLanguage={language}
            show={showLanguageMenu}
            onClose={() => setShowLanguageMenu(false)}
            onSelectLanguage={handleLanguageSelect}
          />
        </div>
        <span className="status-separator">|</span>
        <span 
          className="status-item status-clickable" 
          onClick={onToggleWordWrap}
          title="点击切换自动换行"
        >
          自动换行: {wordWrap === 'on' ? '开' : '关'}
        </span>
        <span className="status-separator">|</span>
        <div className="zoom-container">
          <span 
            className="status-item status-clickable" 
            onClick={handleZoomClick}
            title="点击设置缩放"
          >
            {zoomLevel}%
          </span>
          <ZoomMenu
            theme={theme}
            zoomLevel={zoomLevel}
            show={showZoomMenu}
            onClose={() => setShowZoomMenu(false)}
            onSelectZoom={handleZoomSelect}
          />
        </div>
      </div>
    </div>
  );
}

