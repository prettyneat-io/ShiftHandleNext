<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary/10 via-background-light to-secondary/10 dark:from-primary/20 dark:via-background-dark dark:to-secondary/20 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
      <!-- Header -->
      <div class="text-center">
        <h1 class="text-4xl font-bold text-neutral-dark dark:text-neutral-light mb-2">
          Punch Clock
        </h1>
        <h2 class="text-xl font-semibold text-neutral-dark/80 dark:text-neutral-light/80 mb-2">
          Administration System
        </h2>
        <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">
          Sign in to your account to continue
        </p>
      </div>

      <!-- Login Form -->
      <div class="p-6 rounded-xl shadow-lg bg-background-light dark:bg-neutral-dark/60 border border-neutral-light/70 dark:border-neutral-dark/70">
        <form @submit.prevent="handleLogin" class="space-y-6">
          <!-- Username -->
          <div>
            <label for="username" class="block text-sm font-medium text-neutral-dark dark:text-neutral-light mb-1">
              Username
            </label>
            <input
              id="username"
              v-model="form.username"
              type="text"
              placeholder="Enter your username"
              required
              :disabled="loading"
              autocomplete="username"
              class="block w-full rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-neutral-dark dark:text-neutral-light placeholder-neutral-dark/50 dark:placeholder-neutral-light/50 focus:border-primary focus:ring-1 focus:ring-primary"
            />
          </div>

          <!-- Password -->
          <div>
            <label for="password" class="block text-sm font-medium text-neutral-dark dark:text-neutral-light mb-1">
              Password
            </label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              placeholder="Enter your password"
              required
              :disabled="loading"
              autocomplete="current-password"
              class="block w-full rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-neutral-dark dark:text-neutral-light placeholder-neutral-dark/50 dark:placeholder-neutral-light/50 focus:border-primary focus:ring-1 focus:ring-primary"
            />
          </div>

          <!-- Remember Me & Forgot Password -->
          <div class="flex items-center justify-between">
            <div class="flex items-center">
              <input
                id="remember-me"
                v-model="form.rememberMe"
                type="checkbox"
                class="h-4 w-4 text-primary focus:ring-primary border-neutral-light rounded"
                :disabled="loading"
              />
              <label for="remember-me" class="ml-2 block text-sm text-neutral-dark dark:text-neutral-light">
                Remember me
              </label>
            </div>

            <NuxtLink
              to="/forgot-password"
              class="text-sm font-medium text-primary hover:text-primary-dark dark:hover:text-primary-light"
            >
              Forgot password?
            </NuxtLink>
          </div>

          <!-- Error Message -->
          <div v-if="error" class="p-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
            <p class="text-sm text-red-800 dark:text-red-200">{{ error }}</p>
          </div>

          <!-- Submit Button -->
          <button
            type="submit"
            :disabled="loading"
            class="w-full flex justify-center py-2 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary hover:bg-primary-dark focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="!loading">Sign In</span>
            <span v-else class="flex items-center justify-center">
              <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Signing in...
            </span>
          </button>
        </form>
      </div>

      <!-- Footer -->
      <div class="text-center">
        <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">
          Don't have an account?
          <NuxtLink
            to="/signup"
            class="font-medium text-primary hover:text-primary-dark dark:hover:text-primary-light"
          >
            Contact your administrator
          </NuxtLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

definePageMeta({
  layout: false,
})

const authStore = useAuthStore()
const router = useRouter()

const form = ref({
  username: '',
  password: '',
  rememberMe: false,
})

const loading = ref(false)
const error = ref('')

const handleLogin = async () => {
  loading.value = true
  error.value = ''

  try {
    await authStore.login({
      username: form.value.username,
      password: form.value.password,
    })

    console.log('Login successful, auth state:', {
      isAuthenticated: authStore.isAuthenticated,
      user: authStore.currentUser,
      token: !!authStore.token
    })
    
    // Redirect to dashboard
    await router.push('/')
  } catch (err: any) {
    console.error('Login error:', err)
    console.error('Error type:', err.constructor?.name)
    console.error('Error message:', err.message)
    console.error('Error details:', err)
    error.value = err?.message || 'Invalid username or password. Please try again.'
  } finally {
    loading.value = false
  }
}

// Redirect if already logged in
onMounted(() => {
  if (authStore.isAuthenticated) {
    router.push('/')
  }
})
</script>
