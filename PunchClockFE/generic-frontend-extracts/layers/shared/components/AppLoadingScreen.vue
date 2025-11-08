<template>
  <Transition name="loader-fade">
    <div v-if="isLoading" class="top-loader"></div>
  </Transition>
</template>

<script setup lang="ts">
import { useLoadingStore } from '../stores/loading'
const loadingStore = useLoadingStore()
const isLoading = computed(() => loadingStore.isLoading)
</script>

<style scoped>
.top-loader {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  width: 100%;
  z-index: 9999;
  background-size: 200% 100%;
  background-image: linear-gradient(
    to right,
    transparent 0%,
    theme('colors.primary.DEFAULT') 50%,
    transparent 100%
  );
  animation: move-gradient 1.5s linear infinite;
}

@keyframes move-gradient {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

.loader-fade-leave-active {
  transition: opacity 0.5s ease;
}
.loader-fade-enter-active {
  transition: opacity 0.1s ease;
}
.loader-fade-enter-from,
.loader-fade-leave-to {
  opacity: 0;
}
</style>