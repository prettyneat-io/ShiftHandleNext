/**
 * Global error handler plugin
 * Captures client-side errors and logs them clearly
 */
export default defineNuxtPlugin((nuxtApp) => {
  // Handle Vue errors - these are component errors like destructuring undefined
  nuxtApp.vueApp.config.errorHandler = (error, instance, info) => {
    console.error('\n=== Vue Error ===')
    console.error('Error:', error)
    console.error('Info:', info)
    if (instance) {
      const componentName = instance.$options.__name || instance.$options.name || 'Anonymous'
      console.error('Component:', componentName)
    }
    if (error instanceof Error && error.stack) {
      console.error('Stack:', error.stack)
    }
    console.error('=================\n')
  }

  // Nuxt-specific error hooks
  nuxtApp.hook('vue:error', (error, instance, info) => {
    console.error('\n=== Nuxt Vue Error ===')
    console.error('Error:', error)
    console.error('Info:', info)
    console.error('======================\n')
  })

  // App-level errors
  nuxtApp.hook('app:error', (error) => {
    console.error('\n=== App Error ===')
    console.error('Error:', error)
    console.error('==================\n')
  })

  // Catch unhandled promise rejections
  if (process.client) {
    window.addEventListener('unhandledrejection', (event) => {
      console.error('\n=== Unhandled Promise Rejection ===')
      console.error('Reason:', event.reason)
      console.error('====================================\n')
    })
  }
})
