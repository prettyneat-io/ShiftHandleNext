<template>
  <div>
    <label :for="id" class="block text-sm/6 font-medium text-gray-900 dark:text-white">{{ label }}</label>
    <div class="mt-2">
      <div v-if="type === 'related' && related" class="flex items-center gap-2">
        <select
          :id="id"
          :name="name"
          :value="modelValue"
          @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
          :class="[inputClasses, 'flex-grow']"
        >
          <option value="" disabled>Select a {{ related.label }}...</option>
          <option v-for="item in relatedItems" :key="item.id" :value="item.id">
            {{ item.displayValue }}
          </option>
        </select>
        <AppButton type="button" variant="primary" size="small" @click="emit('createNewRelated', id, related)">
          Create
        </AppButton>
      </div>

      <textarea
        v-else-if="type === 'paragraph'"
        :id="id"
        :name="name"
        rows="3"
        :value="modelValue"
        @input="$emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
        :class="inputClasses"
      />

      <input
        v-else
        :id="id"
        :name="name"
        :type="inputType"
        :value="modelValue"
        @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
        :class="inputClasses"
      />

      <p v-if="error" :id="`${id}-err`" class="mt-2 text-sm text-red-600 dark:text-red-400">
        {{ error }}
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRefreshStore } from '../stores/refresh'

const props = defineProps<{
  modelValue: string | number | null;
  label: string;
  id: string;
  name: string;
  type?: 'text' | 'number' | 'date' | 'paragraph' | 'choice' | 'related';
  error?: string;
  related?: {
    endpoint: string;
    label: string;
    include?: string;
  }
}>();

const emit = defineEmits(['update:modelValue', 'createNewRelated']);

const relatedItems = ref<any[]>([]);
const refreshStore = useRefreshStore(); // <-- INITIALIZE THE STORE

const getDisplayValue = (item: any) => {
  // Special case for Seed: show strain name + seed ID
  if (item.strain && item.strain.strainName) {
    return `${item.strain.strainName} (Seed ID: ${item.id})`;
  }
  return item.name                  // For Location, LightingCycle, PlantStage, Product, etc.
    || item.strainName          // For Strain
    || item.plantIdentifier     // For Plant
    || item.plantGroupIdentifier// For Batch
    || item.packageIdentifier   // For BatchPackage
    || `ID: ${item.id}`;        // Fallback
};

async function fetchRelatedItems() {
  if (props.type !== 'related' || !props.related?.endpoint) return;
  try {
    const params = new URLSearchParams();
    if (props.related.include) {
      params.append('include', props.related.include);
    }
    const response = await $fetch<{ data: any[] }>(`/api/${props.related.endpoint}`, { params });
    relatedItems.value = response.data.map(item => ({
      id: item.id,
      displayValue: getDisplayValue(item)
    }));
  } catch (error) {
    console.error(`Failed to fetch related items for ${props.related.endpoint}:`, error);
  }
}

// --- WATCH FOR REFRESH SIGNALS FROM THE STORE ---
watch(() => props.related ? refreshStore.refreshCounters[props.related.endpoint] : null,
  (newValue, oldValue) => {
    // Check if the counter has been incremented (i.e., a refresh was triggered)
    if (newValue !== null) {
      fetchRelatedItems();
    }
  }
);
// --- END OF NEW WATCHER ---

const handleNewItem = (newItem: any) => {
  relatedItems.value.push({
    id: newItem.id,
    displayValue: getDisplayValue(newItem)
  });
  emit('update:modelValue', newItem.id);
};

defineExpose({ handleNewItem });

onMounted(fetchRelatedItems);
watch(() => props.related?.endpoint, fetchRelatedItems);

const inputType = computed(() => {
  if (props.type === 'date') return 'datetime-local';
  if (props.type === 'number') return 'number';
  return 'text';
});

const inputClasses = computed(() => [
  'block w-full max-w-xl rounded-md border-0 py-1.5 px-3 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-primary focus:shadow-md dark:focus:shadow-lg dark:focus:shadow-primary/20 sm:text-sm sm:leading-6 dark:bg-white/5 dark:text-white dark:ring-white/10 dark:placeholder:text-gray-500 dark:focus:ring-primary disabled:cursor-not-allowed',
]);
</script>
