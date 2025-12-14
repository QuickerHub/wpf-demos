import { useRef, useEffect } from 'react';

interface ZoomMenuProps {
  theme: 'vs' | 'vs-dark';
  zoomLevel: number;
  show: boolean;
  onClose: () => void;
  onSelectZoom: (zoom: number) => void;
}

const ZOOM_OPTIONS = [50, 75, 90, 100, 110, 125, 150, 200];

export default function ZoomMenu({ theme, zoomLevel, show, onClose, onSelectZoom }: ZoomMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onClose();
      }
    };

    if (show) {
      // Use a small delay to avoid closing immediately when opening
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
      className={`zoom-menu ${theme === 'vs-dark' ? 'dark' : 'light'}`}
      onClick={(e) => {
        e.stopPropagation();
      }}
      onMouseDown={(e) => {
        e.stopPropagation();
      }}
    >
      {ZOOM_OPTIONS.map((zoom) => (
        <div
          key={zoom}
          className={`zoom-menu-item ${zoom === zoomLevel ? 'active' : ''}`}
          onMouseDown={(e) => {
            e.stopPropagation();
            e.preventDefault();
            onSelectZoom(zoom);
          }}
          onClick={(e) => {
            e.stopPropagation();
            onSelectZoom(zoom);
          }}
        >
          {zoom}%
        </div>
      ))}
    </div>
  );
}

