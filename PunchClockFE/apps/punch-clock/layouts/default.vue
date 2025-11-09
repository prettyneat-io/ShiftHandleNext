<template>
  <div
    v-if="authStore.isAuthenticated"
    class="min-h-screen bg-background-light dark:bg-background-dark text-neutral-dark dark:text-neutral flex flex-col"
  >
    <AppHeader />
    <div class="flex flex-1 overflow-hidden relative">
      <PunchClockSidebar />
      <main class="flex-1 overflow-y-auto p-6 transition-all duration-300 ease-in-out">
        <div class="max-w-7xl mx-auto">
          <slot />
        </div>
      </main>
    </div>
    <AppLoadingScreen />
    <AppNotificationContainer />
  </div>
  <NuxtLayout v-else name="auth">
    <slot />
  </NuxtLayout>
</template>

<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

const authStore = useAuthStore()
</script>
