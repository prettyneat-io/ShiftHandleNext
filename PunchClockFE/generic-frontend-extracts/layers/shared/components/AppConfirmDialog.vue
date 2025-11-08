<template>
  <TransitionRoot as="template" :show="isOpen">
    <Dialog class="relative z-50" @close="onCancel">
      <TransitionChild
        as="template"
        enter="ease-out duration-300"
        enter-from="opacity-0"
        enter-to="opacity-100"
        leave="ease-in duration-200"
        leave-from="opacity-100"
        leave-to="opacity-0"
      >
        <div class="fixed inset-0 bg-gray-500/75 transition-opacity dark:bg-gray-900/80 backdrop-blur-sm"></div>
      </TransitionChild>

      <div class="fixed inset-0 z-50 w-screen overflow-y-auto">
        <div class="flex min-h-full items-center justify-center p-4 text-center sm:p-0">
          <TransitionChild
            as="template"
            enter="ease-out duration-300"
            enter-from="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
            enter-to="opacity-100 translate-y-0 sm:scale-100"
            leave="ease-in duration-200"
            leave-from="opacity-100 translate-y-0 sm:scale-100"
            leave-to="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
          >
            <DialogPanel class="relative transform overflow-hidden rounded-lg bg-white text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg">
              <div class="bg-white px-4 pb-4 pt-5 sm:p-6 sm:pb-4 dark:bg-gray-800">
                <div class="sm:flex sm:items-start">
                  <div class="mx-auto flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-red-100 sm:mx-0 sm:h-10 sm:w-10 dark:bg-red-500/20">
                    <ExclamationTriangleIcon class="h-6 w-6 text-red-600 dark:text-red-400" aria-hidden="true" />
                  </div>
                  <div class="mt-3 text-center sm:ml-4 sm:mt-0 sm:text-left">
                    <DialogTitle as="h3" class="text-base font-semibold leading-6 text-gray-900 dark:text-white">{{ title }}</DialogTitle>
                    <div class="mt-2">
                      <p class="text-sm text-gray-500 dark:text-gray-400">{{ message }}</p>
                    </div>
                  </div>
                </div>
              </div>
              <div class="bg-gray-50 px-4 py-3 sm:flex sm:flex-row-reverse sm:px-6 dark:bg-gray-800/50">
                <AppButton variant="danger" size="small" @click="onConfirm">
                  {{ confirmText }}
                </AppButton>
                <AppButton variant="outline" size="small" @click="onCancel" class="mr-3">
                  {{ cancelText }}
                </AppButton>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </div>
    </Dialog>
  </TransitionRoot>
</template>

<script setup lang="ts">
import { Dialog, DialogPanel, DialogTitle, TransitionChild, TransitionRoot } from '@headlessui/vue'
import { ExclamationTriangleIcon } from '@heroicons/vue/24/outline'

withDefaults(defineProps<{
  isOpen: boolean
  title?: string
  message?: string
  confirmText?: string
  cancelText?: string
}>(), {
  title: 'Confirm Action',
  message: 'Are you sure you want to proceed? This action cannot be undone.',
  confirmText: 'Confirm',
  cancelText: 'Cancel',
});

const emit = defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()

const onConfirm = () => emit('confirm')
const onCancel = () => emit('cancel')
</script>
