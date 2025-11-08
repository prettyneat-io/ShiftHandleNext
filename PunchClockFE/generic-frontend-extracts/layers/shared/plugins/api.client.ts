import { ofetch } from 'ofetch';
import { useAuthStore } from '../stores/auth';
import { useNotificationStore } from '../stores/notification';

export default defineNuxtPlugin((nuxtApp) => {
  const authStore = useAuthStore();
  const notificationStore = useNotificationStore();

  globalThis.$fetch = ofetch.create({
    onRequest({ options }) {
      if (authStore.token) {
        options.headers = new Headers(options.headers);
        options.headers.set('Authorization', `Bearer ${authStore.token}`);
      }
    },
    onResponseError({ request, response, options }) {
      // ✨ FIX: Check for a status of 0 to detect client-side errors like
      // network issues or cancelled requests, which should be ignored.
      if (response.status === 0) {
        return; // Silently ignore the error.
      }

      if (response.status === 401) {
        authStore.logout();
        notificationStore.addNotification({
          message: 'Your session has expired. Please log in again.',
          type: 'error',
        });
        return;
      }

      const errorMessage = response._data?.error || 'An unexpected server error occurred.';
      notificationStore.addNotification({
        message: errorMessage,
        type: 'error',
      });
    }
  }) as unknown as typeof globalThis.$fetch;
});
