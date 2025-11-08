<template>
  <nav v-if="trail.length > 1" aria-label="Breadcrumb" class="mb-4 flex items-center text-sm">
    <ol role="list" class="flex items-center space-x-1 text-gray-500 dark:text-gray-400">
      <li v-for="(crumb, index) in trail" :key="index">
        <div class="flex items-center">
          <button
            type="button"
            @click="handleClick(index)"
            :disabled="index === trail.length - 1"
            :class="[
              index === trail.length - 1
                ? 'font-semibold text-gray-800 dark:text-gray-200 cursor-default'
                : 'hover:underline'
            ]"
          >
            {{ crumb.name }}
          </button>
          <svg v-if="index < trail.length - 1" class="ml-1 h-5 w-5 flex-shrink-0" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
          </svg>
        </div>
      </li>
    </ol>
  </nav>
</template>

<script setup lang="ts">
const props = defineProps<{
  trail: { name: string }[]
}>()

const emit = defineEmits<{
  (e: 'navigate', toIndex: number): void
}>()

const handleClick = (index: number) => {
  if (index < props.trail.length - 1) {
    emit('navigate', index)
  }
}
</script>
