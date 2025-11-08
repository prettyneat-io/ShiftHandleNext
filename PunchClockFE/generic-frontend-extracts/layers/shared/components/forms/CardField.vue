<template>
  <div class="flex items-center text-sm">
    <dt class="text-gray-500 dark:text-gray-400 flex items-center shrink-0">
      <component :is="icon" class="h-4 w-4 mr-1.5" aria-hidden="true" />
      <span class="font-medium">{{ field.label }}:</span>
    </dt>
    <dd class="ml-2 text-gray-700 dark:text-gray-200 truncate">{{ formattedValue }}</dd>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { CalendarDaysIcon, InformationCircleIcon } from '@heroicons/vue/20/solid';
import type { Col } from './FormTable.vue';

const props = defineProps<{
  field: Col;
  value: any;
}>();

const icon = computed(() => {
  switch (props.field.type) {
    case 'date':
      return CalendarDaysIcon;
    default:
      return InformationCircleIcon;
  }
});

const formattedValue = computed(() => {
  const val = props.value;
  if (val === null || val === undefined || val === '') {
    return '—';
  }

  // Apply specific formatting based on the field type
  switch (props.field.type) {
    case 'date':
      const d = new Date(val);
      return isNaN(d.getTime()) ? String(val) : d.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      });
    // Add cases for 'decimal', 'currency', etc. here in the future
    default:
      return String(val);
  }
});
</script>
