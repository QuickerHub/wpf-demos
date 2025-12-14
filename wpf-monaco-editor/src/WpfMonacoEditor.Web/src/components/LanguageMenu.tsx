import { useRef, useEffect } from 'react';

interface LanguageMenuProps {
  theme: 'vs' | 'vs-dark';
  currentLanguage: string;
  show: boolean;
  onClose: () => void;
  onSelectLanguage: (language: string) => void;
}

// Common languages only
const LANGUAGES = [
  { id: 'plaintext', name: 'Plaintext' },
  { id: 'javascript', name: 'JavaScript' },
  { id: 'typescript', name: 'TypeScript' },
  { id: 'json', name: 'JSON' },
  { id: 'python', name: 'Python' },
  { id: 'java', name: 'Java' },
  { id: 'csharp', name: 'C#' },
  { id: 'cpp', name: 'C++' },
  { id: 'c', name: 'C' },
  { id: 'html', name: 'HTML' },
  { id: 'css', name: 'CSS' },
  { id: 'xml', name: 'XML' },
  { id: 'sql', name: 'SQL' },
  { id: 'markdown', name: 'Markdown' },
  { id: 'yaml', name: 'YAML' },
  { id: 'shell', name: 'Shell' },
  { id: 'powershell', name: 'PowerShell' },
  { id: 'go', name: 'Go' },
  { id: 'rust', name: 'Rust' },
  { id: 'php', name: 'PHP' },
];

export default function LanguageMenu({ theme, currentLanguage, show, onClose, onSelectLanguage }: LanguageMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onClose();
      }
    };

    if (show) {
      const timeoutId = setTimeout(() => {
        document.addEventListener('click', handleClickOutside, true);
      }, 100);
      
      return () => {
        clearTimeout(timeoutId);
        document.removeEventListener('click', handleClickOutside, true);
      };
    }
  }, [show, onClose]);

  if (!show) return null;

  return (
    <div 
      ref={menuRef}
      className={`language-menu ${theme === 'vs-dark' ? 'dark' : 'light'}`}
      onClick={(e) => {
        e.stopPropagation();
      }}
      onMouseDown={(e) => {
        e.stopPropagation();
      }}
    >
      {LANGUAGES.map((lang) => (
        <div
          key={lang.id}
          className={`language-menu-item ${lang.id === currentLanguage ? 'active' : ''}`}
          onMouseDown={(e) => {
            e.stopPropagation();
            e.preventDefault();
            onSelectLanguage(lang.id);
          }}
          onClick={(e) => {
            e.stopPropagation();
            onSelectLanguage(lang.id);
          }}
        >
          {lang.name}
        </div>
      ))}
    </div>
  );
}

