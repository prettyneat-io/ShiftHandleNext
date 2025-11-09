// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  extends: '../../layers/shared',
  compatibilityDate: '2024-11-01',
  devtools: { enabled: true },
  ssr: false,
  
  // Override CSS from layer - use absolute paths
  css: [
    '../../layers/shared/assets/css/fonts.css',
    '../../layers/shared/assets/css/main.css'
  ],
  
  runtimeConfig: {
    public: {
      // API base URL - can be overridden by NUXT_PUBLIC_API_BASE env var
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '/api',
      appName: 'ShiftHandle Next',
      appVersion: '1.0.0'
    },
  },

  vite: {
    server: {
      proxy: {
        // Proxy API requests to backend
        '/api': {
          target: process.env.BACKEND_URL || 'http://localhost:5187',
          changeOrigin: true,
          secure: false,
          ws: true, // Enable WebSocket support for SignalR
        },
      },
    },
    build: {
      sourcemap: true,
    },
  },

  app: {
    head: {
      title: 'ShiftHandle Next',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'ShiftHandle Next - Staff attendance and time tracking management' }
      ],
    },
  },
})
