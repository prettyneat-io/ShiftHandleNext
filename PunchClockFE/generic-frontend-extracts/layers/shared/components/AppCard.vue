<template>
  <li
    class="relative col-span-1 flex rounded-xl shadow-lg shadow-black/10 transition-transform duration-150 ease-in-out hover:scale-[1.02]"
    :class="wrapperClass"
  >
    <div
      class="flex w-16 shrink-0 items-center justify-center rounded-l-xl text-sm font-medium text-white"
      :class="bgColor"
    >
      <slot name="initials">
        {{ initials }}
      </slot>
    </div>

    <div
      class="flex flex-1 items-center justify-between truncate rounded-r-xl border border-neutral-light/70 bg-background-light/90 backdrop-blur-sm transition-colors dark:border-neutral-dark/70 dark:bg-neutral-dark/60"
    >
      <div class="flex-1 truncate px-4 py-2 text-sm">
        <NuxtLink
          :to="to"
          class="font-medium text-neutral-dark dark:text-neutral-light"
        >
          <span class="absolute inset-0" aria-hidden="true"></span>
          {{ title }}
        </NuxtLink>
        <p v-if="subtitle" class="text-neutral-dark/80 dark:text-neutral/80">
          {{ subtitle }}
        </p>
        <p v-if="meta" class="text-xs text-neutral-dark/60 dark:text-neutral/60 mt-1">
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
const bgColor = computed(() => props.bgColor ?? 'bg-primary dark:bg-primary-dark')
const wrapperClass = computed(() => props.wrapperClass ?? '')
</script>
