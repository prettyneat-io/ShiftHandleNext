<template>
  <div class="space-y-6">
    <AppNestedBreadcrumbs v-if="breadcrumbTrail" :trail="breadcrumbTrail" />

    <div class="flex items-center justify-between pb-4 border-b border-gray-200 dark:border-white/10">
      <h3 class="text-lg font-semibold text-gray-900 dark:text-white">{{ title }}</h3>
      <div v-if="!isEditing">
        <AppButton @click="toggleEdit(true)" variant="primary" size="small" class="inline-flex items-center gap-2">
          <PencilIcon class="h-4 w-4" />
          <span>Edit</span>
        </AppButton>
      </div>
    </div>

    <div v-if="!isEditing" class="space-y-4">
      <div v-for="field in fields" :key="field.key" class="grid grid-cols-3 gap-4 text-sm">
        <dt class="font-medium text-gray-500 dark:text-gray-400 col-span-1">{{ field.label }}</dt>
        <dd class="text-gray-700 dark:text-gray-200 col-span-2">{{ formattedValue(field) }}</dd>
      </div>
    </div>

    <div v-else>
      <DynamicCreateForm
        :endpoint="endpoint"
        :fields="fields"
        :initial-values="record"
        :record-id="record.id"
        mode="edit"
        title=""
        @updated="handleUpdateSuccess"
        :breadcrumb-trail="breadcrumbTrail"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { PencilIcon } from '@heroicons/vue/20/solid';
import type { Col } from './FormTable.vue';
import DynamicCreateForm from './DynamicCreateForm.vue';

const props = defineProps<{
  title: string;
  record: Record<string, any>;
  fields: Col[];
  endpoint: string;
  breadcrumbTrail?: { name: string }[];
}>();

const emit = defineEmits(['updated']);

const isEditing = ref(false);

const toggleEdit = (value: boolean) => {
  isEditing.value = value;
};

const handleUpdateSuccess = (updatedRecord: any) => {
  emit('updated', updatedRecord);
  isEditing.value = false; // Switch back to view mode on success
};

// Helper to format the displayed value in read-only mode
const formattedValue = (field: Col) => {
  const rawValue = props.record[field.key];
  if (rawValue === null || rawValue === undefined || rawValue === '') {
    return '—';
  }
  if (field.type === 'date') {
    const d = new Date(rawValue);
    return isNaN(d.getTime()) ? String(rawValue) : d.toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  // This could be expanded to format related fields better if needed
  return String(rawValue);
};

// Expose toggleEdit to parent components if needed
defineExpose({ toggleEdit });
</script>
