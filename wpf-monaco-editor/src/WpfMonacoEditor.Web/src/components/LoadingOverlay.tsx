interface LoadingOverlayProps {
  theme: 'vs' | 'vs-dark';
}

export default function LoadingOverlay({ theme }: LoadingOverlayProps) {
  return (
    <div className={`loading-overlay ${theme === 'vs-dark' ? 'dark' : 'light'}`}>
      <div className="loading-content">
        <div className="loading-spinner"></div>
      </div>
    </div>
  );
}

