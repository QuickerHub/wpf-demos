import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

/**
 * Router Bridge - Provides navigation API for WPF
 * This allows WPF to navigate between pages programmatically
 */

// Store navigation function globally for WPF access
let globalNavigate: ((path: string) => void) | null = null;
let globalLocation: (() => string) | null = null;

/**
 * Initialize router bridge with navigation functions
 * This should be called from a component that has access to useNavigate
 */
export function initRouterBridge(navigate: (path: string) => void, getLocation: () => string) {
  globalNavigate = navigate;
  globalLocation = getLocation;
}

/**
 * Hook to expose router bridge to WPF
 * Use this in a component that has access to useNavigate
 */
export function useRouterBridge() {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    // Initialize bridge with current navigation function
    initRouterBridge(
      (path: string) => {
        navigate(path);
      },
      () => {
        return location.pathname + location.hash;
      }
    );

    // Expose navigation API to window for WPF
    (window as any).wpfRouter = {
      /**
       * Navigate to a specific route
       * @param path - Route path (e.g., '/diff', '/editor', '/')
       */
      navigate: (path: string) => {
        if (globalNavigate) {
          globalNavigate(path);
        } else {
          console.warn('Router bridge not initialized');
        }
      },

      /**
       * Get current route path
       * @returns Current route path
       */
      getCurrentPath: (): string => {
        if (globalLocation) {
          return globalLocation();
        }
        return window.location.hash.replace('#', '') || '/';
      },

      /**
       * Navigate back (if history available)
       */
      goBack: () => {
        if (window.history.length > 1) {
          window.history.back();
        }
      },

      /**
       * Navigate forward (if history available)
       */
      goForward: () => {
        window.history.forward();
      },
    };

    return () => {
      // Cleanup
      if ((window as any).wpfRouter) {
        delete (window as any).wpfRouter;
      }
    };
  }, [navigate, location]);
}

// Type declaration for WPF Router API
declare global {
  interface Window {
    wpfRouter?: {
      navigate: (path: string) => void;
      getCurrentPath: () => string;
      goBack: () => void;
      goForward: () => void;
    };
  }
}

