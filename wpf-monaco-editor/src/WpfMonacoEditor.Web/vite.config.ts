import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { writeFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [
    react(),
    {
      name: 'dev-server-info',
      configureServer(server) {
        server.httpServer?.once('listening', () => {
          const address = server.httpServer?.address();
          if (address && typeof address === 'object') {
            // Normalize address: convert IPv6 localhost (::, ::1) and IPv4 0.0.0.0 to localhost
            const host = address.address === '::' || address.address === '::1' || address.address === '0.0.0.0'
              ? 'localhost'
              : address.address;
            const url = `http://${host}:${address.port}`;
            const filePath = resolve(__dirname, process.env.VITE_DEV_SERVER_FILE || '.vite-dev-server');
            try {
              writeFileSync(filePath, url, 'utf-8');
              console.log(`\n✓ Dev server URL written to: ${filePath}`);
              console.log(`✓ Dev server running at: ${url}\n`);
            } catch (error) {
              console.warn(`\n⚠ Failed to write dev server URL file: ${error instanceof Error ? error.message : String(error)}\n`);
            }
          }
        });
      }
    }
  ],
  server: {
    port: parseInt(process.env.VITE_PORT || '5174', 10),
    host: process.env.VITE_HOST || 'localhost',
    strictPort: false,
    cors: true
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    assetsDir: 'assets',
    rollupOptions: {
      input: './index.html',
      output: {
        // Optimize chunk splitting for better caching and parallel loading
        manualChunks: (id) => {
          // Split node_modules into separate chunks
          if (id.includes('node_modules')) {
            if (id.includes('react') || id.includes('react-dom')) {
              return 'react-vendor';
            }
            if (id.includes('monaco-editor')) {
              return 'monaco-vendor';
            }
            // Other vendor libraries
            return 'vendor';
          }
        },
        // Optimize file names for better caching
        chunkFileNames: 'assets/[name]-[hash].js',
        entryFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]'
      }
    },
    // Use esbuild for faster minification (default in Vite 5)
    minify: 'esbuild',
    // Increase chunk size warning limit (Monaco Editor is large)
    chunkSizeWarningLimit: 2000,
    // Disable source maps for smaller bundle size
    sourcemap: false,
    // Enable CSS code splitting
    cssCodeSplit: true
  },
  base: './'
});

