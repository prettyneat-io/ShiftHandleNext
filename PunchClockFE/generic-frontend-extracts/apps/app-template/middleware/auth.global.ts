import { useAuthStore } from '../../../layers/shared/stores/auth'; // <-- Add this import

export default defineNuxtRouteMiddleware(async (to) => {
  const publicPages = ['/login', '/signup', '/forgot-password'];
  const isPublicPage = publicPages.includes(to.path);
  
  const authStore = useAuthStore();

  // On the initial client-side load, if we have a token but no user, fetch the user.
  if (process.client && !authStore.isAuthenticated && authStore.token) {
    await authStore.fetchUser();
  }

  if (isPublicPage && authStore.isAuthenticated) {
    // If user is logged in and tries to access a public page, redirect to home
    return navigateTo('/');
  }

  if (!isPublicPage && !authStore.isAuthenticated) {
    // If user is not logged in and tries to access a protected page, redirect to login
    return navigateTo('/login');
  }
});