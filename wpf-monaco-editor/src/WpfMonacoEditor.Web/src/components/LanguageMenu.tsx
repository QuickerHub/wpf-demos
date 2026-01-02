import { useRef, useEffect, useState, useMemo } from 'react';

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
  const [searchText, setSearchText] = useState('');
  const [highlightedIndex, setHighlightedIndex] = useState(0);
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Filter languages based on search text (like native select behavior)
  // Supports searching by name or id (e.g., "cs" matches "C#")
  const filteredLanguages = useMemo(() => {
    if (!searchText) return LANGUAGES;
    const lowerSearch = searchText.toLowerCase();
    return LANGUAGES.filter(lang => {
      const nameLower = lang.name.toLowerCase();
      const idLower = lang.id.toLowerCase();
      return (
        nameLower.startsWith(lowerSearch) ||
        nameLower.includes(lowerSearch) ||
        idLower.startsWith(lowerSearch) ||
        idLower.includes(lowerSearch)
      );
    });
  }, [searchText]);

  // Reset search when menu opens/closes
  useEffect(() => {
    if (show) {
      setSearchText('');
      setHighlightedIndex(0);
    }
  }, [show]);

  // Handle keyboard input for filtering (like native select)
  useEffect(() => {
    if (!show) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      // Ignore if user is typing in an input field
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }

      // Handle Escape to close
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
        return;
      }

      // Handle Enter to select highlighted item
      if (event.key === 'Enter') {
        event.preventDefault();
        if (filteredLanguages[highlightedIndex]) {
          onSelectLanguage(filteredLanguages[highlightedIndex].id);
        }
        return;
      }

      // Handle Arrow keys for navigation
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        setHighlightedIndex(prev => (prev + 1) % filteredLanguages.length);
        return;
      }

      if (event.key === 'ArrowUp') {
        event.preventDefault();
        setHighlightedIndex(prev => (prev - 1 + filteredLanguages.length) % filteredLanguages.length);
        return;
      }

      // Handle character input for filtering (like native select)
      if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
        event.preventDefault();
        
        // Clear previous timeout
        if (searchTimeoutRef.current) {
          clearTimeout(searchTimeoutRef.current);
        }

        // Append character to search text
        const newSearchText = searchText + event.key;
        setSearchText(newSearchText);

        // Reset search after 1 second of no input (like native select)
        searchTimeoutRef.current = setTimeout(() => {
          setSearchText('');
          setHighlightedIndex(0);
        }, 1000);

        // Reset highlighted index when filtering
        setHighlightedIndex(0);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [show, searchText, filteredLanguages, highlightedIndex, onClose, onSelectLanguage]);

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

  // Scroll highlighted item into view
  useEffect(() => {
    if (menuRef.current && filteredLanguages.length > 0) {
      const items = menuRef.current.querySelectorAll('.language-menu-item');
      if (items[highlightedIndex]) {
        items[highlightedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    }
  }, [highlightedIndex, filteredLanguages.length]);

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
      {filteredLanguages.length === 0 ? (
        <div className="language-menu-item" style={{ opacity: 0.5 }}>
          No matches
        </div>
      ) : (
        filteredLanguages.map((lang, index) => (
          <div
            key={lang.id}
            className={`language-menu-item ${lang.id === currentLanguage ? 'active' : ''} ${index === highlightedIndex ? 'highlighted' : ''}`}
            onMouseEnter={() => setHighlightedIndex(index)}
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
        ))
      )}
    </div>
  );
}

