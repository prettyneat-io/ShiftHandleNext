// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  extends: '../../layers/shared',
  compatibilityDate: '2024-11-01',
  devtools: { enabled: true },
  ssr: false,
  // plugins: ['~/plugins/my-api'],
  runtimeConfig: {
    public: {
      // client-side - this will be overridden by NUXT_PUBLIC_API_BASE env var
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '/api'
    },
  },

  vite: {
    server: {
      proxy: {
        // Everything under /api -> your backend
        '/api': {
          target: process.env.NUXT_PUBLIC_API_BASE || 'http://127.0.0.1:3002',
          changeOrigin: true,
          secure: false,
          ws: true,
          // If your backend does NOT have a /api prefix, uncomment the rewrite:
          // rewrite: (path) => path.replace(/^\/api/, '')
        },
      },
    },
  },
})
