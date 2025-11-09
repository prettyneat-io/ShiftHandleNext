/**
 * Terminal logger plugin
 * Sends client-side errors to the terminal via console
 * In development, Vite's HMR will capture these and show in terminal
 */
export default defineNuxtPlugin((nuxtApp) => {
  if (process.dev) {
    // Override Vue error handler to also log to terminal-style output
    const originalErrorHandler = nuxtApp.vueApp.config.errorHandler
    
    nuxtApp.vueApp.config.errorHandler = (error, instance, info) => {
      // Call original handler first
      if (originalErrorHandler) {
        originalErrorHandler(error, instance, info)
      }
      
      // Log in a way that's visible in terminal
      const componentName = instance?.$options?.__name || instance?.$options?.name || 'Unknown Component'
      const errorMessage = error instanceof Error ? error.message : String(error)
      
      // This format will be caught by Vite and shown in terminal
      console.error(`\n[Vue Error] ${errorMessage}`)
      console.error(`Component: ${componentName}`)
      console.error(`Info: ${info}`)
      if (error instanceof Error && error.stack) {
        console.error(`\nStack trace:\n${error.stack}`)
      }
    }
  }
})
