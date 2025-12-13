import { defineConfig } from 'vite';
import { writeFileSync } from 'fs';
import { resolve } from 'path';

export default defineConfig({
  server: {
    port: 5173,
    strictPort: false, // Allow port fallback if 5173 is occupied
    cors: true,
    host: 'localhost',
    onListening(server) {
      const address = server.httpServer?.address();
      if (address && typeof address === 'object') {
        const url = `http://${address.address === '::' ? 'localhost' : address.address}:${address.port}`;
        // Write dev server URL to file for WPF to read
        const devServerFile = resolve(__dirname, '.vite-dev-server');
        writeFileSync(devServerFile, url, 'utf-8');
        console.log(`\n✓ Dev server URL written to: ${devServerFile}`);
        console.log(`✓ Dev server running at: ${url}\n`);
      }
    }
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    assetsDir: 'assets',
    rollupOptions: {
      input: {
        main: './index.html'
      }
    }
  },
  base: './' // Use relative paths for local file loading
});

