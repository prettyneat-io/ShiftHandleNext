<template>
  <div aria-live="assertive" class="pointer-events-none fixed inset-0 flex items-end px-4 py-6 sm:items-start sm:p-6 z-[100]">
    <div class="flex w-full flex-col items-center space-y-4 sm:items-end">
      <TransitionGroup name="list" tag="div" class="flex w-full flex-col items-center space-y-4 sm:items-end">
        <div
          v-for="notification in notificationStore.notifications"
          :key="notification.id"
          class="pointer-events-auto w-full max-w-sm overflow-hidden rounded-lg bg-white dark:bg-gray-800 shadow-lg ring-1 ring-black dark:ring-white ring-opacity-5 dark:ring-opacity-10"
        >
          <div class="p-4">
            <div class="flex items-start">
              <div class="flex-shrink-0">
                <CheckCircleIcon v-if="notification.type === 'success'" class="h-6 w-6 text-green-400" aria-hidden="true" />
                <XCircleIcon v-else class="h-6 w-6 text-red-400" aria-hidden="true" />
              </div>
              <div class="ml-3 w-0 flex-1 pt-0.5">
                <p class="text-sm font-medium text-gray-900 dark:text-gray-100">
                  {{ notification.type === 'success' ? 'Success' : 'Error' }}
                </p>
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  {{ notification.message }}
                </p>
              </div>
              <div class="ml-4 flex flex-shrink-0">
                <button
                  type="button"
                  @click="notificationStore.removeNotification(notification.id)"
                  class="inline-flex rounded-md bg-white dark:bg-gray-800 text-gray-400 hover:text-gray-500 dark:hover:text-gray-300 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 dark:focus:ring-offset-gray-800"
                >
                  <span class="sr-only">Close</span>
                  <XMarkIcon class="h-5 w-5" aria-hidden="true" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </TransitionGroup>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useNotificationStore } from '../stores/notification'
import { CheckCircleIcon, XCircleIcon } from '@heroicons/vue/24/outline'
import { XMarkIcon } from '@heroicons/vue/20/solid'

const notificationStore = useNotificationStore()
</script>

<style scoped>
.list-enter-active,
.list-leave-active {
  transition: all 0.5s ease;
}
.list-enter-from,
.list-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
