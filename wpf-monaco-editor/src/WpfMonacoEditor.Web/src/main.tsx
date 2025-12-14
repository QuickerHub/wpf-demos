import React from 'react';
import ReactDOM from 'react-dom/client';
import './config/monaco'; // Initialize Monaco Editor configuration
import App from './App';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

