<template>
  <div class="flex min-h-screen flex-1 flex-col justify-center bg-background-light px-6 py-12 dark:bg-background-dark lg:px-8">
    <slot />
    <AppLoadingScreen />
    <AppNotificationContainer />
  </div>
</template>

<script setup lang="ts">
// This layout provides the full-page wrapper for authentication forms.
</script>