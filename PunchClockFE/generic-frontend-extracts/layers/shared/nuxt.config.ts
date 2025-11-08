// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  devtools: { enabled: true },
  compatibilityDate: '2025-02-16',
  modules: [
    '@nuxt/image',
    '@nuxtjs/tailwindcss',
    '@nuxtjs/google-fonts',
    '@pinia/nuxt',
    [
      '@vee-validate/nuxt',
      {
        // disable or enable auto imports
        autoImports: true,
      },
    ],
  ],
  image: {
    // Use static provider for static site generation
    provider: 'static',
    static: {
      baseURL: '/'
    },
    screens: {
      xs: 320,
      sm: 640,
      md: 768,
      lg: 1024,
      xl: 1280,
    },
  },
  ssr: true,
  css: [
    './assets/css/fonts.css',
    './assets/css/main.css'
  ],
  googleFonts: {
    families: {
      Pacifico: true,
      Righteous: true,
      Inter: [300, 400, 600],
      Poppins: [300, 400, 600],
      Montserrat: [300, 400, 600],
    },
    display: "swap",
  },
  pinia: {
    storesDirs: ['defineStore', 'acceptHMRUpdate'],
  },
})
