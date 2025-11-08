import { defineStore } from 'pinia';
import type { User } from '../types/User';

interface AuthState {
  user: User | null;
  token: string | null;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    token: useCookie('auth_token').value || null,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token && !!state.user,
    currentUser: (state) => state.user,
  },

  actions: {
    async login(credentials: { email: string; password: string }) {
      try {
        const { data } = await $fetch<{ data: { user: User; token: string } }>('/api/auth/login', {
          method: 'POST',
          body: credentials,
        });
        this.setAuth(data);
      } catch (error) {
        this.logout();
        throw error;
      }
    },

    async register(credentials: { name: string, email: string; password: string }) {
      try {
        const { data } = await $fetch<{ data: { user: User; token: string } }>('/api/auth/register', {
          method: 'POST',
          body: credentials,
        });
        this.setAuth(data);
      } catch (error) {
        this.logout();
        throw error;
      }
    },

    logout() {
      this.token = null;
      this.user = null;
      useCookie('auth_token').value = null;
      navigateTo('/login');
    },
    
    async fetchUser() {
      if (!this.token) return;

      try {
        const { data } = await $fetch<{ data: { user: User } }>('/api/auth/me', {
          headers: {
            Authorization: `Bearer ${this.token}`,
          },
        });
        this.user = data.user;
      } catch (error) {
        this.logout();
      }
    },

    setAuth({ user, token }: { user: User; token: string }) {
        this.user = user;
        this.token = token;
        useCookie('auth_token').value = token;
    }
  },
});