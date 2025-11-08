<template>
  <div class="h-full min-h-screen">
    <NuxtRouteAnnouncer />
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </div>
</template>

<style>
.page-enter-active,
.page-leave-active {
  transition: opacity 0.25s ease;
}
.page-enter-from,
.page-leave-to {
  opacity: 0;
}
</style>