import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import path from 'path';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'auto',
      manifest: false, // Use existing public/manifest.json
      workbox: {
        // Precache versioned app shell assets only
        globPatterns: ['**/*.{js,css,html,svg,png,ico,woff2}'],
        // Security: NEVER cache protected API endpoints, JWTs, downloads or messages
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//, /^\/storage\//],
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname.startsWith('/api/') || url.pathname.startsWith('/storage/'),
            handler: 'NetworkOnly',
            options: {
              backgroundSync: {
                name: 'no-cache-queue',
                options: {
                  maxRetentionTime: 0
                }
              }
            }
          }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    open: true,
  },
  preview: {
    port: 3005,
    allowedHosts: true,
  },
});
