<template>
  <TransitionRoot as="template" :show="isOpen">
    <Dialog class="relative z-50" @close="handleClose">
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
            <DialogPanel class="relative transform overflow-hidden rounded-lg bg-background-light text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-2xl dark:bg-background-dark border border-neutral-light dark:border-neutral-dark">
               <div class="absolute top-0 right-0 pt-4 pr-4 block">
                <button
                  type="button"
                  class="rounded-md bg-background-light dark:bg-background-dark text-neutral hover:text-neutral-dark focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
                  @click="emit('close')"
                >
                  <span class="sr-only">Close</span>
                  <XMarkIcon class="h-6 w-6" aria-hidden="true" />
                </button>
              </div>
              <div class="bg-background-light px-4 pt-5 pb-4 sm:p-6 sm:pb-4 dark:bg-background-dark">
                <div class="sm:flex sm:items-start">
                  <div class="mt-3 text-center sm:mt-0 sm:text-left w-full">
                    <DialogTitle as="h3" class="text-base font-semibold leading-6 text-neutral-dark dark:text-neutral-light">
                      <slot name="title" />
                    </DialogTitle>
                    <div class="mt-4">
                      <slot name="content" />
                    </div>
                  </div>
                </div>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </div>
    </Dialog>
  </TransitionRoot>
</template>

<script setup lang="ts">
import {
  Dialog,
  DialogPanel,
  DialogTitle,
  TransitionChild,
  TransitionRoot,
} from '@headlessui/vue'
import { XMarkIcon } from '@heroicons/vue/24/outline'

const props = defineProps<{
  isOpen: boolean,
  preventCloseOnBackdrop?: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const handleClose = () => {
  if (!props.preventCloseOnBackdrop) {
    emit('close')
  }
}
</script>
