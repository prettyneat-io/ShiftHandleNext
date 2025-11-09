<template>
  <li
    class="relative col-span-1 flex rounded-lg shadow-lg shadow-neutral-dark/5 transition-all duration-180 ease-out hover:shadow-xl hover:shadow-primary/10"
    :class="wrapperClass"
  >
    <div
      class="flex w-16 shrink-0 items-center justify-center rounded-l-lg text-sm font-medium text-white"
      :class="bgColor"
    >
      <slot name="initials">
        {{ initials }}
      </slot>
    </div>

    <div
      class="flex flex-1 items-center justify-between truncate rounded-r-lg border border-neutral-light/60 bg-white dark:border-border-dark dark:bg-panel-dark transition-colors"
    >
      <div class="flex-1 truncate px-4 py-2 text-sm">
        <NuxtLink
          :to="to"
          class="font-medium text-neutral-dark dark:text-text-dark"
        >
          <span class="absolute inset-0" aria-hidden="true"></span>
          {{ title }}
        </NuxtLink>
        <p v-if="subtitle" class="text-neutral-mid dark:text-text-dark-muted">
          {{ subtitle }}
        </p>
        <p v-if="meta" class="text-xs text-neutral-mid/80 dark:text-text-dark-muted/80 mt-1">
          {{ meta }}
        </p>
      </div>

    </div>
  </li>
</template>

<script setup lang="ts">
import { EllipsisVerticalIcon } from '@heroicons/vue/20/solid'

defineEmits<{ (e: 'action'): void }>()

const props = defineProps<{
  title: string
  to: string
  subtitle?: string
  meta?: string
  initials?: string
  bgColor?: string
  wrapperClass?: string
}>()

const initials = computed(() => props.initials ?? '–')
const bgColor = computed(() => props.bgColor ?? 'bg-primary dark:bg-primary-alt')
const wrapperClass = computed(() => props.wrapperClass ?? '')
</script>
