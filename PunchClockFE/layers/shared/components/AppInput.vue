<template>
  <div>
    <label :for="id" class="block text-sm font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-neutral/80">{{ label }}</label>
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

      <p v-if="error" :id="`${id}-err`" class="mt-2 text-sm text-danger dark:text-danger-light">
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
  type?: 'text' | 'number' | 'date' | 'paragraph' | 'choice' | 'related' | 'email' | 'password' | 'tel';
  error?: string;
  related?: {
    endpoint: string;
    label: string;
    include?: string;
  }
}>();

const emit = defineEmits(['update:modelValue', 'createNewRelated']);

const relatedItems = ref<any[]>([]);
const refreshStore = useRefreshStore();

const getDisplayValue = (item: any) => {
  // Special case for Seed: show strain name + seed ID
  if (item.strain && item.strain.strainName) {
    return `${item.strain.strainName} (Seed ID: ${item.id})`;
  }
  return item.name
    || item.strainName
    || item.plantIdentifier
    || item.plantGroupIdentifier
    || item.packageIdentifier
    || `ID: ${item.id}`;
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

watch(() => props.related ? refreshStore.refreshCounters[props.related.endpoint] : null,
  (newValue, oldValue) => {
    if (newValue !== null) {
      fetchRelatedItems();
    }
  }
);

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
  if (props.type === 'email') return 'email';
  if (props.type === 'password') return 'password';
  if (props.type === 'tel') return 'tel';
  return 'text';
});

const inputClasses = computed(() => [
  'block w-full max-w-xl rounded-md border py-1.5 px-3 shadow-sm transition-all duration-180 ease-out',
  'text-neutral-dark dark:text-text-dark placeholder:text-neutral-mid/60 dark:placeholder:text-text-dark-muted/60',
  props.error 
    ? 'border-danger/70 bg-danger/5 text-danger focus:border-danger focus:ring-2 focus:ring-danger/30'
    : 'border-neutral-light/70 bg-white dark:bg-white/5 dark:border-border-dark focus:border-primary focus:ring-2 focus:ring-primary/30',
  'sm:text-sm sm:leading-6',
  'disabled:cursor-not-allowed disabled:bg-background-alt/60 disabled:text-neutral-mid',
]);
</script>
