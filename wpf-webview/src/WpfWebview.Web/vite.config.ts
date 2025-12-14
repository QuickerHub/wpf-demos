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
    port: parseInt(process.env.VITE_PORT || '5173', 10),
    host: process.env.VITE_HOST || 'localhost',
    strictPort: false,
    cors: true
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    assetsDir: 'assets',
    rollupOptions: {
      input: './index.html'
    }
  },
  base: './'
});

