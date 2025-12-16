import { Link } from 'react-router-dom';
import '../App.css';

/**
 * Home Page - Navigation page
 */
export default function HomePage() {
  return (
    <div style={{ 
      padding: '40px', 
      fontFamily: 'system-ui, -apple-system, sans-serif',
      maxWidth: '800px',
      margin: '0 auto'
    }}>
      <h1 style={{ marginBottom: '30px' }}>Monaco Editor</h1>
      
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        <Link 
          to="/diff" 
          style={{
            display: 'block',
            padding: '20px',
            border: '2px solid #007acc',
            borderRadius: '8px',
            textDecoration: 'none',
            color: '#007acc',
            transition: 'all 0.2s'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#f0f8ff';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'transparent';
          }}
        >
          <h2 style={{ margin: '0 0 10px 0' }}>Diff Editor</h2>
          <p style={{ margin: 0, color: '#666' }}>
            Side-by-side diff view for comparing two versions of code
          </p>
        </Link>

        <Link 
          to="/editor" 
          style={{
            display: 'block',
            padding: '20px',
            border: '2px solid #007acc',
            borderRadius: '8px',
            textDecoration: 'none',
            color: '#007acc',
            transition: 'all 0.2s'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#f0f8ff';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'transparent';
          }}
        >
          <h2 style={{ margin: '0 0 10px 0' }}>Code Editor</h2>
          <p style={{ margin: 0, color: '#666' }}>
            Single editor view for code editing
          </p>
        </Link>
      </div>
    </div>
  );
}

