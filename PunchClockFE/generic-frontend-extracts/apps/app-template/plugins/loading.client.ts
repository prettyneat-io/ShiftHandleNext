import { useLoadingStore } from '../../../layers/shared/stores/loading'

export default defineNuxtPlugin((nuxtApp) => {
  const loading = useLoadingStore()

  nuxtApp.hook('page:start', () => {
    loading.start()
  })

  nuxtApp.hook('page:finish', () => {
    // Add a small delay to prevent flickering on fast page loads
    setTimeout(() => {
      loading.finish()
    }, 200)
  })
})
