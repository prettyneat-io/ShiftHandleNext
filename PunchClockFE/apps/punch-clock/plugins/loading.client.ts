import { useLoadingStore } from '../../../layers/shared/stores/loading'

/**
 * Loading screen plugin
 * Shows a loading indicator during navigation
 */
export default defineNuxtPlugin(() => {
  const loadingStore = useLoadingStore()
  const router = useRouter()

  router.beforeEach(() => {
    loadingStore.start()
  })

  router.afterEach(() => {
    loadingStore.finish()
  })

  router.onError((error) => {
    console.error('Router error:', error)
    loadingStore.finish()
  })
})
