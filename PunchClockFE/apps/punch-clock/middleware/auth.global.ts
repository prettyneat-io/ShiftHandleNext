import { useAuthStore } from '~/stores/auth'

/**
 * Global auth middleware
 * Protects routes that require authentication
 */
export default defineNuxtRouteMiddleware(async (to, from) => {
  const authStore = useAuthStore()

  // Public routes that don't require authentication
  const publicRoutes = ['/login', '/signup', '/forgot-password']

  // Ensure auth state is initialised before making decisions
  await authStore.init()

  const isPublicRoute = publicRoutes.includes(to.path)

  if (!authStore.isAuthenticated && !isPublicRoute) {
    return navigateTo('/login')
  }

  if (authStore.isAuthenticated && to.path === '/login') {
    return navigateTo('/')
  }
})
