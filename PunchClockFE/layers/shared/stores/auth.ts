import { defineStore } from 'pinia'
import type { User } from '../types/User'

type ApiUser = (User & Record<string, unknown>) | null
type LoginCredentials =
  | { username: string; password: string }
  | { email: string; password: string }

interface AuthState {
  user: ApiUser
  token: string | null
  refreshToken: string | null
  initialized: boolean
}

interface AuthPayload {
  user: NonNullable<ApiUser>
  token: string
  refreshToken: string
}

const TOKEN_COOKIE = 'auth_token'
const REFRESH_COOKIE = 'refresh_token'

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    token: null,
    refreshToken: null,
    initialized: false,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token && !!state.user,
    currentUser: (state) => state.user,
  },

  actions: {
    async init() {
      if (this.initialized) {
        return
      }

      const tokenCookie = useCookie<string | null>(TOKEN_COOKIE, {
        maxAge: 60 * 60 * 24 * 7,
        sameSite: 'lax',
        secure: false,
      })
      const refreshCookie = useCookie<string | null>(REFRESH_COOKIE, {
        maxAge: 60 * 60 * 24 * 30,
        sameSite: 'lax',
        secure: false,
      })

      this.token = tokenCookie.value || null
      this.refreshToken = refreshCookie.value || null

      if (this.token && !this.user) {
        try {
          await this.fetchUser()
        } catch (error) {
          console.error('Auth init failed while fetching user:', error)
          this.clearSession({ redirect: false })
        }
      }

      this.initialized = true
    },

    async login(credentials: LoginCredentials) {
      try {
        const response = await $fetch('/api/auth/login', {
          method: 'POST',
          body: credentials,
        })

        if (!response) {
          throw new Error('No response received from server')
        }

        const data = response as {
          accessToken?: string
          refreshToken?: string
          user?: NonNullable<ApiUser>
        }

        if (data.accessToken && data.refreshToken && data.user) {
          this.setAuth({
            user: data.user,
            token: data.accessToken,
            refreshToken: data.refreshToken,
          })
          return true
        }

        throw new Error('Invalid response from server - missing required fields')
      } catch (error) {
        console.error('Login error caught in shared auth store:', error)
        this.clearSession({ redirect: false })
        throw error
      }
    },

    async register(credentials: { email: string; password: string; firstName: string; lastName: string }) {
      try {
        const response = await $fetch('/api/auth/register', {
          method: 'POST',
          body: credentials,
        })

        const data = response as {
          accessToken?: string
          refreshToken?: string
          user?: NonNullable<ApiUser>
        }

        if (data.accessToken && data.refreshToken && data.user) {
          this.setAuth({
            user: data.user,
            token: data.accessToken,
            refreshToken: data.refreshToken,
          })
        } else {
          throw new Error('Invalid response from server')
        }
      } catch (error) {
        this.clearSession({ redirect: false })
        throw error
      }
    },

    logout() {
      this.clearSession({ redirect: true })
    },

    async fetchUser() {
      if (!this.token) {
        return
      }

      const config = useRuntimeConfig()
      const headers = new Headers()
      headers.set('Authorization', `Bearer ${this.token}`)

      const response = await fetch(`${config.public.apiBase}/auth/me`, {
        headers,
      })

      if (!response.ok) {
        if (response.status === 401) {
          this.clearSession({ redirect: false })
        }
        throw new Error('Failed to fetch user')
      }

      const data = await response.json()
      this.user = (data as { user?: NonNullable<ApiUser> }).user || (data as ApiUser)
      return this.user
    },

    async refreshAuthToken() {
      if (!this.refreshToken) {
        this.clearSession({ redirect: true })
        return
      }

      try {
        const response = await $fetch('/api/auth/refresh', {
          method: 'POST',
          body: {
            refreshToken: this.refreshToken,
          },
        })

        const data = response as {
          accessToken?: string
          refreshToken?: string
        }

        if (data.accessToken) {
          this.token = data.accessToken
          useCookie<string | null>(TOKEN_COOKIE, {
            maxAge: 60 * 60 * 24 * 7,
            sameSite: 'lax',
            secure: false,
          }).value = data.accessToken

          if (data.refreshToken) {
            this.refreshToken = data.refreshToken
            useCookie<string | null>(REFRESH_COOKIE, {
              maxAge: 60 * 60 * 24 * 30,
              sameSite: 'lax',
              secure: false,
            }).value = data.refreshToken
          }
        } else {
          throw new Error('Failed to refresh token')
        }
      } catch (error) {
        console.error('Token refresh failed:', error)
        this.clearSession({ redirect: true })
      }
    },

    setAuth({ user, token, refreshToken }: AuthPayload) {
      this.user = user
      this.token = token
      this.refreshToken = refreshToken

      useCookie<string | null>(TOKEN_COOKIE, {
        maxAge: 60 * 60 * 24 * 7,
        sameSite: 'lax',
        secure: false,
      }).value = token

      useCookie<string | null>(REFRESH_COOKIE, {
        maxAge: 60 * 60 * 24 * 30,
        sameSite: 'lax',
        secure: false,
      }).value = refreshToken

      this.initialized = true
    },

    clearSession({ redirect = false }: { redirect?: boolean } = {}) {
      this.user = null
      this.token = null
      this.refreshToken = null
      this.initialized = true

      useCookie<string | null>(TOKEN_COOKIE, {
        maxAge: 60 * 60 * 24 * 7,
        sameSite: 'lax',
        secure: false,
      }).value = null

      useCookie<string | null>(REFRESH_COOKIE, {
        maxAge: 60 * 60 * 24 * 30,
        sameSite: 'lax',
        secure: false,
      }).value = null

      if (redirect) {
        navigateTo('/login')
      }
    },
  },
})