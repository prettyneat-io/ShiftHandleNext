import { useAuthStore } from '~/stores/auth'

/**
 * Auth Plugin
 * Ensures auth state is initialised on client hydration
 */
export default defineNuxtPlugin(async () => {
  const authStore = useAuthStore()

  try {
    await authStore.init()
  } catch (error) {
    console.error('Auth init failed in client plugin:', error)
  }
})
